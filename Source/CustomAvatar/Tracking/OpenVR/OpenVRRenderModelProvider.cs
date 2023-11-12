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

using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CustomAvatar.Logging;
using Valve.VR;
using Zenject;

namespace CustomAvatar.Tracking.OpenVR
{
#pragma warning disable IDE0065
    using OpenVR = Valve.VR.OpenVR;
#pragma warning restore IDE0065

    internal class OpenVRRenderModelProvider : IRenderModelProvider, IInitializable
    {
        private static readonly uint kInputOriginInfoStructSize = (uint)Marshal.SizeOf(typeof(InputOriginInfo_t));

        private readonly ILogger<OpenVRRenderModelProvider> _logger;
        private readonly OpenVRRenderModelLoader _openVRRenderModelLoader;

        private readonly ulong[] _handles = new ulong[6];

        public OpenVRRenderModelProvider(ILogger<OpenVRRenderModelProvider> logger, OpenVRRenderModelLoader openVRRenderModelLoader)
        {
            _logger = logger;
            _openVRRenderModelLoader = openVRRenderModelLoader;
        }

        public void Initialize()
        {
            _handles[(int)DeviceUse.Head] = GetDeviceHandle(OpenVR.k_pchPathUserHead);
            _handles[(int)DeviceUse.LeftHand] = GetDeviceHandle(OpenVR.k_pchPathUserHandLeft);
            _handles[(int)DeviceUse.RightHand] = GetDeviceHandle(OpenVR.k_pchPathUserHandRight);
            _handles[(int)DeviceUse.Waist] = GetDeviceHandle(OpenVR.k_pchPathUserWaist);
            _handles[(int)DeviceUse.LeftFoot] = GetDeviceHandle(OpenVR.k_pchPathUserFootLeft);
            _handles[(int)DeviceUse.RightFoot] = GetDeviceHandle(OpenVR.k_pchPathUserFootRight);
        }

        public Task<RenderModel> GetRenderModelAsync(DeviceUse deviceUse)
        {
            InputOriginInfo_t originInfo = GetOriginInfo(deviceUse);
            string renderModelName = GetStringTrackedDeviceProperty(originInfo.trackedDeviceIndex, ETrackedDeviceProperty.Prop_RenderModelName_String);

            if (string.IsNullOrEmpty(renderModelName))
            {
                return Task.FromResult<RenderModel>(null);
            }

            return _openVRRenderModelLoader.GetRenderModelAsync(renderModelName);
        }

        private ulong GetDeviceHandle(string devicePath)
        {
            ulong handle = 0;
            EVRInputError error = OpenVR.Input.GetInputSourceHandle(devicePath, ref handle);

            if (error != EVRInputError.None)
            {
                _logger.LogError($"Failed to get input source handle for '{devicePath}': {error}");
                return 0;
            }

            return handle;
        }

        private InputOriginInfo_t GetOriginInfo(DeviceUse deviceUse)
        {
            ulong handle = _handles[(int)deviceUse];

            if (handle == 0)
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
                throw new System.InvalidOperationException("OpenVR is not running");
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
