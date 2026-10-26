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

        InputAction useItemAction;
        void OnEnable()
        {
            InputSystem.onDeviceChange += DeviceChanged;
            if(useItemAction==null)
            {
                useItemAction=new InputAction("Use held item",InputActionType.Button);
                useItemAction.AddBinding("<Keyboard>/f");
                useItemAction.AddBinding("<Gamepad>/rightShoulder");
                useItemAction.performed += ItemPressed;
            }
            useItemAction.Enable();
        }
        void ItemPressed(InputAction.CallbackContext context)
        {
            // Button actions perform once per press. An unavailable press is discarded,
            // never queued to consume a pickup after a pause/countdown/menu transition.
            if(HasItems && !NetworkControlled)UseItem();
        }
        void OnDisable()
        {
            InputSystem.onDeviceChange -= DeviceChanged;
            useItemAction?.Disable();
        }
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
