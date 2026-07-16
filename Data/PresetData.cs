using System;
using System.Collections.Generic;
using System.Linq;

namespace KBMJoyAssist.Data
{
    public struct ResponseCurvePoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public ResponseCurvePoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class PresetSettings
    {
        public const string DefaultResponseCurvePoints = "0,0;0.05,0.005;0.1,0.015;0.15,0.03;0.2,0.05;0.3,0.1;0.4,0.18;0.5,0.3;0.6,0.45;0.7,0.62;0.8,0.8;0.9,0.93;1,1";

        public double PedalSensitivity { get; set; } = 40.0;
        public double MousePedalDeadZone { get; set; } = 0.1;
        public double WheelSensitivity { get; set; } = 0.1;
        public bool EnableThrottle { get; set; } = true;
        public bool EnableBrake { get; set; } = true;
        public bool EnableKeyboardThrottle { get; set; } = true;
        public bool EnableKeyboardBrake { get; set; } = true;
        public string KeyboardThrottleKey { get; set; } = "W";
        public string KeyboardBrakeKey { get; set; } = "S";
        public double KeyboardThrottleLagUpSeconds { get; set; } = 0.6;
        public double KeyboardThrottleLagDownSeconds { get; set; } = 0.4;
        public double KeyboardBrakeLagUpSeconds { get; set; } = 0.3;
        public double KeyboardBrakeLagDownSeconds { get; set; } = 0.20;
        public bool EnableWheelCentering { get; set; }
        public double WheelReturnTimeSeconds { get; set; } = 20.95;
        public bool EnableBrakeResetting { get; set; }
        public double BrakeReturnTimeSeconds { get; set; } = 5.0;
        public bool EnableRightClickPedalMode { get; set; } = true;
        public bool EnableCenterLockedCursor { get; set; } = true;
        public bool EnableFullThrottleHold { get; set; } = false;
        public double FullThrottleHoldDeadzone { get; set; } = 0.01;
        public bool EnableSteeringDampening { get; set; } = true;
        public double SteeringDampening { get; set; } = 0.20;
        public bool EnableBrakeAssist { get; set; } = false;
        public double BrakeAssistThreshold { get; set; } = 0.70;
        public double BrakeAssistStrength { get; set; } = 0.35;
        public double BrakeAssistHold { get; set; } = 0.0;
        public bool EnableBrakeXDeadzone { get; set; } = false;
        public double BrakeXDeadzone { get; set; } = 0.04;
        public bool EnableTimedBrakeXDeadzone { get; set; } = false;
        public double BrakeXDeadzoneDurationSeconds { get; set; } = 0.05;
        public bool EnableTrailBraking { get; set; } = false;
        public double TrailBrakingRelease { get; set; } = 0.06;

        public bool EnableKeyboardThrottleCurve { get; set; }
        public string KeyboardThrottleCurvePoints { get; set; } = DefaultResponseCurvePoints;
        public bool EnableKeyboardBrakeCurve { get; set; }
        public string KeyboardBrakeCurvePoints { get; set; } = DefaultResponseCurvePoints;
        public bool EnableKeyboardBrakeSteeringAssist { get; set; } = true;
        public double KeyboardBrakeSteeringAssistStrength { get; set; } = 0.50;
        public bool EnableKeyboardThrottleSteeringAssist { get; set; } = true;
        public double KeyboardThrottleSteeringAssistStrength { get; set; } = 0.50;
        public double KeyboardThrottleAssistIdleThreshold { get; set; } = 1.0;
        public double KeyboardThrottleAssistDuration { get; set; } = 2.0;

        public double KeyboardSteeringThrottleLagUpSlowingEffect { get; set; } = 10;
        public double KeyboardSteeringBrakeLagUpSlowingEffect { get; set; } = 15;

        public double KeyboardThrottleLimit { get; set; } = 1.0;
        public double KeyboardThrottleScrollSensitivity { get; set; } = 0.05;
        public double KeyboardThrottleScrollResetTime { get; set; } = 1.0;
        public double KeyboardBrakeLimit { get; set; } = 1.0;
        public double KeyboardBrakeScrollSensitivity { get; set; } = 0.05;
        public double KeyboardBrakeScrollResetTime { get; set; } = 1.0;

        public static PresetSettings CreateDefault() => new();
    }

    public class PresetSlotData
    {
        public string Name { get; set; } = string.Empty;
        public PresetSettings Settings { get; set; } = new();

        public static PresetSlotData CreateDefault(int index)
        {
            return new PresetSlotData
            {
                Name = $"Preset {index + 1}",
                Settings = PresetSettings.CreateDefault()
            };
        }
    }

    public class PresetFileData
    {
        public const int PresetSlotCount = 10;

        public int ActivePresetIndex { get; set; }
        public List<PresetSlotData> Presets { get; set; } = new();

        public static PresetFileData CreateDefault()
        {
            var data = new PresetFileData();
            for (int i = 0; i < PresetSlotCount; i++)
            {
                data.Presets.Add(PresetSlotData.CreateDefault(i));
            }
            return data;
        }

        public PresetFileData Normalize()
        {
            while (Presets.Count < PresetSlotCount)
            {
                Presets.Add(PresetSlotData.CreateDefault(Presets.Count));
            }
            if (Presets.Count > PresetSlotCount)
            {
                Presets = Presets.Take(PresetSlotCount).ToList();
            }
            ActivePresetIndex = Math.Clamp(ActivePresetIndex, 0, PresetSlotCount - 1);
            return this;
        }
    }
}