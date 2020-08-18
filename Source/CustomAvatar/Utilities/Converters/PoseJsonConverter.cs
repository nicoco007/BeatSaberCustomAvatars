//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CustomAvatar.Utilities.Converters
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

            if (obj == null) return existingValue;

            return new Pose(
                obj.GetValue("position")?.ToObject<Vector3>(serializer) ?? default,
                obj.GetValue("rotation")?.ToObject<Quaternion>(serializer) ?? default
            );
        }
    }
}
