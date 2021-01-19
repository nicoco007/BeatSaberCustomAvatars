//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Configuration;
using Newtonsoft.Json;
using System;

namespace CustomAvatar.Utilities.Converters
{
    internal class ObservableValueJsonConverter<T> : JsonConverter<ObservableValue<T>>
    {
        public override void WriteJson(JsonWriter writer, ObservableValue<T> value, JsonSerializer serializer)
        {
            if (value == null) serializer.Serialize(writer, null);

            serializer.Serialize(writer, value.value);
        }

        public override ObservableValue<T> ReadJson(JsonReader reader, Type objectType, ObservableValue<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            T obj = serializer.Deserialize<T>(reader);

            if (existingValue != null)
            {
                existingValue.value = obj;
                return existingValue;
            }

            return new ObservableValue<T>(obj);
        }
    }
}
