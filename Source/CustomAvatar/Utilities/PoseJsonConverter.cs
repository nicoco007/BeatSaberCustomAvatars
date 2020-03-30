using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class PoseJsonConverter : JsonConverter<Pose>
    {
        public override void WriteJson(JsonWriter writer, Pose value, JsonSerializer serializer)
        {
            var obj = new Dictionary<string, object>
            {
                { "position", value.position },
                { "rotation", value.rotation }
            };

            serializer.Serialize(writer, obj);
        }

        public override Pose ReadJson(JsonReader reader, Type objectType, Pose existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = serializer.Deserialize<JObject>(reader);

            if (obj == null) return Pose.identity;

            return new Pose(
                obj.GetValue("position")?.ToObject<Vector3>(serializer) ?? default,
                obj.GetValue("rotation")?.ToObject<Quaternion>(serializer) ?? default
            );
        }
    }
}