// The shared RoxyError schema is derived, never typed: its property set is the union of
// every 4xx and 5xx response schema in the document, so a field the API starts sending
// on an error appears in the generated exception without an edit here. Running it over an
// already normalized document is a no-op, because every response then references the
// schema it produced. The test project links this file to assert both properties.
using System.Text.Json.Nodes;

namespace RoxyDevTools;

internal static class ErrorSchema
{
    public const string Name = "RoxyError";

    private const string Fallback = "Error returned by every RoxyAPI endpoint on a 4xx or 5xx response.";

    // Every documented error carries both, so they stay required even when one response
    // schema omits one; the other required members are those present in every response.
    private static readonly string[] AlwaysRequired = ["code", "error"];

    /// <summary>
    /// Derives RoxyError plus one named schema per nested object (RoxyErrorIssue for the
    /// <c>issues</c> array). Throws when two schemas disagree on a property type.
    /// </summary>
    public static Dictionary<string, JsonObject> Derive(JsonObject spec)
    {
        var sources = new List<JsonObject>();
        foreach (var (_, _, op) in Naming.Operations(spec))
        {
            if (op["responses"] is not JsonObject responses) continue;
            foreach (var (code, node) in responses)
            {
                if (!IsError(code)) continue;
                var schema = Resolve(spec, Resolve(spec, node)?["content"]?["application/json"]?["schema"]);
                if (schema is not null) sources.Add(schema);
            }
        }
        if (sources.Count == 0) throw new InvalidOperationException("the document declares no error response schema");

        var hoisted = new Dictionary<string, JsonObject>();
        var error = MergeObject(spec, sources, Name, hoisted, Fallback, AlwaysRequired);
        foreach (var key in AlwaysRequired)
            if (error["properties"]?[key] is null)
                throw new InvalidOperationException($"no error response schema has a `{key}` property");
        error["properties"]!["error"]!["x-ms-primary-error-message"] = true;
        hoisted[Name] = error;
        return hoisted;
    }

    public static bool IsError(string status) => status.Length == 3 && status[0] is '4' or '5';

    // Keys are emitted in one fixed order (type, description, required, properties), and
    // required is sorted, so deriving from this output reproduces it byte for byte.
    private static JsonObject MergeObject(JsonObject spec, List<JsonObject> schemas, string name, Dictionary<string, JsonObject> hoisted,
        string? fallbackDescription = null, string[]? alwaysRequired = null)
    {
        var byProperty = new SortedDictionary<string, List<JsonObject>>(StringComparer.Ordinal);
        var requiredCount = new Dictionary<string, int>();
        foreach (var schema in schemas)
        {
            if (schema["properties"] is JsonObject props)
                foreach (var (key, value) in props)
                    if (Resolve(spec, value) is { } resolved)
                        (byProperty.TryGetValue(key, out var list) ? list : byProperty[key] = []).Add(resolved);
            if (schema["required"] is JsonArray required)
                foreach (var r in required) requiredCount[r!.GetValue<string>()] = requiredCount.GetValueOrDefault(r.GetValue<string>()) + 1;
        }

        var properties = new JsonObject();
        foreach (var (key, variants) in byProperty)
            properties[key] = MergeProperty(spec, variants, name + Pascal(key), key, hoisted);

        var merged = new JsonObject { ["type"] = "object" };
        if ((MostCommonDescription(schemas) ?? fallbackDescription) is { } description) merged["description"] = description;
        var always = byProperty.Keys
            .Where(k => requiredCount.GetValueOrDefault(k) == schemas.Count || (alwaysRequired?.Contains(k) ?? false)).ToList();
        if (always.Count > 0) merged["required"] = new JsonArray(always.Select(k => (JsonNode)JsonValue.Create(k)!).ToArray());
        merged["properties"] = properties;
        return merged;
    }

    private static JsonNode MergeProperty(JsonObject spec, List<JsonObject> variants, string nestedName, string key, Dictionary<string, JsonObject> hoisted)
    {
        var types = variants.Select(v => v["type"]?.GetValue<string>()).Where(t => t is not null).Distinct().ToList();
        if (types.Count > 1)
            throw new InvalidOperationException($"error property `{key}` has conflicting types across responses: {string.Join(", ", types)}");
        var type = types.SingleOrDefault() ?? "string";

        JsonNode result;
        switch (type)
        {
            case "object":
                result = Hoist(spec, variants, nestedName, hoisted);
                break;
            case "array":
                {
                    var items = variants.Select(v => Resolve(spec, v["items"])).OfType<JsonObject>().ToList();
                    result = new JsonObject { ["type"] = "array" };
                    if (MostCommonDescription(variants) is { } d) result["description"] = d;
                    var itemTypes = items.Select(i => i["type"]?.GetValue<string>() ?? "object").Distinct().ToList();
                    if (itemTypes.Count > 1)
                        throw new InvalidOperationException($"error property `{key}` has conflicting item types across responses: {string.Join(", ", itemTypes)}");
                    result["items"] = itemTypes.SingleOrDefault() == "object"
                        ? Hoist(spec, items, key.EndsWith('s') ? nestedName[..^1] : nestedName, hoisted)
                        : new JsonObject { ["type"] = itemTypes.SingleOrDefault() ?? "string" };
                    break;
                }
            default:
                result = new JsonObject { ["type"] = type };
                if (MostCommonDescription(variants) is { } desc) result["description"] = desc;
                break;
        }
        return result;
    }

    private static JsonObject Hoist(JsonObject spec, List<JsonObject> schemas, string name, Dictionary<string, JsonObject> hoisted)
    {
        hoisted[name] = MergeObject(spec, schemas, name, hoisted);
        return new JsonObject { ["$ref"] = $"#/components/schemas/{name}" };
    }

    // The wording most responses agree on; the first seen breaks a tie.
    private static string? MostCommonDescription(IEnumerable<JsonObject> schemas) =>
        schemas.Select(s => s["description"]?.GetValue<string>()).Where(d => !string.IsNullOrWhiteSpace(d))
            .GroupBy(d => d).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();

    // Follows local $ref pointers (components/schemas, components/responses) to the node itself.
    private static JsonObject? Resolve(JsonObject spec, JsonNode? node)
    {
        while (node is JsonObject o && o["$ref"]?.GetValue<string>() is { } pointer)
        {
            if (!pointer.StartsWith("#/", StringComparison.Ordinal))
                throw new InvalidOperationException($"unsupported $ref {pointer}");
            node = pointer[2..].Split('/').Aggregate((JsonNode?)spec, (current, part) => current?[part.Replace("~1", "/").Replace("~0", "~")]);
        }
        return node as JsonObject;
    }

    private static string Pascal(string s) => Naming.Pascal(s);
}
