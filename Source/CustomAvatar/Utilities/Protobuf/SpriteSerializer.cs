//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using ProtoBuf;
using ProtoBuf.Serializers;
using UnityEngine;

namespace CustomAvatar.Utilities.Protobuf
{
    internal class SpriteSerializer : ISerializer<Sprite>
    {
        public SerializerFeatures Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;

        public Sprite Read(ref ProtoReader.State state, Sprite value)
        {
            Texture2D texture = null;

            int field;

            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch (field)
                {
                    case 1:
                        texture = state.ReadAny<Texture2D>();
                        break;
                }
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        public void Write(ref ProtoWriter.State state, Sprite value)
        {
            // TODO: if the sprite is part of an atlas, this won't work
            state.WriteAny(1, value.texture);
        }
    }
}
