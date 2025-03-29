//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using ProtoBuf;
using ProtoBuf.Serializers;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace CustomAvatar.Utilities.Protobuf
{
    internal class Texture2DSerializer : ISerializer<Texture2D>
    {
        private const int kMaxTextureSize = 256;

        public SerializerFeatures Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;

        public Texture2D Read(ref ProtoReader.State state, Texture2D value)
        {
            int width = default;
            int height = default;
            GraphicsFormat graphicsFormat = default;
            int mipmapCount = default;
            byte[] textureBytes = Array.Empty<byte>();
            TextureWrapMode wrapModeU = default;
            TextureWrapMode wrapModeV = default;

            int field;

            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch (field)
                {
                    case 1:
                        width = state.ReadInt32();
                        break;

                    case 2:
                        height = state.ReadInt32();
                        break;

                    case 3:
                        graphicsFormat = (GraphicsFormat)state.ReadInt32();
                        break;

                    case 4:
                        mipmapCount = state.ReadInt32();
                        break;

                    case 5:
                        textureBytes = state.AppendBytes(null);
                        break;

                    case 6:
                        wrapModeU = (TextureWrapMode)state.ReadInt32();
                        break;

                    case 7:
                        wrapModeV = (TextureWrapMode)state.ReadInt32();
                        break;
                }
            }

            // TODO: who's responsible for this object's lifecycle? currently relies on Resources.UnloadUnusedAssets to get cleaned up
            value = new Texture2D(width, height, graphicsFormat, mipmapCount, TextureCreationFlags.None)
            {
                wrapModeU = wrapModeU,
                wrapModeV = wrapModeV,
            };

            value.LoadRawTextureData(textureBytes);
            value.Apply(false, false);

            return value;
        }

        public void Write(ref ProtoWriter.State state, Texture2D value)
        {
            int width = value.width;
            int height = value.height;
            int mipmapCount = value.mipmapCount;
            GraphicsFormat graphicsFormat = value.graphicsFormat;
            byte[] textureBytes;

            // TODO: this conversion should be done somewhere else (maybe during avatar load?)
            if (!value.isReadable || width > kMaxTextureSize || height > kMaxTextureSize)
            {
                if (SystemInfo.IsFormatSupported(value.graphicsFormat, FormatUsage.ReadPixels) && width <= kMaxTextureSize && height <= kMaxTextureSize)
                {
                    textureBytes = FetchTextureDataSync(value);
                }
                else
                {
                    float scale = Mathf.Min(1, (float)kMaxTextureSize / value.width, (float)kMaxTextureSize / value.height);
                    width = Mathf.RoundToInt(value.width * scale);
                    height = Mathf.RoundToInt(value.height * scale);
                    mipmapCount = Math.Min(value.mipmapCount, (int)Math.Ceiling(Log2(Math.Max(width, height))) + 1);
                    graphicsFormat = SystemInfo.GetCompatibleFormat(value.graphicsFormat, FormatUsage.Render);

                    RenderTextureDescriptor renderTextureDescriptor = new(width, height)
                    {
                        graphicsFormat = graphicsFormat,
                        mipCount = mipmapCount,
                        useMipMap = value.mipmapCount > 1,
                    };

                    var renderTexture = RenderTexture.GetTemporary(renderTextureDescriptor);

                    RenderTexture previous = RenderTexture.active;
                    Graphics.Blit(value, renderTexture);
                    RenderTexture.active = previous;

                    textureBytes = FetchTextureDataSync(renderTexture);

                    RenderTexture.ReleaseTemporary(renderTexture);
                }
            }
            else
            {
                textureBytes = value.GetRawTextureData();
            }

            // ideally we'd compress the texture at this point but Texture2D.Compress looks absolutely horrendous, even with highQuality set to true

            state.WriteFieldHeader(1, WireType.Varint);
            state.WriteInt32(width);

            state.WriteFieldHeader(2, WireType.Varint);
            state.WriteInt32(height);

            state.WriteFieldHeader(3, WireType.Varint);
            state.WriteInt32((int)graphicsFormat);

            state.WriteFieldHeader(4, WireType.Varint);
            state.WriteInt32(mipmapCount);

            state.WriteFieldHeader(5, WireType.String);
            state.WriteBytes(textureBytes);

            state.WriteFieldHeader(6, WireType.Varint);
            state.WriteInt32((int)value.wrapModeU);

            state.WriteFieldHeader(7, WireType.Varint);
            state.WriteInt32((int)value.wrapModeV);
        }

        private byte[] FetchTextureDataSync(Texture texture)
        {
            uint totalSize = 0;
            var requests = new AsyncGPUReadbackRequest[texture.mipmapCount];

            for (int i = 0; i < texture.mipmapCount; i++)
            {
                requests[i] = AsyncGPUReadback.Request(texture, i);

                int divisor = (int)Math.Pow(2, i);
                int targetWidth = Mathf.Max(1, texture.width / divisor);
                int targetHeight = Mathf.Max(1, texture.height / divisor);
                totalSize += GraphicsFormatUtility.ComputeMipmapSize(targetWidth, targetHeight, texture.graphicsFormat);
            }

            int offset = 0;
            byte[] textureBytes = new byte[totalSize];

            for (int i = 0; i < texture.mipmapCount; i++)
            {
                requests[i].WaitForCompletion();
                NativeArray<byte> data = requests[i].GetData<byte>();
                NativeArray<byte>.Copy(data, 0, textureBytes, offset, data.Length);
                data.Dispose();
                offset += data.Length;
            }

            return textureBytes;
        }

        private double Log2(double d) => Math.Log(d) / Math.Log(2);
    }
}
