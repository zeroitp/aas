namespace AasxServerDB.Helpers;
using System;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;

public class JsonNodeConverter : JsonConverter<JsonNode>
{
    public override JsonNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            return JsonNode.Parse(doc.RootElement.GetRawText());
        }
    }

    public override void Write(Utf8JsonWriter writer, JsonNode value, JsonSerializerOptions options)
    {
        value.WriteTo(writer);
    }
}
