//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using CustomAvatar.Logging;
using IPA.Utilities;
using ProtoBuf;
using ProtoBuf.Meta;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Configuration
{
    internal class CalibrationData : IInitializable, IDisposable
    {
        public static readonly string kCalibrationDataFilePath = Path.Join(UnityGame.UserDataPath, "CustomAvatars.CalibrationData.dat");

        private readonly ILogger<CalibrationData> _logger;
        private readonly RuntimeTypeModel _runtimeTypeModel;

        private CalibrationState _calibrationState = new();

        public FullBodyCalibration calibration => _calibrationState.calibration;

        public FullBodyCalibration manualOffset => _calibrationState.manualOffset;

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

        private void Load()
        {
            if (!File.Exists(kCalibrationDataFilePath)) return;

            _logger.LogInformation($"Reading calibration data from '{kCalibrationDataFilePath}'");

            using (FileStream fileStream = File.OpenRead(kCalibrationDataFilePath))
            {
                _calibrationState = _runtimeTypeModel.Deserialize<CalibrationState>(fileStream);
            }
        }

        private void Save()
        {
            _logger.LogInformation($"Saving calibration data to '{kCalibrationDataFilePath}'");

            using (FileStream fileStream = File.OpenWrite(kCalibrationDataFilePath))
            {
                _runtimeTypeModel.Serialize(fileStream, _calibrationState);
                fileStream.SetLength(fileStream.Position);
            }

            File.SetAttributes(kCalibrationDataFilePath, FileAttributes.Hidden);
        }

        [ProtoContract]
        internal class CalibrationState
        {
            [ProtoMember(1)]
            internal FullBodyCalibration calibration { get; } = new FullBodyCalibration();

            [ProtoMember(3)]
            internal FullBodyCalibration manualOffset { get; } = new();
        }

        [ProtoContract]
        internal class FullBodyCalibration
        {
            [ProtoMember(2)]
            internal Pose waist { get; set; } = Pose.identity;

            [ProtoMember(3)]
            internal Pose leftFoot { get; set; } = Pose.identity;

            [ProtoMember(4)]
            internal Pose rightFoot { get; set; } = Pose.identity;

            internal bool isSet => !waist.Equals(Pose.identity) || !leftFoot.Equals(Pose.identity) || !rightFoot.Equals(Pose.identity);
        }
    }
}
