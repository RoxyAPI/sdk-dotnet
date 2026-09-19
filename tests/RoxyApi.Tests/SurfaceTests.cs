using System.Reflection;
using System.Text.Json.Nodes;
using RoxyDevTools;

namespace RoxyApi.Tests;

// Walks every operation of the committed spec and asserts the compiled client exposes it
// through the naming rule in Naming.cs (linked from the generator tool, so the test and the
// docs renderer share one derivation): each path segment is a request builder property,
// each {param} an indexer typed from the parameter schema, and the verb an executor method.
// A path the SDK no longer exposes, or a parameter whose type moved, fails here by name.
public class SurfaceTests
{
    private static readonly JsonObject Spec =
        JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "specs", "openapi.json")))!.AsObject();

    public static TheoryData<string, string> Operations()
    {
        var data = new TheoryData<string, string>();
        foreach (var (path, verb, _) in Naming.Operations(Spec)) data.Add(verb, path);
        return data;
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public void Every_operation_is_reachable_on_the_client(string verb, string path)
    {
        var op = Spec["paths"]![path]![verb]!.AsObject();
        var pathParams = (op["parameters"]?.AsArray() ?? [])
            .Select(p => p!.AsObject())
            .Where(p => p["in"]?.GetValue<string>() == "path")
            .ToDictionary(p => p["name"]!.GetValue<string>(), p => p["schema"]!.AsObject());

        var builder = typeof(RoxyClient);
        var walked = "roxy";
        foreach (var segment in path.Trim('/').Split('/'))
        {
            if (segment.StartsWith('{'))
            {
                var name = segment[1..^1];
                var indexer = builder.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .SingleOrDefault(p => p.GetIndexParameters().Length == 1);
                Assert.True(indexer is not null, $"{verb} {path}: no indexer for {{{name}}} on {walked}");
                var expected = IndexerType(pathParams[name]);
                var actual = indexer!.GetIndexParameters()[0].ParameterType;
                Assert.True(expected == actual, $"{verb} {path}: {walked}[{{{name}}}] is {actual.Name}, the spec says {expected.Name}");
                builder = indexer.PropertyType;
                walked += $"[{{{name}}}]";
            }
            else
            {
                var member = Naming.Pascal(segment);
                var property = builder.GetProperty(member, BindingFlags.Public | BindingFlags.Instance);
                Assert.True(property is not null, $"{verb} {path}: {walked} has no {member}");
                builder = property!.PropertyType;
                walked += "." + member;
            }
        }

        var method = Naming.Method(verb);
        var executors = builder.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.Name == method).ToList();
        Assert.True(executors.Count == 1, $"{verb} {path}: {walked} has {executors.Count} {method} methods");
    }

    // Kiota types a path indexer from the parameter schema; the null member of a 3.1 union
    // carries no type, so a nullable integer is still an int indexer.
    private static Type IndexerType(JsonObject schema)
    {
        var type = schema["type"] switch
        {
            JsonArray a => a.Select(x => x!.GetValue<string>()).Single(t => t != "null"),
            JsonNode t => t.GetValue<string>(),
            _ => throw new InvalidOperationException($"path parameter without a type: {schema}"),
        };
        return type switch
        {
            "integer" => typeof(int),
            "number" => typeof(double),
            "string" => typeof(string),
            _ => throw new InvalidOperationException($"path parameter of type {type} has no indexer rule"),
        };
    }
}
