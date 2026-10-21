using System.Text.Json.Nodes;
using RoxyDevTools;

namespace RoxyApi.Tests;

// The generator derives the shared RoxyError schema from the error responses of the
// document (tools/RoxyDevTools/ErrorSchema.cs). A new field must appear without an edit
// to the tool, a type disagreement must fail loudly, and a normalized document must
// derive to itself so a regenerate is byte-stable.
public class ErrorSchemaTests
{
    private static JsonObject Doc(params (string Status, string Schema)[] responses)
    {
        var doc = new JsonObject { ["paths"] = new JsonObject(), ["components"] = new JsonObject { ["schemas"] = new JsonObject() } };
        for (var i = 0; i < responses.Length; i++)
        {
            doc["paths"]![$"/p{i}"] = new JsonObject
            {
                ["get"] = new JsonObject
                {
                    ["responses"] = new JsonObject
                    {
                        [responses[i].Status] = new JsonObject
                        {
                            ["content"] = new JsonObject
                            {
                                ["application/json"] = new JsonObject { ["schema"] = JsonNode.Parse(responses[i].Schema) },
                            },
                        },
                    },
                },
            };
        }
        return doc;
    }

    [Fact]
    public void A_field_that_only_one_response_sends_joins_the_schema()
    {
        var doc = Doc(
            ("400", """{"type":"object","required":["error","code"],"properties":{"error":{"type":"string"},"code":{"type":"string"},"issues":{"type":"array","items":{"type":"object","properties":{"path":{"type":"string"},"hint":{"type":"string"}}}}}}"""),
            ("429", """{"type":"object","required":["error"],"properties":{"error":{"type":"string"},"code":{"type":"string"},"retry_hint":{"type":"integer","description":"Seconds."}}}"""));

        var schemas = ErrorSchema.Derive(doc);

        var error = schemas["RoxyError"];
        Assert.Equal(["code", "error", "issues", "retry_hint"], error["properties"]!.AsObject().Select(p => p.Key));
        Assert.Equal("integer", error["properties"]!["retry_hint"]!["type"]!.GetValue<string>());
        Assert.Equal("#/components/schemas/RoxyErrorIssue", error["properties"]!["issues"]!["items"]!["$ref"]!.GetValue<string>());
        Assert.Equal(["code", "error"], error["required"]!.AsArray().Select(r => r!.GetValue<string>()));
        Assert.True(error["properties"]!["error"]!["x-ms-primary-error-message"]!.GetValue<bool>());
        Assert.Contains("hint", schemas["RoxyErrorIssue"]["properties"]!.AsObject().Select(p => p.Key));
    }

    [Fact]
    public void A_type_conflict_between_responses_fails_loudly()
    {
        var doc = Doc(
            ("400", """{"type":"object","properties":{"error":{"type":"string"},"code":{"type":"string"}}}"""),
            ("500", """{"type":"object","properties":{"error":{"type":"string"},"code":{"type":"integer"}}}"""));

        var ex = Assert.Throws<InvalidOperationException>(() => ErrorSchema.Derive(doc));

        Assert.Contains("`code`", ex.Message);
    }

    [Fact]
    public void A_normalized_document_derives_to_itself()
    {
        var spec = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "specs", "openapi.json")))!.AsObject();
        var schemas = spec["components"]!["schemas"]!;

        var derived = ErrorSchema.Derive(spec);

        Assert.Equal(["RoxyError", "RoxyErrorIssue"], derived.Keys.Order());
        foreach (var (name, schema) in derived)
            Assert.Equal(schemas[name]!.ToJsonString(), schema.ToJsonString());
    }

    [Fact]
    public void A_response_that_omits_code_from_required_still_yields_error_and_code_required_and_a_fixed_point()
    {
        var doc = Doc(
            ("400", """{"type":"object","required":["error","code"],"properties":{"error":{"type":"string"},"code":{"type":"string"}}}"""),
            ("404", """{"type":"object","required":["error"],"properties":{"error":{"type":"string"},"code":{"type":"string"},"docs":{"type":"string"}}}"""));

        var first = ErrorSchema.Derive(doc)["RoxyError"];
        Assert.Equal(["code", "error"], first["required"]!.AsArray().Select(r => r!.GetValue<string>()));

        var normalized = Doc(("400", """{"$ref":"#/components/schemas/RoxyError"}"""));
        normalized["components"]!["schemas"]!["RoxyError"] = JsonNode.Parse(first.ToJsonString());
        Assert.Equal(first.ToJsonString(), ErrorSchema.Derive(normalized)["RoxyError"].ToJsonString());
    }
}
