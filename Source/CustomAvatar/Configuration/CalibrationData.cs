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
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Utilities;
using IPA.Utilities;
using ProtoBuf;
using ProtoBuf.Meta;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Configuration
{
    internal class CalibrationData : IInitializable, IDisposable
    {
        public static readonly string kCalibrationDataFilePath = Path.Combine(UnityGame.UserDataPath, "CustomAvatars.CalibrationData.dat");

        private readonly ILogger<CalibrationData> _logger;
        private readonly RuntimeTypeModel _runtimeTypeModel;

        private CalibrationState _calibrationState = new();

        public FullBodyCalibration automaticCalibration => _calibrationState.automaticCalibration;

        private CalibrationData(ILogger<CalibrationData> logger)
        {
            _logger = logger;
            _runtimeTypeModel = RuntimeTypeModel.Create(nameof(CalibrationData));
            _runtimeTypeModel.Add<Pose>().Add(nameof(Pose.position), nameof(Pose.rotation));
            _runtimeTypeModel.Add<Vector3>().Add(nameof(Vector3.x), nameof(Vector3.y), nameof(Vector3.z));
            _runtimeTypeModel.Add<Quaternion>().Add(nameof(Quaternion.x), nameof(Quaternion.y), nameof(Quaternion.z), nameof(Quaternion.w));
        }

        public void Initialize()
        {
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

            if (!_calibrationState.manualCalibrations.TryGetValue(fileName, out FullBodyCalibration fullBodyCalibration))
            {
                fullBodyCalibration = new FullBodyCalibration();
                _calibrationState.manualCalibrations.Add(fileName, fullBodyCalibration);
            }

            return fullBodyCalibration;
        }

        private void Load()
        {
            if (!File.Exists(kCalibrationDataFilePath)) return;

            _logger.LogInformation($"Reading calibration data from '{kCalibrationDataFilePath}'");

            using (FileStream fileStream = File.OpenRead(kCalibrationDataFilePath))
            {
                _calibrationState = _runtimeTypeModel.Deserialize<CalibrationState>(fileStream);
            }

            Prune();
        }

        private void Save()
        {
            _logger.LogInformation($"Saving calibration data to '{kCalibrationDataFilePath}'");

            Prune();

            using (FileStream fileStream = File.OpenWrite(kCalibrationDataFilePath))
            {
                _runtimeTypeModel.Serialize(fileStream, _calibrationState);
                fileStream.SetLength(fileStream.Position);
            }

            File.SetAttributes(kCalibrationDataFilePath, FileAttributes.Hidden);
        }

        private void Prune()
        {
            foreach (string fileName in _calibrationState.manualCalibrations.Keys.ToList())
            {
                if (!PathHelpers.IsValidFileName(fileName))
                {
                    _logger.LogTrace($"'{fileName}' is not a valid file name");
                    _calibrationState.manualCalibrations.Remove(fileName);
                }

                string fullPath = Path.Combine(PlayerAvatarManager.kCustomAvatarsPath, fileName);

                if (!File.Exists(fullPath))
                {
                    _logger.LogTrace($"'{fileName}' no longer exists");
                    _calibrationState.manualCalibrations.Remove(fileName);
                }
            }
        }

        [ProtoContract]
        internal class CalibrationState
        {
            [ProtoMember(1)]
            internal FullBodyCalibration automaticCalibration { get; } = new FullBodyCalibration();

            [ProtoMember(2)]
            internal Dictionary<string, FullBodyCalibration> manualCalibrations { get; set; } = new();
        }

        [ProtoContract]
        internal class FullBodyCalibration
        {
            [ProtoMember(1)]
            internal Pose head { get; set; } = Pose.identity;

            [ProtoMember(2)]
            internal Pose waist { get; set; } = Pose.identity;

            [ProtoMember(3)]
            internal Pose leftFoot { get; set; } = Pose.identity;

            [ProtoMember(4)]
            internal Pose rightFoot { get; set; } = Pose.identity;

            internal bool isCalibrated => !head.Equals(Pose.identity) || !waist.Equals(Pose.identity) || !leftFoot.Equals(Pose.identity) || !rightFoot.Equals(Pose.identity);
        }
    }
}
