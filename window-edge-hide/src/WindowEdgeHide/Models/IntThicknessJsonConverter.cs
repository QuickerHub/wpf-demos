using System;
using Newtonsoft.Json;

namespace WindowEdgeHide.Models
{
    /// <summary>
    /// JSON converter for IntThickness
    /// Serializes as string format: "5", "5,6", or "1,2,3,4"
    /// </summary>
    public class IntThicknessJsonConverter : JsonConverter<IntThickness>
    {
        public override void WriteJson(JsonWriter writer, IntThickness value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override IntThickness ReadJson(JsonReader reader, Type objectType, IntThickness existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return new IntThickness(5);

            if (reader.TokenType == JsonToken.String)
            {
                string? value = reader.Value?.ToString();
                if (string.IsNullOrWhiteSpace(value))
                    return new IntThickness(5);

                try
                {
                    return IntThickness.Parse(value);
                }
                catch
                {
                    return new IntThickness(5);
                }
            }

            return new IntThickness(5);
        }
    }
}

