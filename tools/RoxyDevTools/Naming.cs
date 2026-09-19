// Kiota names the fluent surface from the URL path alone: each segment becomes a
// PascalCased request builder property, each {param} an indexer, and the HTTP verb the
// executor method. This file is the one home for that rule. The docs renderer in
// Program.cs composes every call example from it, and the test project links this same
// file to walk the committed spec against the compiled client, so the renderer and the
// surface test can never restate the rule differently.
using System.Text.Json.Nodes;

namespace RoxyDevTools;

internal static class Naming
{
    public static bool IsHttpVerb(string v) => v is "get" or "post" or "put" or "delete" or "patch";

    /// <summary>A path segment or parameter name as the identifier Kiota emits: natal-chart is NatalChart.</summary>
    public static string Pascal(string s)
    {
        var parts = s.Split(['-', '_', ' ', '.', '/'], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }

    /// <summary>An HTTP verb as the executor method Kiota emits: post is PostAsync.</summary>
    public static string Method(string verb) => char.ToUpperInvariant(verb[0]) + verb[1..] + "Async";

    /// <summary>Every operation of the document in document order.</summary>
    public static IEnumerable<(string Path, string Verb, JsonObject Op)> Operations(JsonObject spec)
    {
        foreach (var (path, pathItem) in spec["paths"]!.AsObject())
            foreach (var (verb, opNode) in pathItem!.AsObject())
                if (IsHttpVerb(verb) && opNode is JsonObject op)
                    yield return (path, verb, op);
    }
}
