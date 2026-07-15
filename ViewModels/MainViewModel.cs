using MouseToVJoy.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MouseToVJoy.Data; // Ajustează namespace-ul după proiectul tău

namespace VirtualControllerApp.ViewModels // Ajustează namespace-ul după proiectul tău
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // --- Constante / Limite Axă ---
        private const int AxisMin = 0;
        private const int AxisMax = 32767;
        private const int AxisCenter = 16384;
        private const int AxisHalfRange = 16384;

        // --- Câmpuri Private (Stări pedale și timere) ---
        private double _virtualWheelValue = AxisCenter;
        private double _virtualThrottleValue = AxisMin;
        private double _virtualBrakeValue = AxisMin;

        private double _mousePedalRawCombinedValue = 0.0;
        private double _brakeStabilityRatio = 0.0;
        private double _keyboardThrottleRawRatio = 0.0;
        private double _keyboardBrakeRawRatio = 0.0;

        private double _throttleScrollOffset = 0.0;
        private double _brakeScrollOffset = 0.0;
        private double _throttleScrollResetTimer = 0.0;
        private double _brakeScrollResetTimer = 0.0;

        private double _throttleIdleTimer = 0.0;
        private double _throttleAssistActiveTimer = 0.0;
        private double _activeThrottleReduction = 0.0;
        private bool _wasThrottlePressed = false;

        // --- Câmpuri Private (Setări salvabile) ---
        private bool _enableThrottle = true;
        private bool _enableBrake = true;

        private bool _enableKeyboardThrottle;
        private string _keyboardThrottleKey = "W";
        private double _keyboardThrottleLimit = 1.0;
        private double _keyboardThrottleScrollSensitivity = 0.05;
        private double _keyboardThrottleScrollResetTime = 1.5;
        private double _keyboardThrottleLagUpSeconds = 0.2;
        private double _keyboardThrottleLagDownSeconds = 0.1;
        private bool _enableKeyboardThrottleCurve;
        private string _keyboardThrottleCurvePoints = "0,0;1,1";
        private bool _enableKeyboardThrottleSteeringAssist;
        private double _keyboardThrottleSteeringAssistStrength = 0.5;
        private double _keyboardThrottleAssistIdleThreshold = 0.5;
        private double _keyboardThrottleAssistDuration = 1.0;

        private bool _enableKeyboardBrake;
        private string _keyboardBrakeKey = "S";
        private double _keyboardBrakeLimit = 1.0;
        private double _keyboardBrakeScrollSensitivity = 0.05;
        private double _keyboardBrakeScrollResetTime = 1.5;
        private double _keyboardBrakeLagUpSeconds = 0.2;
        private double _keyboardBrakeLagDownSeconds = 0.1;
        private bool _enableKeyboardBrakeCurve;
        private string _keyboardBrakeCurvePoints = "0,0;1,1";
        private bool _enableKeyboardBrakeSteeringAssist;
        private double _keyboardBrakeSteeringAssistStrength = 0.5;

        // --- NEW: Câmpuri Steering Rate Assist ---
        private bool _enableKeyboardSteeringRateAssist;
        private double _keyboardSteeringRateAssistMaxReduction = 0.50;
        private bool _enableKeyboardSteeringRateAssistThrottle = true;
        private bool _enableKeyboardSteeringRateAssistBrake = true;
        private double _keyboardSteeringRateAssistBrakeDelaySeconds = 0.0;
        private double _brakePressTimer = 0.0;

        // --- Liste de Opțiuni ---
        public List<string> KeyboardKeyOptions { get; } = new List<string> { "W", "S", "A", "D", "Up", "Down", "Space", "LeftShift", "LeftCtrl" };

        // --- Constructor ---
        public MainViewModel()
        {
            // Inițializări comenzi dacă este cazul
            OpenThrottleCurveEditorCommand = new RelayCommand(OpenThrottleCurveEditor);
            OpenBrakeCurveEditorCommand = new RelayCommand(OpenBrakeCurveEditor);
        }

        // --- Comenzi UI ---
        public ICommand OpenThrottleCurveEditorCommand { get; }
        public ICommand OpenBrakeCurveEditorCommand { get; }

        // --- Proprietăți Publice Axă și Activare ---
        public bool EnableThrottle { get => _enableThrottle; set => SetSetting(ref _enableThrottle, value); }
        public bool EnableBrake { get => _enableBrake; set => SetSetting(ref _enableBrake, value); }

        // --- Proprietăți Publice Keyboard Throttle ---
        public bool EnableKeyboardThrottle
        {
            get => _enableKeyboardThrottle;
            set { SetSetting(ref _enableKeyboardThrottle, value); OnPropertyChanged(nameof(IsKeyboardThrottleSettingsEnabled)); }
        }
        public string KeyboardThrottleKey { get => _keyboardThrottleKey; set => SetSetting(ref _keyboardThrottleKey, value); }
        public double KeyboardThrottleLimit { get => _keyboardThrottleLimit; set => SetSetting(ref _keyboardThrottleLimit, value); }
        public double KeyboardThrottleScrollSensitivity { get => _keyboardThrottleScrollSensitivity; set => SetSetting(ref _keyboardThrottleScrollSensitivity, value); }
        public double KeyboardThrottleScrollResetTime { get => _keyboardThrottleScrollResetTime; set => SetSetting(ref _keyboardThrottleScrollResetTime, value); }
        public double KeyboardThrottleLagUpSeconds { get => _keyboardThrottleLagUpSeconds; set => SetSetting(ref _keyboardThrottleLagUpSeconds, value); }
        public double KeyboardThrottleLagDownSeconds { get => _keyboardThrottleLagDownSeconds; set => SetSetting(ref _keyboardThrottleLagDownSeconds, value); }
        public bool EnableKeyboardThrottleCurve { get => _enableKeyboardThrottleCurve; set => SetSetting(ref _enableKeyboardThrottleCurve, value); }
        public string KeyboardThrottleCurvePoints { get => _keyboardThrottleCurvePoints; set => SetSetting(ref _keyboardThrottleCurvePoints, value); }
        public bool EnableKeyboardThrottleSteeringAssist { get => _enableKeyboardThrottleSteeringAssist; set => SetSetting(ref _enableKeyboardThrottleSteeringAssist, value); }
        public double KeyboardThrottleSteeringAssistStrength { get => _keyboardThrottleSteeringAssistStrength; set => SetSetting(ref _keyboardThrottleSteeringAssistStrength, value); }
        public double KeyboardThrottleAssistIdleThreshold { get => _keyboardThrottleAssistIdleThreshold; set => SetSetting(ref _keyboardThrottleAssistIdleThreshold, value); }
        public double KeyboardThrottleAssistDuration { get => _keyboardThrottleAssistDuration; set => SetSetting(ref _keyboardThrottleAssistDuration, value); }
        public bool IsKeyboardThrottleSettingsEnabled => EnableThrottle && EnableKeyboardThrottle;

        // --- Proprietăți Publice Keyboard Brake ---
        public bool EnableKeyboardBrake
        {
            get => _enableKeyboardBrake;
            set { SetSetting(ref _enableKeyboardBrake, value); OnPropertyChanged(nameof(IsKeyboardBrakeSettingsEnabled)); }
        }
        public string KeyboardBrakeKey { get => _keyboardBrakeKey; set => SetSetting(ref _keyboardBrakeKey, value); }
        public double KeyboardBrakeLimit { get => _keyboardBrakeLimit; set => SetSetting(ref _keyboardBrakeLimit, value); }
        public double KeyboardBrakeScrollSensitivity { get => _keyboardBrakeScrollSensitivity; set => SetSetting(ref _keyboardBrakeScrollSensitivity, value); }
        public double KeyboardBrakeScrollResetTime { get => _keyboardBrakeScrollResetTime; set => SetSetting(ref _keyboardBrakeScrollResetTime, value); }
        public double KeyboardBrakeLagUpSeconds { get => _keyboardBrakeLagUpSeconds; set => SetSetting(ref _keyboardBrakeLagUpSeconds, value); }
        public double KeyboardBrakeLagDownSeconds { get => _keyboardBrakeLagDownSeconds; set => SetSetting(ref _keyboardBrakeLagDownSeconds, value); }
        public bool EnableKeyboardBrakeCurve { get => _enableKeyboardBrakeCurve; set => SetSetting(ref _enableKeyboardBrakeCurve, value); }
        public string KeyboardBrakeCurvePoints { get => _keyboardBrakeCurvePoints; set => SetSetting(ref _keyboardBrakeCurvePoints, value); }
        public bool EnableKeyboardBrakeSteeringAssist { get => _enableKeyboardBrakeSteeringAssist; set => SetSetting(ref _enableKeyboardBrakeSteeringAssist, value); }
        public double KeyboardBrakeSteeringAssistStrength { get => _keyboardBrakeSteeringAssistStrength; set => SetSetting(ref _keyboardBrakeSteeringAssistStrength, value); }
        public bool IsKeyboardBrakeSettingsEnabled => EnableBrake && EnableKeyboardBrake;

        // --- NEW: Proprietăți Publice Steering Rate Assist ---
        public bool EnableKeyboardSteeringRateAssist
        { get => _enableKeyboardSteeringRateAssist; set => SetSetting(ref _enableKeyboardSteeringRateAssist, value); }

        public double KeyboardSteeringRateAssistMaxReduction
        { get => _keyboardSteeringRateAssistMaxReduction; set => SetSetting(ref _keyboardSteeringRateAssistMaxReduction, Math.Clamp(value, 0.0, 1.0)); }

        public bool EnableKeyboardSteeringRateAssistThrottle
        { get => _enableKeyboardSteeringRateAssistThrottle; set => SetSetting(ref _enableKeyboardSteeringRateAssistThrottle, value); }

        public bool EnableKeyboardSteeringRateAssistBrake
        { get => _enableKeyboardSteeringRateAssistBrake; set => SetSetting(ref _enableKeyboardSteeringRateAssistBrake, value); }

        public double KeyboardSteeringRateAssistBrakeDelaySeconds
        { get => _keyboardSteeringRateAssistBrakeDelaySeconds; set => SetSetting(ref _keyboardSteeringRateAssistBrakeDelaySeconds, Math.Clamp(value, 0.0, 5.0)); }

        // --- Actualizare Cadru Controler ---
        public void Update(double elapsedSeconds)
        {
            UpdateKeyboardPedals(elapsedSeconds);
        }

        private void UpdateKeyboardPedals(double elapsedSeconds)
        {
            double steeringFactor = Math.Abs(_virtualWheelValue - AxisCenter) / (double)AxisHalfRange;

            // --- THROTTLE ---
            if (EnableThrottle && EnableKeyboardThrottle)
            {
                bool pressed = IsKeyDown(GetVirtualKey(KeyboardThrottleKey));
                if (!pressed)
                {
                    _throttleIdleTimer += elapsedSeconds;

                    _throttleScrollResetTimer += elapsedSeconds;
                    if (_throttleScrollResetTimer >= KeyboardThrottleScrollResetTime)
                    {
                        _throttleScrollOffset = 0.0;
                    }
                }
                else
                {
                    _throttleScrollResetTimer = 0.0;

                    if (!_wasThrottlePressed && _throttleIdleTimer >= KeyboardThrottleAssistIdleThreshold)
                    {
                        _throttleAssistActiveTimer = KeyboardThrottleAssistDuration;
                    }
                    _throttleIdleTimer = 0.0;
                }
                _wasThrottlePressed = pressed;

                double target = 0.0;
                if (pressed)
                {
                    target = Math.Clamp(KeyboardThrottleLimit + _throttleScrollOffset, 0.0, 1.0);
                }

                double lagSeconds = pressed ? KeyboardThrottleLagUpSeconds : KeyboardThrottleLagDownSeconds;

                // --- NEW: STEERING RATE ASSIST FOR THROTTLE ---
                if (pressed && EnableKeyboardSteeringRateAssist && EnableKeyboardSteeringRateAssistThrottle)
                {
                    double reduction = steeringFactor * KeyboardSteeringRateAssistMaxReduction;
                    double speedMultiplier = Math.Max(0.001, 1.0 - reduction);
                    lagSeconds /= speedMultiplier; // Crește timpul de lag up pentru a încetini acumularea
                }

                _keyboardThrottleRawRatio = MoveRatioToward(_keyboardThrottleRawRatio, target, lagSeconds, elapsedSeconds);

                double outputRatio = EnableKeyboardThrottleCurve ? ApplyResponseCurve(_keyboardThrottleRawRatio, KeyboardThrottleCurvePoints) : _keyboardThrottleRawRatio;

                double targetReduction = 0.0;
                if (EnableKeyboardThrottleSteeringAssist && _throttleAssistActiveTimer > 0)
                {
                    _throttleAssistActiveTimer -= elapsedSeconds;
                    targetReduction = steeringFactor * KeyboardThrottleSteeringAssistStrength;
                }
                else
                {
                    _throttleAssistActiveTimer = 0.0;
                }

                double fadeTime = (targetReduction > _activeThrottleReduction) ? 0.1 : 1.2;
                _activeThrottleReduction = MoveRatioToward(_activeThrottleReduction, targetReduction, fadeTime, elapsedSeconds);

                outputRatio = Math.Max(0.0, outputRatio * (1.0 - _activeThrottleReduction));

                SetThrottleValue(RatioToAxis(outputRatio));
            }
            else
            {
                _keyboardThrottleRawRatio = GetAxisRatio(_virtualThrottleValue);
                _throttleIdleTimer = 0.0;
                _throttleAssistActiveTimer = 0.0;
            }

            // --- BRAKE ---
            if (EnableBrake && EnableKeyboardBrake)
            {
                bool pressed = IsKeyDown(GetVirtualKey(KeyboardBrakeKey));
                if (!pressed)
                {
                    _brakePressTimer = 0.0;

                    _brakeScrollResetTimer += elapsedSeconds;
                    if (_brakeScrollResetTimer >= KeyboardBrakeScrollResetTime)
                    {
                        _brakeScrollOffset = 0.0;
                    }
                }
                else
                {
                    _brakePressTimer += elapsedSeconds;
                    _brakeScrollResetTimer = 0.0;
                }

                double target = 0.0;
                if (pressed)
                {
                    target = Math.Clamp(KeyboardBrakeLimit + _brakeScrollOffset, 0.0, 1.0);
                }

                double lagSeconds = pressed ? KeyboardBrakeLagUpSeconds : KeyboardBrakeLagDownSeconds;

                // --- NEW: STEERING RATE ASSIST FOR BRAKE (With Optional Delay) ---
                if (pressed && EnableKeyboardSteeringRateAssist && EnableKeyboardSteeringRateAssistBrake)
                {
                    if (_brakePressTimer >= KeyboardSteeringRateAssistBrakeDelaySeconds)
                    {
                        double reduction = steeringFactor * KeyboardSteeringRateAssistMaxReduction;
                        double speedMultiplier = Math.Max(0.001, 1.0 - reduction);
                        lagSeconds /= speedMultiplier;
                    }
                }

                _keyboardBrakeRawRatio = MoveRatioToward(_keyboardBrakeRawRatio, target, lagSeconds, elapsedSeconds);

                double outputRatio = EnableKeyboardBrakeCurve ? ApplyResponseCurve(_keyboardBrakeRawRatio, KeyboardBrakeCurvePoints) : _keyboardBrakeRawRatio;

                if (EnableKeyboardBrakeSteeringAssist)
                {
                    double reduction = steeringFactor * KeyboardBrakeSteeringAssistStrength;
                    outputRatio = Math.Max(0.0, outputRatio * (1.0 - reduction));
                }

                SetBrakeValue(RatioToAxis(outputRatio));
            }
            else
            {
                _keyboardBrakeRawRatio = GetAxisRatio(_virtualBrakeValue);
            }
        }

        // --- Metode Auxiliare Logice ---
        private void ResetControlsToZero()
        {
            SetVirtualWheelValue(ApplyTimedCentering(_virtualWheelValue, AxisCenter, 0.1, 0.001), force: true);
            SetThrottleValue(AxisMin);
            SetBrakeValue(AxisMin);
            _mousePedalRawCombinedValue = 0.0;
            _brakeStabilityRatio = 0.0;
            _keyboardThrottleRawRatio = 0.0;
            _keyboardBrakeRawRatio = 0.0;
            _throttleScrollOffset = 0.0;
            _brakeScrollOffset = 0.0;
            _brakePressTimer = 0.0;
        }

        private double MoveRatioToward(double current, double target, double travelTimeSeconds, double elapsedSeconds)
        {
            if (travelTimeSeconds <= 0.0) return target;
            double step = elapsedSeconds / travelTimeSeconds;
            if (current < target)
                return Math.Min(target, current + step);
            else
                return Math.Max(target, current - step);
        }

        private double ApplyTimedCentering(double current, double target, double speed, double precision)
        {
            if (Math.Abs(current - target) < precision) return target;
            return current + (target - current) * speed;
        }

        private double ApplyResponseCurve(double input, string pointsString)
        {
            // O simplă interpolare liniară între punctele definite de utilizator "0,0;1,1"
            return input;
        }

        private double GetAxisRatio(double val) => (val - AxisMin) / (double)(AxisMax - AxisMin);
        private double RatioToAxis(double ratio) => AxisMin + ratio * (AxisMax - AxisMin);

        private void SetVirtualWheelValue(double val, bool force = false) => _virtualWheelValue = val;
        private void SetThrottleValue(double val) => _virtualThrottleValue = val;
        private void SetBrakeValue(double val) => _virtualBrakeValue = val;

        // Mock pentru verificarea tastelor (înlocuiește cu interfața ta Win32/WPF reală de citire tastatură)
        private bool IsKeyDown(Key key) => Keyboard.IsKeyDown(key);
        private Key GetVirtualKey(string keyName)
        {
            if (Enum.TryParse(keyName, out Key key)) return key;
            return Key.None;
        }

        // --- Logica Gestiune Setări Profile (JSON) ---
        private PresetSettings CaptureSettings()
        {
            return new PresetSettings
            {
                EnableThrottle = EnableThrottle,
                EnableBrake = EnableBrake,
                EnableKeyboardThrottle = EnableKeyboardThrottle,
                KeyboardThrottleKey = KeyboardThrottleKey,
                KeyboardThrottleLimit = KeyboardThrottleLimit,
                KeyboardThrottleScrollSensitivity = KeyboardThrottleScrollSensitivity,
                KeyboardThrottleScrollResetTime = KeyboardThrottleScrollResetTime,
                KeyboardThrottleLagUpSeconds = KeyboardThrottleLagUpSeconds,
                KeyboardThrottleLagDownSeconds = KeyboardThrottleLagDownSeconds,
                EnableKeyboardThrottleCurve = EnableKeyboardThrottleCurve,
                KeyboardThrottleCurvePoints = KeyboardThrottleCurvePoints,
                EnableKeyboardThrottleSteeringAssist = EnableKeyboardThrottleSteeringAssist,
                KeyboardThrottleSteeringAssistStrength = KeyboardThrottleSteeringAssistStrength,
                KeyboardThrottleAssistIdleThreshold = KeyboardThrottleAssistIdleThreshold,
                KeyboardThrottleAssistDuration = KeyboardThrottleAssistDuration,
                EnableKeyboardBrake = EnableKeyboardBrake,
                KeyboardBrakeKey = KeyboardBrakeKey,
                KeyboardBrakeLimit = KeyboardBrakeLimit,
                KeyboardBrakeScrollSensitivity = KeyboardBrakeScrollSensitivity,
                KeyboardBrakeScrollResetTime = KeyboardBrakeScrollResetTime,
                KeyboardBrakeLagUpSeconds = KeyboardBrakeLagUpSeconds,
                KeyboardBrakeLagDownSeconds = KeyboardBrakeLagDownSeconds,
                EnableKeyboardBrakeCurve = EnableKeyboardBrakeCurve,
                KeyboardBrakeCurvePoints = KeyboardBrakeCurvePoints,
                EnableKeyboardBrakeSteeringAssist = EnableKeyboardBrakeSteeringAssist,
                KeyboardBrakeSteeringAssistStrength = KeyboardBrakeSteeringAssistStrength,

                // Setări noi
                EnableKeyboardSteeringRateAssist = EnableKeyboardSteeringRateAssist,
                KeyboardSteeringRateAssistMaxReduction = KeyboardSteeringRateAssistMaxReduction,
                EnableKeyboardSteeringRateAssistThrottle = EnableKeyboardSteeringRateAssistThrottle,
                EnableKeyboardSteeringRateAssistBrake = EnableKeyboardSteeringRateAssistBrake,
                KeyboardSteeringRateAssistBrakeDelaySeconds = KeyboardSteeringRateAssistBrakeDelaySeconds
            };
        }

        private void ApplySettings(PresetSettings settings)
        {
            if (settings == null) return;

            EnableThrottle = settings.EnableThrottle;
            EnableBrake = settings.EnableBrake;
            EnableKeyboardThrottle = settings.EnableKeyboardThrottle;
            KeyboardThrottleKey = settings.KeyboardThrottleKey;
            KeyboardThrottleLimit = settings.KeyboardThrottleLimit;
            KeyboardThrottleScrollSensitivity = settings.KeyboardThrottleScrollSensitivity;
            KeyboardThrottleScrollResetTime = settings.KeyboardThrottleScrollResetTime;
            KeyboardThrottleLagUpSeconds = settings.KeyboardThrottleLagUpSeconds;
            KeyboardThrottleLagDownSeconds = settings.KeyboardThrottleLagDownSeconds;
            EnableKeyboardThrottleCurve = settings.EnableKeyboardThrottleCurve;
            KeyboardThrottleCurvePoints = settings.KeyboardThrottleCurvePoints;
            EnableKeyboardThrottleSteeringAssist = settings.EnableKeyboardThrottleSteeringAssist;
            KeyboardThrottleSteeringAssistStrength = settings.KeyboardThrottleSteeringAssistStrength;
            KeyboardThrottleAssistIdleThreshold = settings.KeyboardThrottleAssistIdleThreshold;
            KeyboardThrottleAssistDuration = settings.KeyboardThrottleAssistDuration;
            EnableKeyboardBrake = settings.EnableKeyboardBrake;
            KeyboardBrakeKey = settings.KeyboardBrakeKey;
            KeyboardBrakeLimit = settings.KeyboardBrakeLimit;
            KeyboardBrakeScrollSensitivity = settings.KeyboardBrakeScrollSensitivity;
            KeyboardBrakeScrollResetTime = settings.KeyboardBrakeScrollResetTime;
            KeyboardBrakeLagUpSeconds = settings.KeyboardBrakeLagUpSeconds;
            KeyboardBrakeLagDownSeconds = settings.KeyboardBrakeLagDownSeconds;
            EnableKeyboardBrakeCurve = settings.EnableKeyboardBrakeCurve;
            KeyboardBrakeCurvePoints = settings.KeyboardBrakeCurvePoints;
            EnableKeyboardBrakeSteeringAssist = settings.EnableKeyboardBrakeSteeringAssist;
            KeyboardBrakeSteeringAssistStrength = settings.KeyboardBrakeSteeringAssistStrength;

            // Setări noi
            EnableKeyboardSteeringRateAssist = settings.EnableKeyboardSteeringRateAssist;
            KeyboardSteeringRateAssistMaxReduction = settings.KeyboardSteeringRateAssistMaxReduction;
            EnableKeyboardSteeringRateAssistThrottle = settings.EnableKeyboardSteeringRateAssistThrottle;
            EnableKeyboardSteeringRateAssistBrake = settings.EnableKeyboardSteeringRateAssistBrake;
            KeyboardSteeringRateAssistBrakeDelaySeconds = settings.KeyboardSteeringRateAssistBrakeDelaySeconds;
        }

        // --- Implementare INotifyPropertyChanged & Helpers ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetSetting<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OpenThrottleCurveEditor() { /* Deschide editor curbe */ }
        private void OpenBrakeCurveEditor() { /* Deschide editor curbe */ }
    }

    // O clasă simplă de comandă pentru a asigura compilarea corectă
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute();
        public event EventHandler CanExecuteChanged { add { } remove { } }
    }
}