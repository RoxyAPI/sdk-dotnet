// RoxyAPI .NET SDK maintainer tooling. Two commands, both keyed off the live
// OpenAPI spec, which is the single source of truth.
//
//   generate    Fetch the spec, patch it for client generation, run Kiota into
//               src/Generated, then sync the spec-derived docs.
//   sync-docs   Regenerate only the spec-derived regions of README.md, AGENTS.md,
//               and docs/llms-full.txt (between BEGIN/END markers). Run by CI and
//               the pre-push hook to fail on drift.
//
// Run from the repository root: `dotnet run --project tools/RoxyDevTools -- <cmd>`.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

const string SpecUrl = "https://roxyapi.com/api/v2/openapi.json";
const string AbsoluteServerUrl = "https://roxyapi.com/api/v2";
const string SpecPath = "specs/openapi.json";
const string GeneratedDir = "src/Generated";

// Error status codes the API returns with a `{ error, code }` body. The served
// spec declares them as descriptions only (no schema) because typed 401/500
// responses break the server's route handler types. We attach a RoxyError schema
// to the local spec copy so Kiota emits one catchable, typed exception.
int[] errorStatusCodes = [400, 401, 404, 405, 429, 500];

return (args.FirstOrDefault()) switch
{
    "generate" => await GenerateAsync(),
    "sync-docs" => SyncDocs(),
    _ => Usage(),
};

int Usage()
{
    Console.Error.WriteLine("usage: dotnet run --project tools/RoxyDevTools -- <generate|sync-docs>");
    return 1;
}

async Task<int> GenerateAsync()
{
    Console.WriteLine($"Fetching OpenAPI spec from {SpecUrl}");
    using var http = new HttpClient();
    http.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
    var raw = await http.GetStringAsync(SpecUrl);

    var spec = JsonNode.Parse(raw)!.AsObject();
    PatchServerUrl(spec);
    FixPathParameters(spec);
    NormalizeErrors(spec);

    Directory.CreateDirectory("specs");
    var pretty = spec.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(SpecPath, pretty);
    Console.WriteLine($"Spec saved to {SpecPath}");

    Console.WriteLine("Running Kiota generator...");
    var exit = RunKiota();
    if (exit != 0) return exit;
    Console.WriteLine("SDK generated.");

    return SyncDocs();
}

// Rewrite the relative production server (`/api/v2`) to an absolute URL so the
// generated client targets production out of the box.
void PatchServerUrl(JsonObject spec)
{
    if (spec["servers"] is JsonArray servers && servers.Count > 0 && servers[0] is JsonObject s0)
    {
        s0["url"] = AbsoluteServerUrl;
    }
}

// OpenAPI requires path parameters to be required. At least one upstream route
// omits it, which Kiota rejects. Normalize every path parameter to required so
// generation is clean. (Reported upstream; harmless to enforce here regardless.)
void FixPathParameters(JsonObject spec)
{
    var fixedCount = 0;
    foreach (var (_, pathItem) in spec["paths"]!.AsObject())
    {
        foreach (var (verb, opNode) in pathItem!.AsObject())
        {
            if (!IsHttpVerb(verb) || opNode is not JsonObject op || op["parameters"] is not JsonArray ps) continue;
            foreach (var p in ps)
            {
                if (p is JsonObject param && param["in"]?.GetValue<string>() == "path"
                    && param["required"]?.GetValue<bool>() != true)
                {
                    param["required"] = true;
                    fixedCount++;
                }
            }
        }
    }
    if (fixedCount > 0) Console.WriteLine($"Forced required:true on {fixedCount} path parameters.");
}

// Every endpoint returns the same `{ error, code }` shape on failure (400 adds
// validation `issues[]`, 405 adds `allow[]`). The served spec inlines that schema
// per operation, so Kiota would emit a distinct error type for every operation and
// status. Point every error response at one shared RoxyError schema instead: Kiota
// then generates a single catchable exception inheriting ApiException, and the
// generated client shrinks by thousands of redundant types. x-ms-primary-error-message
// surfaces `error` as Exception.Message.
void NormalizeErrors(JsonObject spec)
{
    var schemas = spec["components"]!.AsObject()["schemas"]!.AsObject();

    schemas["RoxyErrorIssue"] = new JsonObject
    {
        ["type"] = "object",
        ["description"] = "A single field-level validation failure from a 400 response.",
        ["properties"] = new JsonObject
        {
            ["path"] = StringProp("Dot-separated field path, or (root) for a top-level error."),
            ["message"] = StringProp("Human readable description of this failure."),
            ["code"] = StringProp("Validation issue code, for example invalid_type or too_small."),
            ["expected"] = StringProp("Expected type, when the issue is a type mismatch."),
        },
    };
    schemas["RoxyError"] = new JsonObject
    {
        ["type"] = "object",
        ["description"] = "Error returned by every RoxyAPI endpoint on a 4xx or 5xx response.",
        ["required"] = new JsonArray("error", "code"),
        ["properties"] = new JsonObject
        {
            ["error"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Human readable error message. Wording may change; switch on Code instead.",
                ["x-ms-primary-error-message"] = true,
            },
            ["code"] = StringProp("Stable machine readable error code, for example validation_error or rate_limit_exceeded."),
            ["issues"] = new JsonObject
            {
                ["type"] = "array",
                ["description"] = "Present on 400 responses: every field that failed validation.",
                ["items"] = new JsonObject { ["$ref"] = "#/components/schemas/RoxyErrorIssue" },
            },
            ["allow"] = new JsonObject
            {
                ["type"] = "array",
                ["description"] = "Present on 405 responses: the HTTP methods this path accepts.",
                ["items"] = new JsonObject { ["type"] = "string" },
            },
            ["docs"] = StringProp("Link to the documentation for this domain, when available."),
        },
    };

    var normalized = 0;
    foreach (var (_, pathItem) in spec["paths"]!.AsObject())
    {
        foreach (var (verb, opNode) in pathItem!.AsObject())
        {
            if (!IsHttpVerb(verb) || opNode is not JsonObject op || op["responses"] is not JsonObject responses) continue;
            foreach (var code in errorStatusCodes)
            {
                if (responses[code.ToString()] is not JsonObject resp) continue;
                resp["content"] = new JsonObject
                {
                    ["application/json"] = new JsonObject
                    {
                        ["schema"] = new JsonObject { ["$ref"] = "#/components/schemas/RoxyError" },
                    },
                };
                normalized++;
            }
        }
    }
    Console.WriteLine($"Normalized {normalized} error responses to RoxyError.");
}

JsonObject StringProp(string description) => new() { ["type"] = "string", ["description"] = description };

int RunKiota()
{
    var kiota = Environment.GetEnvironmentVariable("KIOTA_PATH") ?? "kiota";
    var psi = new ProcessStartInfo(kiota)
    {
        UseShellExecute = false,
    };
    foreach (var a in new[]
    {
        "generate", "--language", "CSharp",
        "--class-name", "RoxyClient",
        "--namespace-name", "RoxyApi",
        "--openapi", SpecPath,
        "--output", GeneratedDir,
        "--exclude-backward-compatible",
        "--clean-output",
        "--clear-cache",
        "--log-level", "Warning",
    }) psi.ArgumentList.Add(a);

    using var proc = Process.Start(psi)!;
    proc.WaitForExit();
    return proc.ExitCode;
}

// ─── sync-docs ───────────────────────────────────────────────────────────────

int SyncDocs()
{
    if (!File.Exists(SpecPath))
    {
        Console.Error.WriteLine($"sync-docs: {SpecPath} not found. Run `generate` first.");
        return 1;
    }

    var spec = JsonNode.Parse(File.ReadAllText(SpecPath))!.AsObject();
    var domains = BuildDomains(spec);
    var total = domains.Sum(d => d.Count);

    var table = RenderDomainsTable(domains);
    var changed = false;
    changed |= ReplaceRegion("README.md", "<!-- BEGIN:DOMAINS -->", "<!-- END:DOMAINS -->", table, optional: true);
    changed |= ReplaceRegion("AGENTS.md", "<!-- BEGIN:DOMAINS -->", "<!-- END:DOMAINS -->", table, optional: true);

    // Method reference + its compile guard are rendered from the same call expressions:
    // the .g.cs file is compiled by the test project, so any example that would not
    // compile fails CI before it can ship in docs/llms-full.txt.
    var (methods, guard) = RenderMethods(spec);
    changed |= ReplaceRegion("docs/llms-full.txt", "<!-- BEGIN:METHODS -->", "<!-- END:METHODS -->", methods, optional: true);
    Directory.CreateDirectory("tests/RoxyApi.Tests/Generated");
    changed |= WriteIfChanged("tests/RoxyApi.Tests/Generated/MethodReferenceExamples.g.cs", guard);

    Console.WriteLine($"sync-docs: {domains.Count} domains, {total} endpoints. Docs {(changed ? "updated" : "unchanged")}.");
    return 0;
}

bool WriteIfChanged(string path, string content)
{
    if (File.Exists(path) && File.ReadAllText(path) == content) return false;
    File.WriteAllText(path, content);
    return true;
}

// One row per product domain, ordered by the spec's tag array (canonical brand
// order). The accessor is the PascalCased first path segment, which is exactly
// the top-level request builder Kiota generates (roxy.Astrology, roxy.VedicAstrology).
List<Domain> BuildDomains(JsonObject spec)
{
    var (opsBySegment, tagToSegment) = IndexBySegment(spec);
    var domains = new List<Domain>();
    foreach (var tagNode in spec["tags"]?.AsArray() ?? [])
    {
        var name = tagNode!["name"]!.GetValue<string>();
        if (!tagToSegment.TryGetValue(name, out var segment)) continue;
        domains.Add(new Domain(
            Accessor: $"roxy.{Pascal(segment)}",
            Count: opsBySegment[segment].Count,
            Summary: TagSummary(tagNode.AsObject())));
    }
    return domains;
}

// Walk the spec once: operations bucketed by URL first segment (document order),
// plus each tag mapped to its segment. The accessor for a domain is the PascalCased
// first segment, which is exactly the top-level request builder Kiota generates.
(Dictionary<string, List<(string path, string verb, JsonObject op)>> opsBySegment, Dictionary<string, string> tagToSegment)
    IndexBySegment(JsonObject spec)
{
    var opsBySegment = new Dictionary<string, List<(string, string, JsonObject)>>();
    var tagToSegment = new Dictionary<string, string>();
    foreach (var (path, pathItem) in spec["paths"]!.AsObject())
    {
        var segment = path.Trim('/').Split('/')[0];
        foreach (var (verb, opNode) in pathItem!.AsObject())
        {
            if (!IsHttpVerb(verb) || opNode is not JsonObject op) continue;
            if (!opsBySegment.TryGetValue(segment, out var list)) opsBySegment[segment] = list = [];
            list.Add((path, verb, op));
            var tag = op["tags"]?.AsArray().FirstOrDefault()?.GetValue<string>();
            if (tag is not null && !tagToSegment.ContainsKey(tag)) tagToSegment[tag] = segment;
        }
    }
    return (opsBySegment, tagToSegment);
}

string RenderDomainsTable(List<Domain> domains)
{
    var sb = new StringBuilder();
    sb.AppendLine("<!-- BEGIN:DOMAINS -->");
    sb.AppendLine("| Accessor | What it covers |");
    sb.AppendLine("|----------|----------------|");
    foreach (var d in domains)
        sb.AppendLine($"| `{d.Accessor}` | {d.Summary} |");
    sb.Append("<!-- END:DOMAINS -->");
    return sb.ToString();
}

// Short, dev-facing summary: the tag description up to its first sentence, capped.
string TagSummary(JsonObject tag)
{
    var desc = (tag["description"]?.GetValue<string>() ?? tag["name"]?.GetValue<string>() ?? "").Trim();
    if (desc.Length == 0) return "";
    var firstSentence = desc.Split(". ", 2)[0].Trim().TrimEnd('.');
    var flat = string.Join(' ', firstSentence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    return flat.Length > 120 ? flat[..117].TrimEnd() + "..." : flat;
}

bool IsHttpVerb(string v) => v is "get" or "post" or "put" or "delete" or "patch";

string Pascal(string s)
{
    var parts = s.Split(['-', '_', ' ', '.', '/'], StringSplitOptions.RemoveEmptyEntries);
    return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
}

// Swap the text between two markers in a file. optional=true skips a file that is
// missing (used before the docs are authored); otherwise a missing marker is fatal.
bool ReplaceRegion(string path, string begin, string end, string block, bool optional)
{
    if (!File.Exists(path))
    {
        if (optional) { Console.WriteLine($"sync-docs: {path} not present yet, skipping."); return false; }
        Console.Error.WriteLine($"sync-docs: {path} not found."); Environment.Exit(1);
    }
    var src = File.ReadAllText(path);
    var b = src.IndexOf(begin, StringComparison.Ordinal);
    var e = src.IndexOf(end, StringComparison.Ordinal);
    if (b < 0 || e < 0 || e < b)
    {
        Console.Error.WriteLine($"sync-docs: {path} is missing {begin} / {end} markers.");
        Environment.Exit(1);
    }
    var next = src[..b] + block + src[(e + end.Length)..];
    if (next == src) return false;
    File.WriteAllText(path, next);
    return true;
}

// ─── method reference rendering ──────────────────────────────────────────────

// Render the per-domain method reference for docs/llms-full.txt AND a parallel
// .g.cs compile guard from the SAME call expressions, so a non-compiling example
// fails the test build before it can ship in the docs.
(string methods, string guard) RenderMethods(JsonObject spec)
{
    var (opsBySegment, tagToSegment) = IndexBySegment(spec);

    var md = new StringBuilder("<!-- BEGIN:METHODS -->\n");
    var guard = new StringBuilder();
    guard.AppendLine("// <auto-generated/> Compile guard for the docs/llms-full.txt method examples.");
    guard.AppendLine("// Regenerated by `dotnet run --project tools/RoxyDevTools -- sync-docs`. Do not edit by hand.");
    guard.AppendLine("#pragma warning disable");
    guard.AppendLine("using Microsoft.Kiota.Abstractions;");
    guard.AppendLine("using RoxyApi;");
    guard.AppendLine("using RoxyApi.Models;");
    guard.AppendLine();
    guard.AppendLine("namespace RoxyApi.Tests.Generated;");
    guard.AppendLine();
    guard.AppendLine("internal static class MethodReferenceExamples");
    guard.AppendLine("{");
    guard.AppendLine("    public static async Task All(RoxyClient roxy)");
    guard.AppendLine("    {");

    var first = true;
    foreach (var tagNode in spec["tags"]?.AsArray() ?? [])
    {
        var tag = tagNode!["name"]!.GetValue<string>();
        if (!tagToSegment.TryGetValue(tag, out var seg)) continue;
        if (!first) md.Append("\n---\n\n");
        first = false;
        md.Append($"## {tag} - `roxy.{Pascal(seg)}`\n\n");
        var summary = TagSummary(tagNode.AsObject());
        if (summary.Length > 0) md.Append(summary).Append("\n\n");
        md.Append("```csharp\n");
        foreach (var (path, verb, op) in opsBySegment[seg])
        {
            var call = RenderCall(spec, path, verb, op);
            var opSummary = op["summary"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(opSummary)) md.Append("// ").Append(opSummary).Append('\n');
            md.Append("await ").Append(call).Append(";\n\n");
            guard.Append("        await ").Append(call).Append(";\n");
        }
        if (md[md.Length - 1] == '\n' && md[md.Length - 2] == '\n') md.Length--; // trim one trailing blank
        md.Append("```\n");
    }
    md.Append("<!-- END:METHODS -->");
    guard.AppendLine("    }");
    guard.AppendLine("}");
    return (md.ToString(), guard.ToString());
}

string RenderCall(JsonObject spec, string path, string verb, JsonObject op)
{
    var pathParams = (op["parameters"]?.AsArray() ?? [])
        .Select(p => (JsonObject)p!)
        .Where(p => p["in"]?.GetValue<string>() == "path")
        .ToDictionary(p => p["name"]!.GetValue<string>());

    var chain = new StringBuilder("roxy");
    foreach (var s in path.Trim('/').Split('/'))
    {
        if (s.StartsWith('{') && s.EndsWith('}'))
        {
            var name = s[1..^1];
            chain.Append('[').Append(pathParams.TryGetValue(name, out var pp) ? RenderParamValue(spec, pp) : "\"value\"").Append(']');
        }
        else chain.Append('.').Append(Pascal(s));
    }

    var method = char.ToUpperInvariant(verb[0]) + verb[1..] + "Async";
    var args = new List<string>();

    if (op["requestBody"]?["content"]?["application/json"]?["schema"] is JsonObject bodySchema)
        args.Add(RenderObject(spec, bodySchema, 1) ?? "new()");

    var queryParams = (op["parameters"]?.AsArray() ?? [])
        .Select(p => (JsonObject)p!)
        .Where(p => p["in"]?.GetValue<string>() == "query")
        .Where(p => p["name"]!.GetValue<string>() != "lang")
        .Where(p => p["schema"] is not JsonObject qs || !IsEnumLike(spec, qs))
        .Where(p => p["required"]?.GetValue<bool>() == true || ParamExample(spec, p) is not null)
        .ToList();
    if (queryParams.Count > 0)
    {
        var sets = queryParams.Select(p => $"c.QueryParameters.{Pascal(p["name"]!.GetValue<string>())} = {RenderParamValue(spec, p)}").ToList();
        args.Add(sets.Count == 1 ? $"c => {sets[0]}" : "c => { " + string.Join("; ", sets) + "; }");
    }

    return $"{chain}.{method}({string.Join(", ", args)})";
}

// Render an object schema as a target-typed `new() { ... }`. Includes required fields
// and any optional field carrying a spec example. Enum-typed fields are skipped: their
// example value alone does not let us name the generated enum member safely.
string? RenderObject(JsonObject spec, JsonObject schemaIn, int depth)
{
    if (depth > 5) return "new()";
    var schema = Deref(spec, schemaIn);
    if (schema["properties"] is not JsonObject props) return "new()";
    var required = (schema["required"]?.AsArray() ?? []).Select(x => x!.GetValue<string>()).ToHashSet();

    var parts = new List<string>();
    foreach (var (name, propNodeRaw) in props)
    {
        var propNode = (JsonObject)propNodeRaw!;
        var example = ExampleOf(spec, propNode);
        if (!required.Contains(name) && example is null) continue;
        var value = RenderValue(spec, propNode, example, depth + 1);
        if (value is not null) parts.Add($"{Pascal(name)} = {value}");
    }
    return parts.Count == 0 ? "new()" : "new() { " + string.Join(", ", parts) + " }";
}

string? RenderValue(JsonObject spec, JsonObject schemaIn, JsonNode? example, int depth, bool unionAsWrapper = true)
{
    if (depth > 6) return null;
    // Enum members cannot be named safely from an example value, so skip enum fields.
    if (IsEnumLike(spec, schemaIn)) return null;
    if (IsNumberStringUnion(spec, schemaIn))
    {
        // In a request body Kiota models number|string as a wrapper; as a query/path
        // parameter it collapses to a plain double.
        if (!unionAsWrapper)
            return example is JsonValue qv && qv.TryGetValue<double>(out var qd) ? FormatNumber(qd) : "0";
        if (example?.GetValueKind() == JsonValueKind.String) return $"new() {{ String = {Quote(example.GetValue<string>())} }}";
        if (example is JsonValue n && n.TryGetValue<double>(out var dv)) return $"new() {{ Double = {FormatNumber(dv)} }}";
        return "new() { Double = 5.5 }";
    }

    var schema = Deref(spec, schemaIn);
    var type = schema["type"]?.GetValue<string>();
    var format = schema["format"]?.GetValue<string>();

    if (type == "string" && format == "date")
    {
        var s = example?.GetValue<string>() ?? "1990-01-15";
        var p = s.Split('-');
        return p.Length == 3 && int.TryParse(p[0], out var y) && int.TryParse(p[1], out var m) && int.TryParse(p[2], out var d)
            ? $"new Date({y}, {m}, {d})" : "new Date(1990, 1, 15)";
    }
    if (type == "string" && format == "date-time")
        return $"DateTimeOffset.Parse({Quote(example?.GetValue<string>() ?? "2026-01-01T00:00:00Z")})";
    if (type == "object" || schema["properties"] is JsonObject)
        return RenderObject(spec, schema, depth);
    if (type == "array" && schema["items"] is JsonObject items)
    {
        var firstItem = (example as JsonArray)?.FirstOrDefault();
        var rendered = RenderValue(spec, items, firstItem ?? ExampleOf(spec, items), depth + 1);
        return rendered is null ? null : $"[{rendered}]"; // null item (e.g. enum array) skips the field
    }
    if (type is "integer" or "number")
        return example is JsonValue num && num.TryGetValue<double>(out var d2) ? FormatNumber(d2) : (type == "integer" ? "0" : "0.0");
    if (type == "boolean")
        return example is JsonValue b && b.TryGetValue<bool>(out var bb) ? (bb ? "true" : "false") : "false";
    return Quote(example?.GetValue<string>() ?? "string");
}

// Render a path or query parameter value. Path indexers and query properties are typed
// by Kiota from the parameter schema (a number path param becomes an int/double indexer,
// not a string), so render via the schema with the parameter union rule (no wrapper).
string RenderParamValue(JsonObject spec, JsonObject param)
{
    var example = ParamExample(spec, param);
    if (param["schema"] is JsonObject s && RenderValue(spec, s, example, 0, unionAsWrapper: false) is string v) return v;
    return Quote(example is JsonValue ev ? ev.ToString() : "value");
}

JsonObject Deref(JsonObject spec, JsonObject schema, int depth = 0)
{
    if (depth > 8) return schema;
    if (schema["$ref"]?.GetValue<string>() is string r)
    {
        JsonNode? node = spec;
        foreach (var key in r.TrimStart('#', '/').Split('/')) node = node?[key];
        if (node is JsonObject o) return Deref(spec, o, depth + 1);
    }
    if (schema["allOf"] is JsonArray all)
    {
        var props = new JsonObject();
        var required = new JsonArray();
        foreach (var part in all)
        {
            var d = Deref(spec, (JsonObject)part!, depth + 1);
            if (d["properties"] is JsonObject p) foreach (var (k, v) in p) props[k] = v?.DeepClone();
            if (d["required"] is JsonArray req) foreach (var x in req) required.Add(x!.GetValue<string>());
        }
        return new JsonObject { ["type"] = "object", ["properties"] = props, ["required"] = required };
    }
    return schema;
}

JsonNode? ExampleOf(JsonObject spec, JsonObject schema)
{
    if (schema["example"] is JsonNode e) return e;
    var d = Deref(spec, schema);
    if (d["example"] is JsonNode e2) return e2;
    if (d["enum"] is JsonArray en && en.Count > 0) return en[0];
    return null;
}

JsonNode? ParamExample(JsonObject spec, JsonObject param)
{
    if (param["example"] is JsonNode e) return e;
    return param["schema"] is JsonObject s ? ExampleOf(spec, s) : null;
}

// Enum-like = a direct enum, or an anyOf/oneOf whose branches are enums (e.g. houseSystem).
// Kiota generates a dedicated enum type for these; we cannot name its members from an example.
bool IsEnumLike(JsonObject spec, JsonObject schema)
{
    if (Deref(spec, schema)["enum"] is JsonArray) return true;
    if ((schema["anyOf"] ?? schema["oneOf"]) is JsonArray arr)
        return arr.Any(x => x is JsonObject o && Deref(spec, o)["enum"] is JsonArray);
    return false;
}

bool IsNumberStringUnion(JsonObject spec, JsonObject schema)
{
    if ((schema["anyOf"] ?? schema["oneOf"]) is not JsonArray arr) return false;
    var types = arr.Select(x => Deref(spec, (JsonObject)x!)["type"]?.GetValue<string>()).ToHashSet();
    return types.Contains("string") && (types.Contains("number") || types.Contains("integer"));
}

string FormatNumber(double d) =>
    d == Math.Truncate(d) && Math.Abs(d) < 1e15
        ? ((long)d).ToString(CultureInfo.InvariantCulture)
        : d.ToString("R", CultureInfo.InvariantCulture);

string Quote(string s) =>
    "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";

record Domain(string Accessor, int Count, string Summary);
