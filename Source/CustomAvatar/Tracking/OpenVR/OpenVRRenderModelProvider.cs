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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CustomAvatar.Logging;
using Valve.VR;

namespace CustomAvatar.Tracking.OpenVR
{
#pragma warning disable IDE0065
    using OpenVR = Valve.VR.OpenVR;
#pragma warning restore IDE0065

    internal class OpenVRRenderModelProvider : IRenderModelProvider
    {
        private static readonly uint kInputOriginInfoStructSize = (uint)Marshal.SizeOf(typeof(InputOriginInfo_t));

        private readonly ILogger<OpenVRRenderModelProvider> _logger;
        private readonly OpenVRRenderModelLoader _openVRRenderModelLoader;

        public OpenVRRenderModelProvider(ILogger<OpenVRRenderModelProvider> logger, OpenVRRenderModelLoader openVRRenderModelLoader)
        {
            _logger = logger;
            _openVRRenderModelLoader = openVRRenderModelLoader;
        }

        public Task<RenderModel> GetRenderModelAsync(DeviceUse deviceUse)
        {
            InputOriginInfo_t originInfo = GetOriginInfo(deviceUse);

            if (originInfo.devicePath == default)
            {
                return Task.FromResult<RenderModel>(null);
            }

            string renderModelName = GetStringTrackedDeviceProperty(originInfo.trackedDeviceIndex, ETrackedDeviceProperty.Prop_RenderModelName_String);

            if (string.IsNullOrEmpty(renderModelName))
            {
                return Task.FromResult<RenderModel>(null);
            }

            return _openVRRenderModelLoader.GetRenderModelAsync(renderModelName);
        }

        private ulong GetDeviceHandle(DeviceUse deviceUse)
        {
            if (OpenVR.Input == null)
            {
                return default;
            }

            string devicePath = deviceUse switch
            {
                DeviceUse.Head => OpenVR.k_pchPathUserHead,
                DeviceUse.LeftHand => OpenVR.k_pchPathUserHandLeft,
                DeviceUse.RightHand => OpenVR.k_pchPathUserHandRight,
                DeviceUse.Waist => OpenVR.k_pchPathUserWaist,
                DeviceUse.LeftFoot => OpenVR.k_pchPathUserFootLeft,
                DeviceUse.RightFoot => OpenVR.k_pchPathUserFootRight,
                _ => throw new ArgumentException("Invalid device use", nameof(deviceUse)),
            };

            ulong handle = 0;
            EVRInputError error = OpenVR.Input.GetInputSourceHandle(devicePath, ref handle);

            if (error != EVRInputError.None)
            {
                _logger.LogError($"Failed to get input source handle for '{devicePath}': {error}");
                return default;
            }

            return handle;
        }

        private InputOriginInfo_t GetOriginInfo(DeviceUse deviceUse)
        {
            if (OpenVR.Input == null)
            {
                return default;
            }

            ulong handle = GetDeviceHandle(deviceUse);

            if (handle == default)
            {
                return default;
            }

            InputOriginInfo_t originInfo = default;
            EVRInputError error = OpenVR.Input.GetOriginTrackedDeviceInfo(handle, ref originInfo, kInputOriginInfoStructSize);

            if (error is not EVRInputError.None)
            {
                if (error is not EVRInputError.NoData and not EVRInputError.InvalidHandle)
                {
                    _logger.LogError($"Failed to get origin tracked device info for {deviceUse} ({handle}): {error}");
                }

                return default;
            }

            return originInfo;
        }

        private static string GetStringTrackedDeviceProperty(uint deviceIndex, ETrackedDeviceProperty property)
        {
            if (OpenVR.System == null)
            {
                throw new InvalidOperationException("OpenVR is not running");
            }

            ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
            uint length = OpenVR.System.GetStringTrackedDeviceProperty(deviceIndex, property, null, 0, ref error);

            if (error == ETrackedPropertyError.TrackedProp_UnknownProperty)
            {
                return null;
            }

            if (error is not ETrackedPropertyError.TrackedProp_Success and not ETrackedPropertyError.TrackedProp_BufferTooSmall)
            {
                throw new OpenVRException($"Failed to get property '{property}' for device at index {deviceIndex}: {error}", property, error);
            }

            if (length > 0)
            {
                var stringBuilder = new StringBuilder((int)length);
                OpenVR.System.GetStringTrackedDeviceProperty(deviceIndex, property, stringBuilder, length, ref error);

                if (error != ETrackedPropertyError.TrackedProp_Success)
                {
                    throw new OpenVRException($"Failed to get property '{property}' for device at index {deviceIndex}: {error}", property, error);
                }

                return stringBuilder.ToString();
            }

            return null;
        }
    }
}
