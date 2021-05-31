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

using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class SimpleDdsLoader
    {
        /// <summary>
        /// Loads a DDS file from a stream. Supports DXT1 and DXT5 compression.
        /// </summary>
        /// <param name="textureStream">Stream that contains the DDS file.</param>
        /// <returns>A new <see cref="Texture2D"/> containing the image.</returns>
        public static Texture2D LoadImage(Stream textureStream)
        {
            // DDS header: https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-header
            byte[] headerBytes = new byte[128];
            textureStream.Read(headerBytes, 0, headerBytes.Length);

            string magic = Encoding.ASCII.GetString(headerBytes, 0, 4);

            if (magic != "DDS ")
            {
                throw new IOException("Invalid file signature");
            }

            int size = BitConverter.ToInt32(headerBytes, 4);

            if (size != 124)
            {
                throw new IOException("Invalid header length");
            }

            int flags = BitConverter.ToInt32(headerBytes, 8);

            if ((flags & 0x1) == 0 || (flags & 0x2) == 0 || (flags & 0x4) == 0 || (flags & 0x1000) == 0)
            {
                throw new IOException("Invalid DDS header flags");
            }

            int height = BitConverter.ToInt32(headerBytes, 12);
            int width = BitConverter.ToInt32(headerBytes, 16);
            string format = Encoding.ASCII.GetString(headerBytes, 84, 4);

            bool hasMipMaps = (flags & 0x20000) != 0;
            TextureFormat textureFormat;

            switch (format)
            {
                case "DXT1":
                    textureFormat = TextureFormat.DXT1;
                    break;

                case "DXT5":
                    textureFormat = TextureFormat.DXT5;
                    break;

                default:
                    throw new IOException($"Unexpected DDS format '{format}'");
            }

            var texture = new Texture2D(width, height, textureFormat, hasMipMaps);

            byte[] textureBytes = new byte[textureStream.Length - headerBytes.Length];
            textureStream.Read(textureBytes, 0, textureBytes.Length);

            texture.LoadRawTextureData(textureBytes);
            texture.Apply();

            return texture;
        }
    }
}
