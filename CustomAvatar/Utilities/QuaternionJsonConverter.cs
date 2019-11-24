using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CustomAvatar.Utilities
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

            if (obj == null) return default;

            return new Quaternion(obj.Value<float>("x"), obj.Value<float>("y"), obj.Value<float>("z"), obj.Value<float>("w"));
        }
    }
}
