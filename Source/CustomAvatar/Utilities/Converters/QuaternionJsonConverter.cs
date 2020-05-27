using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CustomAvatar.Utilities.Converters
{
    internal class QuaternionJsonConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            var obj = new JObject
            {
                {"x", value.x},
                {"y", value.y},
                {"z", value.z},
                {"w", value.w}
            };

            serializer.Serialize(writer, obj);
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = serializer.Deserialize<JObject>(reader);

            if (obj == null) return existingValue;

            float x = obj.Value<float>("x");
            float y = obj.Value<float>("y");
            float z = obj.Value<float>("z");
            float w = obj.Value<float>("w");

            // prevent null quaternion
            if (x == 0 && y == 0 && z == 0 && w == 0)
            {
                w = 1.0f;
            }

            return new Quaternion(x, y, z, w);
        }
    }
}
