using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CustomAvatar.Configuration
{
    internal class CalibrationData : IDisposable
    {
        public static readonly string kCalibrationDataFilePath = Path.Combine(Path.GetFullPath("UserData"), "CustomAvatars.CalibrationData.dat");
        public static readonly byte[] kCalibrationDataFileSignature = { 0x43, 0x41, 0x63, 0x64 }; // Custom Avatars calibration data (CAcd)
        public static readonly byte kCalibrationDataFileVersion = 1;

        public FullBodyCalibration automaticCalibration { get; private set; } = new FullBodyCalibration();
        public Dictionary<string, FullBodyCalibration> manualCalibration { get; private set; } = new Dictionary<string, FullBodyCalibration>();

        private ILogger<CalibrationData> _logger;

        public CalibrationData(ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.CreateLogger<CalibrationData>();

            try
            {
                Load();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load calibration data");
                _logger.Error(ex);
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
                _logger.Error("Failed to save calibration data");
                _logger.Error(ex);
            }
        }

        public FullBodyCalibration GetAvatarManualCalibration(string fileName)
        {
            if (!IsValidFileName(fileName)) throw new ArgumentException("Invalid file name", nameof(fileName));

            if (!manualCalibration.ContainsKey(fileName))
            {
                manualCalibration.Add(fileName, new FullBodyCalibration());
            }

            return manualCalibration[fileName];
        }

        private void Load()
        {
            if (!File.Exists(kCalibrationDataFilePath)) return;
            
            _logger.Info($"Reading calibration data from '{kCalibrationDataFilePath}'");

            using (var fileStream = new FileStream(kCalibrationDataFilePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fileStream))
            {
                if (!reader.ReadBytes(kCalibrationDataFileSignature.Length).SequenceEqual(kCalibrationDataFileSignature)) throw new IOException("Invalid file signature");

                if (reader.ReadByte() != kCalibrationDataFileVersion)
                {
                    _logger.Warning("Invalid file version");
                    return;
                }

                automaticCalibration.waist     = reader.ReadPose();
                automaticCalibration.leftFoot  = reader.ReadPose();
                automaticCalibration.rightFoot = reader.ReadPose();

                int count = reader.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    string fileName = reader.ReadString();

                    if (!IsValidFileName(fileName))
                    {
                        _logger.Warning($"'{fileName}' is not a valid file name; skipped");
                    }

                    string fullPath = Path.Combine(PlayerAvatarManager.kCustomAvatarsPath, fileName);

                    if (!File.Exists(fullPath))
                    {
                        _logger.Warning($"'{fullPath}' no longer exists; skipped");
                    }

                    if (!manualCalibration.ContainsKey(fileName))
                    {
                        manualCalibration.Add(fileName, new FullBodyCalibration());
                    }

                    manualCalibration[fileName].waist     = reader.ReadPose();
                    manualCalibration[fileName].leftFoot  = reader.ReadPose();
                    manualCalibration[fileName].rightFoot = reader.ReadPose();
                }
            }
        }

        private void Save()
        {
            _logger.Info($"Saving calibration data to '{kCalibrationDataFilePath}'");

            using (var fileStream = new FileStream(kCalibrationDataFilePath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fileStream))
            {
                writer.Write(kCalibrationDataFileSignature);
                writer.Write(kCalibrationDataFileVersion);
                
                writer.Write(automaticCalibration.waist);
                writer.Write(automaticCalibration.leftFoot);
                writer.Write(automaticCalibration.rightFoot);

                writer.Write(manualCalibration.Count);

                foreach (KeyValuePair<string, FullBodyCalibration> kvp in manualCalibration)
                {
                    writer.Write(kvp.Key); // file name

                    writer.Write(kvp.Value.waist);
                    writer.Write(kvp.Value.leftFoot);
                    writer.Write(kvp.Value.rightFoot);
                }
            }
        }

        private bool IsValidFileName(string str)
        {
            return !str.Any(c => Path.GetInvalidFileNameChars().Contains(c));
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
