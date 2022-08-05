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
using System.Runtime.InteropServices;
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
        public static Texture2D LoadImage(Stream textureStream, bool linear = false)
        {
            // DDS header: https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-header
            byte[] headerBytes = new byte[128];
            textureStream.Read(headerBytes, 0, headerBytes.Length);

            var handle = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
            DdsHeader header;

            try
            {
                header = Marshal.PtrToStructure<DdsHeader>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }

            if (header.signature != "DDS ")
            {
                throw new IOException("Invalid file signature");
            }

            if (header.size != 124)
            {
                throw new IOException("Invalid header length");
            }

            if (!header.flags.HasFlag(DdsFlags.Caps) || !header.flags.HasFlag(DdsFlags.Height) || !header.flags.HasFlag(DdsFlags.Width) || !header.flags.HasFlag(DdsFlags.PixelFormat))
            {
                throw new IOException("Invalid DDS header flags");
            }

            if (header.ddsPixelFormat.size != 32)
            {
                throw new IOException("Invalid pixel format length");
            }

            if (!header.ddsPixelFormat.flags.HasFlag(DdsPixelFormatFlags.FourCC))
            {
                throw new IOException("Only compressed textures are supported");
            }

            int mipMapCount = 0;

            if (header.flags.HasFlag(DdsFlags.MipMapCount))
            {
                mipMapCount = header.mipMapCount;
            }

            string fourCC = header.ddsPixelFormat.fourCC;
            TextureFormat textureFormat;

            switch (fourCC)
            {
                case "DXT1":
                    textureFormat = TextureFormat.DXT1;
                    break;

                case "DXT5":
                    textureFormat = TextureFormat.DXT5;
                    break;

                default:
                    throw new IOException($"Unsupported DDS compression format '{fourCC}'");
            }

            var texture = new Texture2D(header.width, header.height, textureFormat, mipMapCount, linear);

            byte[] textureBytes = new byte[textureStream.Length - headerBytes.Length];
            textureStream.Read(textureBytes, 0, textureBytes.Length);

            texture.LoadRawTextureData(textureBytes);
            texture.Apply();

            return texture;
        }

        [StructLayout(LayoutKind.Sequential, Size = 128, CharSet = CharSet.Ansi)]
        private struct DdsHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] private byte[] _signature;
            public int size;
            public DdsFlags flags;
            public int height;
            public int width;
            public int pitchOrLinearSize;
            public int depth;
            public int mipMapCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)] public int[] reserved;
            public DdsPixelFormat ddsPixelFormat;
            public int caps;
            public int caps2;
            public int caps3;
            public int caps4;
            public int reserved2;

            public string signature
            {
                get => Encoding.ASCII.GetString(_signature);
                set => _signature = Encoding.ASCII.GetBytes(value);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Size = 32)]
        private struct DdsPixelFormat
        {
            public int size;
            public DdsPixelFormatFlags flags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] private byte[] _fourCC;
            public int rgbBitCount;
            public int rBitMask;
            public int gBitMask;
            public int bBitMask;
            public int aBitMask;

            public string fourCC
            {
                get => Encoding.ASCII.GetString(_fourCC);
                set => _fourCC = Encoding.ASCII.GetBytes(value);
            }
        }

        [Flags]
        private enum DdsFlags : int
        {
            Caps = 0x1,
            Height = 0x2,
            Width = 0x4,
            Pitch = 0x8,
            PixelFormat = 0x1000,
            MipMapCount = 0x20000,
            LinearSize = 0x80000,
            Depth = 0x800000
        }

        [Flags]
        private enum DdsPixelFormatFlags : int
        {
            AlphaPixels = 0x1,
            Alpha = 0x2,
            FourCC = 0x4,
            RGB = 0x40,
            YUV = 0x200,
            Luminance = 0x20000
        }
    }
}
