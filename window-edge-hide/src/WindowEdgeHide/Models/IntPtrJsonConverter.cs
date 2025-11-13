using System;
using Newtonsoft.Json;

namespace WindowEdgeHide.Models
{
    /// <summary>
    /// JSON converter for IntPtr
    /// Serializes as long value
    /// </summary>
    public class IntPtrJsonConverter : JsonConverter<IntPtr>
    {
        public override void WriteJson(JsonWriter writer, IntPtr value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToInt64());
        }

        public override IntPtr ReadJson(JsonReader reader, Type objectType, IntPtr existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return IntPtr.Zero;

            if (reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.String)
            {
                long value = Convert.ToInt64(reader.Value);
                return new IntPtr(value);
            }

            return IntPtr.Zero;
        }
    }
}

