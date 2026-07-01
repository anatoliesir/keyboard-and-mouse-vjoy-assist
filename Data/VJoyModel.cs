using System;
using System.Runtime.InteropServices;
using MouseToVJoy.Data;

namespace MouseToVJoy.Data
{
    public class VJoyModel
    {
        [DllImport("vJoyInterface.dll", EntryPoint = "vJoyEnabled")]
        private static extern bool vJoyEnabled();

        [DllImport("vJoyInterface.dll", EntryPoint = "GetVJDStatus")]
        private static extern int GetVJDStatus(uint rID);

        [DllImport("vJoyInterface.dll", EntryPoint = "AcquireVJD")]
        private static extern bool AcquireVJD(uint rID);

        [DllImport("vJoyInterface.dll", EntryPoint = "RelinquishVJD")]
        private static extern void RelinquishVJD(uint rID);

        [DllImport("vJoyInterface.dll", EntryPoint = "UpdateVJD")]
        private static extern bool UpdateVJD(uint rID, ref JoystickState pData);

        [DllImport("vJoyInterface.dll", EntryPoint = "ResetVJD")]
        private static extern bool ResetVJD(uint rID);

        [DllImport("vJoyInterface.dll", EntryPoint = "GetVJDAxisExist")]
        private static extern bool GetVJDAxisExist(uint rID, uint axis);

        // This layout must match the vJoy SDK memory layout.
        [StructLayout(LayoutKind.Sequential)]
        public struct JoystickState
        {
            public byte bDevice;
            public int Throttle;
            public int Rudder;
            public int Aileron;
            public int AxisX;
            public int AxisY;
            public int AxisZ;
            public int AxisXRot;
            public int AxisYRot;
            public int AxisZRot;
            public int Slider;
            public int Dial;
            public int Wheel;
            public int AxisVX;
            public int AxisVY;
            public int AxisVZ;
            public int AxisVBRX;
            public int AxisVBRY;
            public int AxisVBRZ;
            public int Buttons;
            public uint bHats;
            public uint bHatsEx1;
            public uint bHatsEx2;
            public uint bHatsEx3;
            public uint ButtonsEx1;
            public uint ButtonsEx2;
            public uint ButtonsEx3;
        }

        private const int VJD_STAT_FREE = 1;
        private const int VJD_STAT_OWN = 0;
        private const uint HID_USAGE_Y = 0x31;
        private const uint HID_USAGE_Z = 0x32;
        private const uint HID_USAGE_RZ = 0x35;
        private readonly uint _deviceId = 1;
        private JoystickState _state;
        private bool _isAcquired;

        public string LastError { get; private set; } = string.Empty;
        public string LastWarning { get; private set; } = string.Empty;

        public bool Initialize()
        {
            try
            {
                LastError = string.Empty;
                LastWarning = string.Empty;
                if (!vJoyEnabled())
                {
                    LastError = "vJoy is not enabled or the driver is not installed.";
                    return false;
                }

                int status = GetVJDStatus(_deviceId);
                if (status == VJD_STAT_FREE)
                {
                    _isAcquired = AcquireVJD(_deviceId);
                }
                else if (status == VJD_STAT_OWN)
                {
                    _isAcquired = true;
                }
                else
                {
                    LastError = $"vJoy device #{_deviceId} is not available. Status: {status}.";
                    return false;
                }

                if (_isAcquired)
                {
                    ResetVJD(_deviceId);
                    _state = CreateCenteredState(_deviceId);
                    ValidatePedalAxes();

                    if (UpdateVJD(_deviceId, ref _state))
                    {
                        return true;
                    }

                    LastError = "vJoy rejected the initial axis state.";
                    return false;
                }

                LastError = $"Could not acquire vJoy device #{_deviceId}.";
                return _isAcquired;
            }
            catch (DllNotFoundException)
            {
                LastError = "Could not find vJoyInterface.dll next to the executable.";
                return false;
            }
            catch (BadImageFormatException)
            {
                LastError = "vJoyInterface.dll has an incompatible architecture for this application.";
                return false;
            }
            catch (EntryPointNotFoundException ex)
            {
                LastError = $"A required vJoy DLL function is missing: {ex.Message}";
                return false;
            }
        }

        public void UpdateAxes(int wheel, int throttle, int brake)
        {
            if (!_isAcquired) return;

            int wheelValue = ClampAxis(wheel);
            int throttleValue = ClampAxis(throttle);
            int brakeValue = ClampAxis(brake);

            _state.AxisX = wheelValue;

            // Assetto Corsa/DirectInput setups may expose either generic axes
            // or named vJoy axes, depending on how the virtual device is configured.
            _state.AxisY = throttleValue;
            _state.Throttle = throttleValue;

            _state.AxisZ = brakeValue;
            _state.Rudder = brakeValue;
            _state.AxisZRot = brakeValue;

            if (!UpdateVJD(_deviceId, ref _state))
            {
                LastError = "vJoy rejected the latest axis update.";
            }
        }

        public void Disconnect()
        {
            if (_isAcquired)
            {
                RelinquishVJD(_deviceId);
                _isAcquired = false;
            }
        }

        private static JoystickState CreateCenteredState(uint deviceId)
        {
            return new JoystickState
            {
                bDevice = (byte)deviceId,
                Throttle = 1,
                Rudder = 1,
                Aileron = 1,
                AxisX = 16384,
                AxisY = 1,
                AxisZ = 1,
                AxisXRot = 1,
                AxisYRot = 1,
                AxisZRot = 1,
                Slider = 1,
                Dial = 1,
                Wheel = 1,
                AxisVX = 1,
                AxisVY = 1,
                AxisVZ = 1,
                AxisVBRX = 1,
                AxisVBRY = 1,
                AxisVBRZ = 1
            };
        }

        private static int ClampAxis(int value)
        {
            return Math.Clamp(value, 1, 32768);
        }

        private void ValidatePedalAxes()
        {
            try
            {
                bool hasThrottleAxis = GetVJDAxisExist(_deviceId, HID_USAGE_Y);
                bool hasBrakeAxis = GetVJDAxisExist(_deviceId, HID_USAGE_Z) || GetVJDAxisExist(_deviceId, HID_USAGE_RZ);

                if (!hasThrottleAxis || !hasBrakeAxis)
                {
                    LastWarning = "Enable the Y and Z/Rz axes in vJoy Config for throttle and brake.";
                }
            }
            catch (EntryPointNotFoundException)
            {
            }
        }
    }
}
