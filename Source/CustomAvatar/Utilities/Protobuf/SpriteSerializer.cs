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
