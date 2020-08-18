//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
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
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class BinaryReaderExtensions
    {
        public static Pose ReadPose(this BinaryReader reader)
        {
            return new Pose(reader.ReadVector3(), reader.ReadQuaternion());
        }
        
        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
        
        public static Quaternion ReadQuaternion(this BinaryReader reader)
        {
            return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static Texture2D ReadTexture2D(this BinaryReader reader)
        {
            return BytesToTexture2D(reader.ReadBytes(reader.ReadInt32()));
        }

        public static DateTime ReadDateTime(this BinaryReader reader)
        {
            return DateTime.FromBinary(reader.ReadInt64());
        }

        private static Texture2D BytesToTexture2D(byte[] bytes)
        {
            if (bytes.Length == 0) return null;

            Texture2D texture = new Texture2D(0, 0, TextureFormat.ARGB32, false);

            texture.LoadImage(bytes);

            return texture;
        }
    }
}
