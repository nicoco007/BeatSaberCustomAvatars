using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CustomAvatar.Utilities.Converters
{
    internal class ColorJsonConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            var obj = new JObject
            {
                {"r", value.r},
                {"g", value.g},
                {"b", value.b}
            };

            serializer.Serialize(writer, obj);
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = serializer.Deserialize<JObject>(reader);

            if (obj == null) return existingValue;

            return new Color(obj.Value<float>("r"), obj.Value<float>("g"), obj.Value<float>("b"));
        }
    }
}
