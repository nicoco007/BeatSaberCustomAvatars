using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class Vector2JsonConverter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            var obj = new JObject
            {
                {"x", value.x},
                {"y", value.y}
            };

            serializer.Serialize(writer, obj);
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = serializer.Deserialize<JObject>(reader);

            if (obj == null) return Vector2.zero;

            return new Vector2(obj.Value<float>("x"), obj.Value<float>("y"));
        }
    }
}