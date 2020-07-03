using Newtonsoft.Json;
using System;
using UnityEngine;

namespace CustomAvatar.Utilities.Converters
{
    internal class Texture2DConverter : JsonConverter<Texture2D>
    {
        public override void WriteJson(JsonWriter writer, Texture2D value, JsonSerializer serializer)
        {
            if (value == null) writer.WriteNull();

            // work around unreadable textures
            if (!value.isReadable)
            {
                RenderTexture texture = new RenderTexture(value.width, value.height, 0, RenderTextureFormat.ARGB32);
                RenderTexture.active = texture;
                Graphics.Blit(value, texture);
                value = texture.GetTexture2D();
                texture.Release();
            }

            serializer.Serialize(writer, value.EncodeToPNG());
        }

        public override Texture2D ReadJson(JsonReader reader, Type objectType, Texture2D existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            byte[] bytes = serializer.Deserialize<byte[]>(reader);

            if (bytes != null)
            {
                Texture2D texture = new Texture2D(0, 0);
                texture.LoadImage(bytes);
                return texture;
            }
            else if (hasExistingValue)
            {
                return existingValue;
            }
            else
            {
                return null;
            }
        }
    }
}
