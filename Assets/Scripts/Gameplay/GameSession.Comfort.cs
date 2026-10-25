using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VelocityRivals
{
    public sealed partial class GameSession
    {
        readonly HashSet<VehicleController> pausedDrivers = new HashSet<VehicleController>();
        public float ResumeRemaining { get; private set; }
        public string PauseReason { get; private set; } = "Your event is paused.";
        public int ChampionshipPoints(int driver) => driver >= 0 && driver < championshipPoints.Length ? championshipPoints[driver] : 0;

        void OnEnable() { InputSystem.onDeviceChange += DeviceChanged; }
        void OnDisable() { InputSystem.onDeviceChange -= DeviceChanged; }
        void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (!(device is Gamepad) || !Player || Player.LastInputDeviceId != device.deviceId) return;
            if (change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed || change == InputDeviceChange.Disabled)
                SetPaused(true, "Controller disconnected. Reconnect it, or use the keyboard, then choose RESUME.");
        }
        void ResetInterruptions()
        {
            ResumeRemaining = 0; pausedDrivers.Clear(); PauseReason = "Your event is paused.";
        }
        bool EligibleDriver(VehicleController car)
        {
            if (!car || !car.isActiveAndEnabled) return false;
            foreach (var rival in racers) if (rival.Car == car && rival.FinishTime >= 0) return false;
            return true;
        }
        void RestorePausedDrivers()
        {
            foreach (var car in cars)
                if (car) car.ControlsEnabled = countdown <= 0 && pausedDrivers.Contains(car) && EligibleDriver(car);
            pausedDrivers.Clear();
        }
        public void ResumeSafely()
        {
            if (!Paused || !Running || Finished || NetworkControlled) return;
            if (countdown > 0) { SetPaused(false); return; } // The starting grid already has its own countdown.
            Paused = false; ResumeRemaining = 3; Time.timeScale = 0;
        }
        bool TickResume()
        {
            if (ResumeRemaining <= 0) return false;
            ResumeRemaining = Mathf.Max(0, ResumeRemaining - Time.unscaledDeltaTime);
            if (ResumeRemaining <= 0)
            {
                Time.timeScale = 1; RestorePausedDrivers();
                SoundDirector.Instance?.Cue(GameCue.Go);
            }
            return true;
        }
    }
}
