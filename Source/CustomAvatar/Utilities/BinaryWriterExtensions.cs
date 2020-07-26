using System;
using System.IO;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class BinaryWriterExtensions
    {
        public static void Write(this BinaryWriter writer, Pose pose)
        {
            writer.Write(pose.position);
            writer.Write(pose.rotation);
        }

        public static void Write(this BinaryWriter writer, Vector3 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }

        public static void Write(this BinaryWriter writer, Quaternion quaternion)
        {
            writer.Write(quaternion.x);
            writer.Write(quaternion.y);
            writer.Write(quaternion.z);
            writer.Write(quaternion.w);
        }

        public static void Write(this BinaryWriter writer, Texture2D texture, bool forceReadable)
        {
            byte[] textureBytes = BytesFromTexture2D(texture, forceReadable);

            writer.Write(textureBytes.Length);
            writer.Write(textureBytes);
        }

        public static void Write(this BinaryWriter writer, DateTime dateTime)
        {
            writer.Write(dateTime.ToBinary());
        }

        private static byte[] BytesFromTexture2D(Texture2D texture, bool forceReadable)
        {
            if (texture == null || (!texture.isReadable && !forceReadable)) return new byte[0];

            // create readable texture by rendering onto a RenderTexture
            if (!texture.isReadable)
            {
                RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                RenderTexture.active = renderTexture;
                Graphics.Blit(texture, renderTexture);
                texture = renderTexture.GetTexture2D();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
            
            return texture.EncodeToPNG();
        }
    }
}
