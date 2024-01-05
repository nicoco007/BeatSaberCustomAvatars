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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Utilities;
using IPA.Utilities;
using UnityEngine;

namespace CustomAvatar.Configuration
{
    internal class CalibrationData : IDisposable
    {
        public static readonly string kCalibrationDataFilePath = Path.Combine(UnityGame.UserDataPath, "CustomAvatars.CalibrationData.dat");
        public static readonly byte[] kCalibrationDataFileSignature = { 0x43, 0x41, 0x63, 0x64 }; // Custom Avatars calibration data (CAcd)
        public static readonly byte kCalibrationDataFileVersion = 1;

        public FullBodyCalibration automaticCalibration { get; } = new FullBodyCalibration();

        private readonly Dictionary<string, FullBodyCalibration> _manualCalibration = new();

        private readonly ILogger<CalibrationData> _logger;

        public CalibrationData(ILogger<CalibrationData> logger)
        {
            _logger = logger;

            try
            {
                Load();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load calibration data");
                _logger.LogError(ex);
            }
        }

        public void Dispose()
        {
            try
            {
                Save();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save calibration data");
                _logger.LogError(ex);
            }
        }

        public FullBodyCalibration GetAvatarManualCalibration(string fileName)
        {
            if (!PathHelpers.IsValidFileName(fileName))
            {
                throw new ArgumentException("Invalid file name", nameof(fileName));
            }

            if (!_manualCalibration.ContainsKey(fileName))
            {
                _manualCalibration.Add(fileName, new FullBodyCalibration());
            }

            return _manualCalibration[fileName];
        }

        private void Load()
        {
            if (!File.Exists(kCalibrationDataFilePath)) return;

            _logger.LogInformation($"Reading calibration data from '{kCalibrationDataFilePath}'");

            using (var fileStream = new FileStream(kCalibrationDataFilePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fileStream, Encoding.UTF8))
            {
                if (!reader.ReadBytes(kCalibrationDataFileSignature.Length).SequenceEqual(kCalibrationDataFileSignature)) throw new IOException("Invalid file signature");

                if (reader.ReadByte() != kCalibrationDataFileVersion)
                {
                    _logger.LogWarning("Invalid file version");
                    return;
                }

                automaticCalibration.waist = reader.ReadPose();
                automaticCalibration.leftFoot = reader.ReadPose();
                automaticCalibration.rightFoot = reader.ReadPose();

                int count = reader.ReadInt32();

                _logger.LogTrace($"Reading {count} calibrations");

                for (int i = 0; i < count; i++)
                {
                    string fileName = reader.ReadString();

                    if (!PathHelpers.IsValidFileName(fileName))
                    {
                        _logger.LogWarning($"'{fileName}' is not a valid file name; skipped");
                        continue;
                    }

                    string fullPath = Path.Combine(PlayerAvatarManager.kCustomAvatarsPath, fileName);

                    if (!File.Exists(fullPath))
                    {
                        _logger.LogWarning($"'{fullPath}' no longer exists; skipped");
                        continue;
                    }

                    _logger.LogTrace($"Got calibration data for '{fileName}'");

                    if (!_manualCalibration.ContainsKey(fileName))
                    {
                        _manualCalibration.Add(fileName, new FullBodyCalibration());
                    }

                    _manualCalibration[fileName].waist = reader.ReadPose();
                    _manualCalibration[fileName].leftFoot = reader.ReadPose();
                    _manualCalibration[fileName].rightFoot = reader.ReadPose();
                }
            }
        }

        private void Save()
        {
            _logger.LogInformation($"Saving calibration data to '{kCalibrationDataFilePath}'");

            using (var fileStream = new FileStream(kCalibrationDataFilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (var writer = new BinaryWriter(fileStream, Encoding.UTF8, true))
                {
                    writer.Write(kCalibrationDataFileSignature);
                    writer.Write(kCalibrationDataFileVersion);

                    writer.Write(automaticCalibration.waist);
                    writer.Write(automaticCalibration.leftFoot);
                    writer.Write(automaticCalibration.rightFoot);

                    writer.Write(_manualCalibration.Count);

                    foreach (KeyValuePair<string, FullBodyCalibration> kvp in _manualCalibration)
                    {
                        writer.Write(kvp.Key); // file name

                        writer.Write(kvp.Value.waist);
                        writer.Write(kvp.Value.leftFoot);
                        writer.Write(kvp.Value.rightFoot);
                    }
                }

                fileStream.SetLength(fileStream.Position);
            }

            File.SetAttributes(kCalibrationDataFilePath, FileAttributes.Hidden);
        }

        public class FullBodyCalibration
        {
            public Pose waist = Pose.identity;
            public Pose leftFoot = Pose.identity;
            public Pose rightFoot = Pose.identity;

            public bool isCalibrated => !leftFoot.Equals(Pose.identity) || !rightFoot.Equals(Pose.identity) || !waist.Equals(Pose.identity);
        }
    }
}
