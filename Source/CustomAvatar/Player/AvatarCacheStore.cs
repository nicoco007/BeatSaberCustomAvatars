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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Utilities;
using CustomAvatar.Utilities.Protobuf;
using ProtoBuf;
using ProtoBuf.Meta;
using UnityEngine;

namespace CustomAvatar.Player
{
    internal class AvatarCacheStore
    {
        private const string kCacheFileName = "cache.dat";

        private readonly RuntimeTypeModel _runtimeTypeModel;

        private AvatarInfoCollection _avatarInfoCollection = new();

        public AvatarCacheStore(string directory)
        {
            _runtimeTypeModel = RuntimeTypeModel.Create();
            _runtimeTypeModel.Add<Texture2D>().SerializerType = typeof(Texture2DSerializer);
            _runtimeTypeModel.Add<Sprite>().SerializerType = typeof(SpriteSerializer);

            this.directory = directory;
            this.cacheFilePath = Path.Join(directory, kCacheFileName);
        }

        public AvatarInfo this[string fileName]
        {
            get => _avatarInfoCollection.avatarInfos[fileName];
            set => _avatarInfoCollection.avatarInfos[fileName] = value;
        }

        public string directory { get; }

        public string cacheFilePath { get; }

        public void Load()
        {
            if (!File.Exists(cacheFilePath))
            {
                return;
            }

            using (FileStream fileStream = File.OpenRead(cacheFilePath))
            {
                _avatarInfoCollection = _runtimeTypeModel.Deserialize<AvatarInfoCollection>(fileStream);
            }

            Prune();
        }

        public void Save()
        {
            Prune();

            using (FileStream fileStream = File.OpenWrite(cacheFilePath))
            {
                _runtimeTypeModel.Serialize(fileStream, _avatarInfoCollection);
                fileStream.SetLength(fileStream.Position);
            }

            File.SetAttributes(cacheFilePath, FileAttributes.Hidden);
        }

        public bool TryGetValue(string fileName, out AvatarInfo avatarInfo)
        {
            return _avatarInfoCollection.avatarInfos.TryGetValue(fileName, out avatarInfo);
        }

        private void Prune()
        {
            foreach (KeyValuePair<string, AvatarInfo> kvp in _avatarInfoCollection.avatarInfos.ToList())
            {
                if (!PathHelpers.IsValidFileName(kvp.Key))
                {
                    _avatarInfoCollection.avatarInfos.Remove(kvp.Key);
                }

                string fullPath = Path.Join(directory, kvp.Key);

                if (!File.Exists(fullPath) || !kvp.Value.IsForFile(fullPath))
                {
                    _avatarInfoCollection.avatarInfos.Remove(kvp.Key);
                }
            }
        }

        [ProtoContract]
        private class AvatarInfoCollection
        {
            [ProtoMember(1)]
            internal Dictionary<string, AvatarInfo> avatarInfos { get; set; } = new();
        }
    }
}
