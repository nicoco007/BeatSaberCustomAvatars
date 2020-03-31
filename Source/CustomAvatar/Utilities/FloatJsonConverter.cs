using System;
using System.Globalization;
using Newtonsoft.Json;

namespace CustomAvatar.Utilities
{
    internal class FloatJsonConverter : JsonConverter<float>
    {
        public override void WriteJson(JsonWriter writer, float value, JsonSerializer serializer)
        {
            writer.WriteRawValue(value.ToString("0.######", CultureInfo.InvariantCulture));
        }

        public override bool CanRead => false;

        public override float ReadJson(JsonReader reader, Type objectType, float existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new InvalidOperationException();
        }
    }
}
