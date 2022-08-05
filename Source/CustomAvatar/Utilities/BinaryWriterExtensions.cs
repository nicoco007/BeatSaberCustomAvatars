//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System;
using System.IO;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class BinaryWriterExtensions
    {
        private static readonly float kMaxTextureSize = 256;

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
            if (texture == null || (!texture.isReadable && !forceReadable))
            {
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                return;
            }

            // this is a pretty expensive operation (few milliseconds) but since we only need to do it once (images
            // loaded from cache are always readable) and only do it when the game closes, it's not that bad
            if (!texture.isReadable || texture.width > kMaxTextureSize || texture.height > kMaxTextureSize)
            {
                float scale = Mathf.Min(1, kMaxTextureSize / texture.width, kMaxTextureSize / texture.height);
                int width = Mathf.RoundToInt(texture.width * scale);
                int height = Mathf.RoundToInt(texture.height * scale);
                var renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                RenderTexture.active = renderTexture;
                Graphics.Blit(texture, renderTexture);
                texture = renderTexture.GetTexture2D();
                texture.Compress(true);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(renderTexture);
            }

            byte[] textureBytes = texture.GetRawTextureData();

            writer.Write(texture.width);
            writer.Write(texture.height);
            writer.Write((int)texture.graphicsFormat);
            writer.Write(texture.mipmapCount);
            writer.Write(textureBytes.Length);
            writer.Write(textureBytes);
        }

        public static void Write(this BinaryWriter writer, DateTime dateTime)
        {
            writer.Write(dateTime.ToBinary());
        }
    }
}
