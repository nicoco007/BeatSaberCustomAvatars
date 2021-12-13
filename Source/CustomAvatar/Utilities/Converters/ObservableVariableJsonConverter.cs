//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CustomAvatar.Configuration;
using Newtonsoft.Json;
using System;

namespace CustomAvatar.Utilities.Converters
{
    internal class ObservableValueJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (!objectType.IsGenericType) return false;

            return objectType.GetGenericTypeDefinition() == typeof(ObservableValue<>);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null) serializer.Serialize(writer, null);

            serializer.Serialize(writer, value.GetType().GetProperty("value").GetValue(value));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Type valueType = objectType.GenericTypeArguments[0];

            object obj = serializer.Deserialize(reader, valueType);

            if (existingValue != null)
            {
                objectType.GetProperty("value").SetValue(existingValue, obj);
                return existingValue;
            }

            return Activator.CreateInstance(objectType, obj);
        }
    }
}
