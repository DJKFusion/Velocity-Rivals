using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VelocityRivals.Tests
{
    public sealed class SessionTests
    {
        GameObject root;
        string directory, previousDirectory;
        [SetUp]
        public void IsolateSave()
        {
            previousDirectory = SaveStore.StorageDirectoryOverride;
            directory = Path.Combine(Path.GetTempPath(), "VelocityPlayTests-" + System.Guid.NewGuid().ToString("N"));
            SaveStore.StorageDirectoryOverride = directory;
            SaveStore.Reload();
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (root) Object.Destroy(root);
            var camera = Camera.main;
            if (camera) Object.Destroy(camera.gameObject);
            Time.timeScale = 1;
            yield return null;
            yield return null;
            SaveStore.StorageDirectoryOverride = previousDirectory;
            SaveStore.Reload();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [UnityTest]
        public IEnumerator DroppedHazardsExpirePauseAndClearOnRestart()
        {
            root=new GameObject("Hazard test",typeof(GameSession));var session=root.GetComponent<GameSession>();
            session.StartMatch(GameMode.Championship,0,0,Color.cyan);yield return null;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(GameSession).GetField("countdown",flags).SetValue(session,0f);
            var car=session.Player;car.ControlsEnabled=true;
            var items=Object.FindFirstObjectByType<RaceItems>();
            Assert.IsTrue(items.Collect(car,PowerupKind.OilSlick));
            typeof(VehicleController).GetProperty("Grounded").SetValue(car,false);
            Assert.IsFalse(items.Use(car));Assert.AreEqual(PowerupKind.OilSlick,items.Held(car));
            // Put hazards away from racers to test lifetime and limits deterministically.
            var drop=typeof(RaceItems).GetMethod("DropHazard",flags);
            for(int i=0;i<15;i++)drop.Invoke(items,new object[]{car,PowerupKind.ShockMine,new Vector3(1000+i*5,0,1000)});
            Assert.AreEqual(12,items.ActiveHazardCount);
            session.SetPaused(true);float before=session.Elapsed;yield return null;
            Assert.AreEqual(before,session.Elapsed);Assert.AreEqual(12,items.ActiveHazardCount);
            session.SetPaused(false);
            typeof(GameSession).GetField("elapsed",flags).SetValue(session,session.Elapsed+21);
            items.SendMessage("FixedUpdate");Assert.AreEqual(0,items.ActiveHazardCount);
            typeof(VehicleController).GetProperty("Grounded").SetValue(car,true);
            car.GetComponent<Rigidbody>().linearVelocity=Vector3.forward*30;
            typeof(VehicleController).GetField("hazardGraceUntil",flags).SetValue(car,0f);
            Assert.IsTrue(car.ApplyRaceHazard(true));Assert.IsTrue(car.OilAffected);
            Assert.IsFalse(car.ApplyRaceHazard(false));
            car.Respawn(car.PhysicsPosition,car.PhysicsRotation);Assert.IsFalse(car.OilAffected);
            session.RestartMatch();yield return null;
            Assert.AreEqual(0,Object.FindFirstObjectByType<RaceItems>().ActiveHazardCount);
        }

        [UnityTest]
        public IEnumerator RacingSoundsLoadAndMuteDuringPause()
        {
            foreach(string name in new[]{"EngineSport","EngineHeavy","EngineExotic","Tyres","Boost","Wind","Impact","Shift"})
            {
                var clip=Resources.Load<AudioClip>("RacingAudio/"+name);
                Assert.IsNotNull(clip,name);Assert.Greater(clip.length,.1f);
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad,clip.loadType,name);
            }
            root=new GameObject("Race audio test",typeof(GameSession));
            var session=root.GetComponent<GameSession>();session.StartMatch(GameMode.Championship,0,0,Color.cyan);
            yield return null;yield return null;yield return null;
            var sound=session.Player.GetComponent<RacingAudio>();Assert.IsNotNull(sound);
            Assert.Greater(sound.EngineVolume,0);
            Assert.GreaterOrEqual(Object.FindObjectsByType<RacingAudio>(FindObjectsSortMode.None).Length,4);
            session.SetPaused(true);yield return null;
            foreach(var source in session.Player.GetComponents<AudioSource>()) Assert.AreEqual(0,source.volume);
            session.SetPaused(false);yield return null;yield return null;
            Assert.Greater(sound.EngineVolume,0);
            SaveStore.Current.effectsVolume=0;
            yield return null;
            Assert.AreEqual(0,sound.EngineVolume,.001f);
        }

        [UnityTest]
        public IEnumerator RaceItemCollectUsePauseAndRestartRespectInventory()
        {
            root=new GameObject("Held item test",typeof(GameSession));var session=root.GetComponent<GameSession>();
            session.StartMatch(GameMode.Championship,0,0,Color.cyan);
            yield return null;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(GameSession).GetField("countdown",flags).SetValue(session,0f);
            session.Player.ControlsEnabled=true;
            var items=Object.FindFirstObjectByType<RaceItems>();Assert.IsNotNull(items);
            Assert.IsTrue(items.Collect(session.Player,PowerupKind.Shield));
            Assert.IsFalse(items.Collect(session.Player,PowerupKind.Pulse));
            Assert.AreEqual(PowerupKind.Shield,session.HeldItem);Assert.AreEqual("",session.ItemEffect);
            session.SetPaused(true);Assert.IsFalse(session.UseItem());Assert.AreEqual(PowerupKind.Shield,session.HeldItem);
            session.SetPaused(false);Assert.IsTrue(session.UseItem());Assert.IsFalse(session.UseItem());
            StringAssert.StartsWith("SHIELD",session.ItemEffect);Assert.AreEqual(PowerupKind.None,session.HeldItem);
            typeof(VehicleController).GetProperty("BoostAmount").SetValue(session.Player,.1f);
            Assert.IsTrue(items.Collect(session.Player,PowerupKind.Overdrive));Assert.IsTrue(session.UseItem());
            Assert.AreEqual(.7f,session.Player.BoostAmount,.001f);
            Assert.IsTrue(items.Collect(session.Player,PowerupKind.Pulse));
            session.RestartMatch();Assert.AreEqual(PowerupKind.None,session.HeldItem);
            session.StartMatch(GameMode.TimeAttack,0,0,Color.cyan);yield return null;
            Assert.IsFalse(session.HasItems);Assert.IsFalse(session.UseItem());Assert.IsNull(Object.FindFirstObjectByType<RaceItems>());
        }

        [UnityTest]
        public IEnumerator FKeyActivatesHeldRaceItem() => CheckHeldItemInput(false);

        [UnityTest]
        public IEnumerator RightShoulderActivatesHeldRaceItem() => CheckHeldItemInput(true);

        IEnumerator CheckHeldItemInput(bool useGamepad)
        {
            var oldMode=UnityEngine.InputSystem.InputSystem.settings.updateMode;
            var previousKeyboard=UnityEngine.InputSystem.Keyboard.current;
            var previousPad=UnityEngine.InputSystem.Gamepad.current;
            var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            var pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
            try
            {
                root=new GameObject("Item key binding",typeof(GameSession));var session=root.GetComponent<GameSession>();
                session.StartMatch(GameMode.Championship,0,0,Color.cyan);yield return null;
                typeof(GameSession).GetField("countdown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(session,0f);
                session.Player.ControlsEnabled=true;
                var items=Object.FindFirstObjectByType<RaceItems>();
                // Own the input tick so the Editor/player loop cannot clear a one-frame
                // press between event delivery and the production Update handler.
                UnityEngine.InputSystem.InputSystem.settings.updateMode=UnityEngine.InputSystem.InputSettings.UpdateMode.ProcessEventsManually;
                void Tick(bool pressed)
                {
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                        pressed && !useGamepad ? new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F) : new UnityEngine.InputSystem.LowLevel.KeyboardState());
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,
                        pressed && useGamepad ? new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.RightShoulder) : new UnityEngine.InputSystem.LowLevel.GamepadState());
                    UnityEngine.InputSystem.InputSystem.Update();
                    keyboard.MakeCurrent();pad.MakeCurrent();
                    session.SendMessage("Update");
                }
                Tick(false); // Establish released state and prime edge detection.
                Assert.IsTrue(items.Collect(session.Player,PowerupKind.Shield));
                Assert.IsTrue(session.ActionEnabled);
                Tick(true);
                Assert.AreEqual(PowerupKind.None,session.HeldItem);
                StringAssert.StartsWith("SHIELD",session.ItemEffect);
                Assert.IsTrue(items.Collect(session.Player,PowerupKind.Overdrive));
                Tick(true); // Holding the button must not spend a newly collected item.
                Assert.AreEqual(PowerupKind.Overdrive,session.HeldItem);
                Tick(false);
                session.SetPaused(true);Tick(true);
                Assert.AreEqual(PowerupKind.Overdrive,session.HeldItem);
                Tick(false);session.SetPaused(false);Tick(true);
                Assert.AreEqual(PowerupKind.None,session.HeldItem);
                Tick(false);
            }
            finally
            {
                // Restore global input state even when an assertion fails.
                UnityEngine.InputSystem.InputSystem.settings.updateMode=oldMode;
                if(keyboard.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                if(pad.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);
                if(previousKeyboard!=null && previousKeyboard.added)previousKeyboard.MakeCurrent();
                if(previousPad!=null && previousPad.added)previousPad.MakeCurrent();
            }
        }

        [UnityTest]
        public IEnumerator CombatPickupIsHeldUntilPlayerUsesIt()
        {
            root=new GameObject("Combat inventory",typeof(GameSession));var session=root.GetComponent<GameSession>();
            session.StartMatch(GameMode.BoostBattle,0,0,Color.cyan);yield return null;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(GameSession).GetField("countdown",flags).SetValue(session,0f);
            session.Player.ControlsEnabled=true;
            var crate=GameObject.Find("Flux Shield");Assert.IsNotNull(crate);
            session.Player.Respawn(crate.transform.position,Quaternion.identity);
            yield return null;yield return null;
            Assert.AreEqual(PowerupKind.Shield,session.HeldItem);
            Assert.IsFalse(session.CombatStatus.Contains("SHIELD"));
            Assert.IsTrue(session.UseItem());Assert.AreEqual(PowerupKind.None,session.HeldItem);
            StringAssert.Contains("SHIELD",session.CombatStatus);
        }

        [UnityTest]
        public IEnumerator HeavyCarTurnsAtRoadSpeedAndBoostUsesTransparentTrail()
        {
            root=new GameObject("Handling regression");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform);
            floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(300,1,300);
            var stats=Resources.Load<VehicleStats>("VehicleStats/Bulwark");
            var car=VehicleController.Spawn("Handling test",Vector3.up*.4f,Quaternion.identity,stats,Color.cyan,false);
            car.transform.SetParent(root.transform);car.ControlsEnabled=true;
            yield return new WaitForSeconds(.5f);
            var body=car.GetComponent<Rigidbody>();body.linearVelocity=Vector3.forward*28;
            float initialYaw=car.transform.eulerAngles.y;
            for(int i=0;i<60;i++)
            {
                car.SetInput(new DriveInput{Throttle=1,Steering=1});
                yield return new WaitForFixedUpdate();
            }
            Assert.Greater(Mathf.DeltaAngle(initialYaw,car.transform.eulerAngles.y),30,"Bulwark should take a sustained corner at road speed.");
            Assert.Less(car.SpeedKmh,100,"Corner assistance should reduce speed even while throttle is held.");
            var trail=car.GetComponentInChildren<TrailRenderer>();Assert.IsNotNull(trail);
            Assert.AreEqual("Velocity Rivals/Soft Effects",trail.sharedMaterial.shader.name);
            Assert.Less(trail.startColor.a,1);Assert.AreEqual(0,trail.endColor.a);
            car.Respawn(Vector3.up,Quaternion.identity);Assert.AreEqual(0,trail.positionCount);
        }

        [UnityTest]
        public IEnumerator ResumeFreezesClockAndPreservesStoppedDrivers()
        {
            root=new GameObject("Pause safety",typeof(GameSession));var session=root.GetComponent<GameSession>();
            session.StartMatch(GameMode.Championship,0,0,Color.cyan);
            yield return null;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(GameSession).GetField("countdown",flags).SetValue(session,0f);
            session.Player.ControlsEnabled=true;
            var stopped=session.Vehicles[1];stopped.ControlsEnabled=false;
            session.SetPaused(true);float elapsed=session.Elapsed;
            session.ResumeSafely();
            Assert.IsFalse(session.Paused);Assert.Greater(session.ResumeRemaining,0);
            Assert.IsFalse(session.ActionEnabled);Assert.AreEqual(0,Time.timeScale);
            yield return new WaitForSecondsRealtime(.05f);
            Assert.AreEqual(elapsed,session.Elapsed);Assert.IsFalse(stopped.ControlsEnabled);
            session.SetPaused(true,"Interrupted again");
            Assert.AreEqual(0,session.ResumeRemaining);Assert.AreEqual("Interrupted again",session.PauseReason);
            session.ResumeSafely();
            typeof(GameSession).GetField("<ResumeRemaining>k__BackingField",flags).SetValue(session,.0000001f);
            yield return null;yield return null;
            Assert.AreEqual(1,Time.timeScale);Assert.IsTrue(session.Player.ControlsEnabled);
            Assert.IsFalse(stopped.ControlsEnabled);
            session.SetPaused(true);session.ResumeSafely();session.ReturnToMenu();
            Assert.AreEqual(1,Time.timeScale);Assert.AreEqual(0,session.ResumeRemaining);
        }

        [UnityTest]
        public IEnumerator ActiveControllerDisconnectPausesAndFeedbackStops()
        {
            var pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
            pad.MakeCurrent();
            try
            {
                root=new GameObject("Controller safety",typeof(GameSession),typeof(PlayerFeedback));
                var session=root.GetComponent<GameSession>();session.StartMatch(GameMode.FreePlay,0,0,Color.cyan);
                yield return null;yield return null;
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                typeof(GameSession).GetField("countdown",flags).SetValue(session,0f);
                session.Player.ControlsEnabled=true;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.East));
                yield return null;yield return null;
                Assert.AreEqual(pad.deviceId,session.Player.LastInputDeviceId);
                var feedback=root.GetComponent<PlayerFeedback>();Assert.Greater(feedback.LowMotor,0);
                UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);
                Assert.IsTrue(session.Paused);Assert.IsTrue(session.PauseReason.Contains("disconnected"));
                yield return null;
                Assert.AreEqual(0,feedback.LowMotor);Assert.AreEqual(0,feedback.HighMotor);
                Assert.IsFalse(session.Player.ControlsEnabled);
                session.SetPaused(false);Assert.IsFalse(session.Paused);
            }
            finally { if(pad.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(pad); }
        }

        [UnityTest]
        public IEnumerator EliminationStartsFromMenuWinsAwardsAndRestarts()
        {
            root=new GameObject("Elimination menu test",typeof(GameBootstrap));
            yield return null;yield return null;
            UnityEngine.UI.Button Find(string prefix)
            {
                foreach(var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                    if(button.name.StartsWith(prefix))return button;
                Assert.Fail("Missing button: "+prefix);return null;
            }
            Find("PLAY").onClick.Invoke();yield return null;
            for(int i=0;i<6;i++)Find("Mode:").onClick.Invoke();
            Find("START EVENT").onClick.Invoke();yield return null;
            var session=GameSession.Instance;
            Assert.AreEqual(GameMode.Elimination,session.CurrentMode);Assert.AreEqual(4,session.Vehicles.Count);Assert.IsTrue(session.IsCircuit);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var progress=(RaceProgress)typeof(GameSession).GetField("playerProgress",flags).GetValue(session);
            progress.Cross(1,true);
            typeof(GameSession).GetField("countdown",flags).SetValue(session,0f);
            typeof(GameSession).GetField("elapsed",flags).SetValue(session,135f);
            typeof(GameSession).GetMethod("UpdateRace",flags).Invoke(session,null);
            yield return null;
            Assert.IsTrue(session.Finished);Assert.IsTrue(session.ResultWon);Assert.AreEqual(1,session.ActiveRaceCars);
            Assert.AreEqual(360,session.ResultCredits);Assert.Contains("Last survivor",SaveStore.Current.achievements);
            foreach(var car in session.Vehicles)if(car!=session.Player)Assert.IsFalse(car.gameObject.activeSelf);
            Assert.IsNotNull(Find("RACE AGAIN"));Find("RACE AGAIN").onClick.Invoke();yield return null;
            Assert.IsFalse(session.Finished);Assert.AreEqual(4,session.ActiveRaceCars);Assert.AreEqual(0,session.Elapsed);
        }

        [UnityTest]
        public IEnumerator EliminationPauseFreezesClockAndPlayerLossPaysNoCredits()
        {
            root=new GameObject("Elimination loss test",typeof(GameSession));var session=root.GetComponent<GameSession>();
            session.StartMatch(GameMode.Elimination,0,0,Color.cyan);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(GameSession).GetField("countdown",flags).SetValue(session,0f);
            typeof(GameSession).GetField("elapsed",flags).SetValue(session,44f);
            session.SetPaused(true);yield return new WaitForSecondsRealtime(.05f);
            Assert.AreEqual(44,session.Elapsed);Assert.AreEqual(4,session.ActiveRaceCars);
            session.SetPaused(false);
            var rivals=(System.Collections.IList)typeof(GameSession).GetField("racers",flags).GetValue(session);
            foreach(var rival in rivals)((RaceProgress)rival.GetType().GetField("Progress").GetValue(rival)).Cross(1,true);
            typeof(GameSession).GetField("elapsed",flags).SetValue(session,45f);
            typeof(GameSession).GetMethod("UpdateRace",flags).Invoke(session,null);
            Assert.IsTrue(session.GameOver);Assert.AreEqual(0,session.ResultCredits);Assert.AreEqual(500,SaveStore.Current.garage.credits);
            Assert.AreEqual(0,SaveStore.Current.records[0].finishes);
            Assert.IsFalse(session.Player.ControlsEnabled);
        }

        [UnityTest]
        public IEnumerator SlipstreamBuildsBehindAlignedCarAndDropsWhenLeaderTurnsAway()
        {
            root=new GameObject("Slipstream integration",typeof(GameSession));var session=root.GetComponent<GameSession>();
            session.StartMatch(GameMode.Championship,0,0,Color.cyan);yield return null;
            var follower=session.Player;var leader=session.Vehicles[1];
            Vector3 forward=(session.Circuit[1]-session.Circuit[0]).normalized;
            Quaternion facing=Quaternion.LookRotation(forward);
            Vector3 start=session.Circuit[0]+Vector3.up*1.2f;
            follower.Respawn(start,facing);leader.Respawn(start+forward*12,facing);
            follower.ControlsEnabled=leader.ControlsEnabled=true;
            typeof(VehicleController).GetProperty("Grounded").SetValue(follower,true);
            typeof(VehicleController).GetProperty("Grounded").SetValue(leader,true);
            follower.GetComponent<Rigidbody>().linearVelocity=forward*20;leader.GetComponent<Rigidbody>().linearVelocity=forward*20;
            Physics.SyncTransforms();
            var update=typeof(GameSession).GetMethod("UpdateSlipstreams",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            for(int i=0;i<30;i++)update.Invoke(session,null);
            Assert.Greater(follower.DraftStrength,0);
            leader.Respawn(start+forward*12,Quaternion.LookRotation(-forward));
            for(int i=0;i<200;i++)update.Invoke(session,null);
            Assert.AreEqual(0,follower.DraftStrength);
            session.StartMatch(GameMode.TimeAttack,0,0,Color.cyan);update.Invoke(session,null);
            Assert.AreEqual(0,session.Player.DraftStrength);
        }

        [UnityTest]
        public IEnumerator DealershipAndWorkshopPurchaseThroughMenusAndSwitchCars()
        {
            SaveStore.Current.garage.Credit(1000);
            root=new GameObject("Garage menu purchase test",typeof(GameBootstrap));
            yield return null;yield return null;
            void Click(string prefix)
            {
                foreach(var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                    if(button.name.StartsWith(prefix)){Assert.IsTrue(button.interactable);button.onClick.Invoke();return;}
                Assert.Fail("Missing button: "+prefix);
            }
            Click("GARAGE");yield return null;
            Click("VEHICLE DEALERSHIP");yield return null;
            Click("BUY BULWARK");yield return null;
            Click("CONFIRM PURCHASE");yield return null;
            Assert.AreEqual(2,SaveStore.Current.UnlockedVehicles);Assert.AreEqual(1,SaveStore.Current.selectedVehicle);
            Click("BACK");yield return null;
            Click("PERFORMANCE WORKSHOP");yield return null;
            Click("UPGRADE ENGINE");yield return null;
            Click("CONFIRM UPGRADE");yield return null;
            Assert.AreEqual(1,SaveStore.Current.garage.Level(1,0));
            Click("Vehicle:");yield return null;yield return null;
            Assert.AreEqual(0,SaveStore.Current.selectedVehicle);
            SaveStore.Reload();Assert.AreEqual(850,SaveStore.Current.garage.credits);
            Assert.AreEqual(1,SaveStore.Current.garage.Level(1,0));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator UpgradesApplyToDrivingButTimeAttackRemainsStock()
        {
            root=new GameObject("Workshop stats test",typeof(GameSession));
            var session=root.GetComponent<GameSession>();var template=Resources.Load<VehicleStats>("VehicleStats/Viper");
            SaveStore.Current.garage.upgrades[0]=3;SaveStore.Current.garage.upgrades[1]=2;SaveStore.Current.garage.upgrades[2]=1;
            session.StartMatch(GameMode.FreePlay,0,0,Color.cyan);
            Assert.AreEqual(template.MaxSpeed*1.09f,session.Player.Stats.MaxSpeed,.001f);
            Assert.AreEqual(template.Grip*1.16f,session.Player.Stats.Grip,.001f);
            Assert.AreEqual(template.BoostForce*1.08f,session.Player.Stats.BoostForce,.001f);
            session.StartMatch(GameMode.TimeAttack,0,0,Color.cyan);
            Assert.AreEqual(template.MaxSpeed,session.Player.Stats.MaxSpeed,.001f);
            Assert.AreEqual(template.Grip,session.Player.Stats.Grip,.001f);
            Assert.AreEqual(template.BoostForce,session.Player.Stats.BoostForce,.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CareerMedalBonusIsPaidOnlyWhenImproved()
        {
            root=new GameObject("Career rewards test",typeof(GameSession));var session=root.GetComponent<GameSession>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            for(int run=0;run<2;run++)
            {
                session.StartCareerEvent(0,0,Color.cyan);
                typeof(GameSession).GetField("elapsed",flags).SetValue(session,120f);
                var progress=(RaceProgress)typeof(GameSession).GetField("playerProgress",flags).GetValue(session);
                while(!progress.Complete)progress.Cross(progress.NextCheckpoint,true);
                typeof(GameSession).GetMethod("UpdateRace",flags).Invoke(session,null);
                Assert.IsTrue(session.Finished);Assert.AreEqual(3,session.LastCareerMedal);
                Assert.AreEqual(run==0?300:0,session.MedalCredits);
                Assert.AreEqual(run==0?700:400,session.ResultCredits);
                yield return null;
            }
            SaveStore.Reload();Assert.AreEqual(1600,SaveStore.Current.garage.credits);
        }

        [UnityTest]
        public IEnumerator CameraLookClampsRecentresAndSupportsInvertedY()
        {
            root=new GameObject("Camera orbit test",typeof(Camera),typeof(FollowCamera));
            var follow=root.GetComponent<FollowCamera>();
            follow.ApplyLook(new Vector2(1,1),1,false);
            Assert.Greater(follow.LookAngles.x,0);Assert.AreEqual(45,follow.LookAngles.y);
            follow.ApplyLook(Vector2.zero,0,true);Assert.AreEqual(Vector2.zero,follow.LookAngles);
            SaveStore.Current.invertCameraY=true;
            follow.ApplyLook(Vector2.up,1,false);Assert.AreEqual(-25,follow.LookAngles.y);
            for(int i=0;i<50;i++)follow.ApplyLook(Vector2.zero,.1f,false);
            Assert.AreEqual(Vector2.zero,follow.LookAngles);
            follow.ApplyLook(Vector2.right,0,false);Assert.AreEqual(Vector2.zero,follow.LookAngles);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SoundtrackUsesCodeLibraryAndHasNoPlayerImportControls()
        {
            string folder=Path.Combine(directory,"CustomMusic","Menu");Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"Ignored.ogg"),"not an audio file");
            root=new GameObject("Bundled music test",typeof(GameBootstrap));
            yield return null;yield return null;
            Assert.AreEqual(MusicLibrary.MenuTracks.Length,SoundDirector.Instance.MenuTrackCount);
            Assert.AreEqual(MusicLibrary.RaceTracks.Length,SoundDirector.Instance.RaceTrackCount);
            foreach(var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if(button.name=="SOUNDTRACK"){button.onClick.Invoke();break;}
            yield return null;
            foreach(var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
            {
                Assert.AreNotEqual("OPEN CUSTOM MUSIC FOLDER",button.name);
                Assert.AreNotEqual("RELOAD CUSTOM MUSIC",button.name);
            }
        }

        [UnityTest]
        public IEnumerator RechargeLaneRestoresBoostThroughItsTrigger()
        {
            root=new GameObject("Recharge lane test",typeof(GameSession));
            var session=root.GetComponent<GameSession>();session.StartMatch(GameMode.TimeAttack,0,0,Color.cyan);
            typeof(GameSession).GetField("countdown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(session,0f);
            session.Player.ControlsEnabled=true;
            typeof(VehicleController).GetProperty("BoostAmount").SetValue(session.Player,.1f);
            var pad=Object.FindFirstObjectByType<TrackBoostPad>();Assert.IsNotNull(pad);
            session.Player.Respawn(pad.transform.position,Quaternion.identity);
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return null;
            Assert.GreaterOrEqual(session.Player.BoostAmount,.55f);
            Assert.Less(session.Player.BoostAmount,.8f,"Multiple car colliders must not recharge repeatedly.");
            StringAssert.Contains("BOOST RECHARGED",session.RaceNotice);
        }

        [UnityTest]
        public IEnumerator CustomGraphicsProvideRenderersAndCoursePreviewSurvivesClipping()
        {
            root = new GameObject("Custom graphics regression", typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var mask = new GameObject("Clipped area", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D));
            mask.transform.SetParent(root.transform, false);
            mask.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 500);
            foreach (var type in new[] { typeof(CoursePreview), typeof(MenuBackdrop), typeof(RaceMap) })
            {
                // Deliberately omit CanvasRenderer: the component dependency must supply it.
                var graphic = new GameObject(type.Name, typeof(RectTransform));
                graphic.transform.SetParent(mask.transform, false);
                graphic.AddComponent(type);
                graphic.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 240);
                Assert.IsNotNull(graphic.GetComponent<CanvasRenderer>(), type.Name);
                if (graphic.TryGetComponent<CoursePreview>(out var preview)) preview.Configure(0, true);
            }
            Canvas.ForceUpdateCanvases();
            yield return null;
            var course = root.GetComponentInChildren<CoursePreview>();
            course.Configure(3, false);
            Canvas.ForceUpdateCanvases();
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DrivingSchoolCompletesSavesAndOffersCareer()
        {
            root = new GameObject("Driving school test"); root.AddComponent<GameBootstrap>();
            yield return null; yield return null;
            UnityEngine.UI.Button school = null;
            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if (button.name == "DRIVING SCHOOL") school=button;
            Assert.IsNotNull(school);school.onClick.Invoke();yield return null;
            var session=GameSession.Instance;
            Assert.IsTrue(session.IsTraining);
            var lesson=session.Lesson;
            for(int i=0;i<22;i++)lesson.Tick(.1f,30,false,true,true,false);
            for(int i=0;i<12;i++)lesson.Tick(.1f,30,true,true,true,false);
            lesson.Tick(.1f,0,false,true,true,false);
            for(int i=0;i<4;i++)lesson.Tick(.1f,0,false,false,true,false);
            lesson.Tick(.1f,0,false,true,true,false);
            lesson.Tick(.1f,0,false,true,true,true);
            typeof(GameSession).GetField("countdown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(session,0f);
            yield return null;yield return null;
            Assert.IsTrue(session.Finished);Assert.IsTrue(SaveStore.Current.trainingComplete);
            SaveStore.Reload();Assert.IsTrue(SaveStore.Current.trainingComplete);
            UnityEngine.UI.Button career=null;
            foreach(var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if(button.name=="START CAREER")career=button;
            Assert.IsNotNull(career);career.onClick.Invoke();yield return null;yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<CoursePreview>());
            Assert.IsFalse(session.Running);
        }

        [UnityTest]
        public IEnumerator ExpandedArenaHasMatchingWallsGoalsAndPlayableSpace()
        {
            root=new GameObject("Expanded arena",typeof(GameSession));var session=root.GetComponent<GameSession>();
            session.StartMatch(GameMode.ArenaClash,3,0,Color.cyan);
            yield return null;
            var floor=GameObject.Find("Arena floor").GetComponent<Collider>();
            Assert.AreEqual(126,floor.bounds.size.x,.1f);Assert.AreEqual(140,floor.bounds.size.z,.1f);
            Assert.AreEqual(63,GameObject.Find("East wall").transform.position.x,.1f);
            Assert.IsFalse(session.IsOutsidePlayableArea(new Vector3(55,1,60)));
            Assert.IsTrue(session.IsOutsidePlayableArea(new Vector3(66,1,0)));
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(GameSession).GetField("countdown",flags).SetValue(session,0f);
            var core=GameObject.Find("Energy core").GetComponent<Rigidbody>();
            core.position=new Vector3(0,2,45);core.linearVelocity=Vector3.zero;
            typeof(GameSession).GetMethod("UpdateArena",flags).Invoke(session,null);
            Assert.AreEqual(0,session.PlayerScore,"The old goal position is now the field.");
            core.position=new Vector3(0,2,45*CircuitLayout.ArenaExpansion);
            typeof(GameSession).GetMethod("UpdateArena",flags).Invoke(session,null);
            Assert.AreEqual(1,session.PlayerScore,"The expanded goal must score.");
        }

        [UnityTest]
        public IEnumerator EscapedArenaCoreResetsWithoutScoring()
        {
            root=new GameObject("Core recovery test");var session=root.AddComponent<GameSession>();
            session.StartMatch(GameMode.ArenaClash,3,0,Color.cyan);
            typeof(GameSession).GetField("countdown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(session,0f);
            var core=GameObject.Find("Energy core").GetComponent<Rigidbody>();
            foreach(var position in new[]{new Vector3(49*CircuitLayout.ArenaExpansion,2,0),new Vector3(0,2,60*CircuitLayout.ArenaExpansion),new Vector3(0,-6,45)})
            {
                core.position=position;core.linearVelocity=Vector3.zero;
                yield return null;yield return new WaitForFixedUpdate();yield return null;
                Assert.Less(core.position.magnitude,10);
                Assert.AreEqual(0,session.PlayerScore);Assert.AreEqual(0,session.RivalScore);
            }
        }

        [UnityTest]
        public IEnumerator SinglePlayerMenuHasCareerAndNoNetworkComponentOrButton()
        {
            root = new GameObject("Single player menu test"); root.AddComponent<GameBootstrap>();
            yield return null; yield return null;
            UnityEngine.UI.Button career = null;
            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
            {
                Assert.AreNotEqual("MULTIPLAYER", button.name);
                if (button.name == "CAREER TOUR") career = button;
            }
            foreach (var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                Assert.AreNotEqual("LanRaceSession", component.GetType().Name);
            Assert.IsNotNull(career); career.onClick.Invoke(); yield return null;
            UnityEngine.UI.Button start = null;
            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if (button.name == "START CAREER EVENT") start = button;
            Assert.IsNotNull(start); start.onClick.Invoke(); yield return null;
            Assert.AreEqual(0, GameSession.Instance.CareerEventIndex);
            Assert.AreEqual(GameMode.TimeAttack, GameSession.Instance.CurrentMode);
            Assert.IsFalse(GameSession.Instance.NetworkControlled);
        }

        [UnityTest]
        public IEnumerator EscapedCarReturnsWithoutAdvancingCheckpointAndFailureOffersRetry()
        {
            root = new GameObject("Boundary and game over test"); root.AddComponent<GameBootstrap>();
            yield return null; yield return null;
            var session = GameSession.Instance;
            session.StartMatch(GameMode.TimeAttack, 0, 0, Color.cyan);
            typeof(GameSession).GetField("countdown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(session, 0f);
            int checkpoint = session.NextCheckpoint;
            session.Player.Respawn(new Vector3(240, 1, 240), Quaternion.identity);
            Assert.AreEqual(new Vector3(240, 1, 240), session.Player.PhysicsPosition,
                "Rules must observe a teleport immediately, without waiting for render interpolation.");
            yield return null; yield return new WaitForFixedUpdate(); yield return null;
            Assert.IsFalse(session.IsOutsidePlayableArea(session.Player.transform.position));
            Assert.IsTrue(session.RecoveringFromBoundary);
            Assert.AreEqual(checkpoint, session.NextCheckpoint);
            for (int frame = 0; frame < 3; frame++)
            {
                yield return new WaitForFixedUpdate(); yield return null;
                Assert.AreEqual(checkpoint, session.NextCheckpoint, "Interpolation after recovery must not cross a gate.");
            }
            Assert.IsFalse(session.Finished);
            typeof(GameSession).GetField("elapsed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(session, 601f);
            yield return null; yield return null;
            Assert.IsTrue(session.GameOver);
            Assert.IsFalse(session.Player.ControlsEnabled);
            bool heading = false;
            foreach (var label in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
                heading |= label.text == "GAME OVER";
            Assert.IsTrue(heading);
            UnityEngine.UI.Button retry = null;
            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if (button.name == "TRY AGAIN") retry = button;
            Assert.IsNotNull(retry);
            retry.onClick.Invoke(); yield return null;
            Assert.IsFalse(session.Finished);
            Assert.AreEqual(1, session.PlayerLap);
        }

        [UnityTest]
        public IEnumerator BoundariesCoverAllThemesAndAllowRoadJumps()
        {
            root = new GameObject("All boundary test");
            var session = root.AddComponent<GameSession>();
            for (int environment = 0; environment < 5; environment++)
            {
                session.StartMatch(GameMode.TimeAttack, environment, 0, Color.cyan);
                foreach (var point in session.Circuit)
                    Assert.IsFalse(session.IsOutsidePlayableArea(point + Vector3.up * 8), "Legal jump rejected");
                Assert.IsTrue(session.IsOutsidePlayableArea(new Vector3(240, 0, 240)));
                Assert.IsTrue(session.IsOutsidePlayableArea(session.Circuit[0] + Vector3.down * 5));
                session.StartMatch(GameMode.FreePlay, environment, 0, Color.cyan);
                Assert.IsFalse(session.IsOutsidePlayableArea(new Vector3(40*CircuitLayout.ArenaExpansion, 8, 45*CircuitLayout.ArenaExpansion)));
                Assert.IsTrue(session.IsOutsidePlayableArea(new Vector3(47*CircuitLayout.ArenaExpansion, 1, 0)));
                Assert.IsTrue(session.IsOutsidePlayableArea(new Vector3(0, 1, 52*CircuitLayout.ArenaExpansion)));
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SpawnsVehicleAndPauseMenuStateReturnsCleanly()
        {
            root = new GameObject("Test session");
            var session = root.AddComponent<GameSession>();
            session.StartMatch(GameMode.FreePlay, 3, 0, Color.cyan);
            yield return null;
            Assert.IsTrue(session.Running);
            Assert.IsNotNull(session.Player);
            Assert.IsNotNull(session.Player.GetComponent<Rigidbody>());
            session.SetPaused(true);
            Assert.IsTrue(session.Paused);
            Assert.AreEqual(0, Time.timeScale);
            session.SetPaused(false);
            Assert.AreEqual(1, Time.timeScale);
            session.ReturnToMenu();
            yield return null;
            Assert.IsFalse(session.Running);
            Assert.IsNull(session.Player);
        }

        [UnityTest]
        public IEnumerator ChampionshipSpawnsThreeOpponentsAndRestartCleansOldWorld()
        {
            root = new GameObject("Test championship");
            var session = root.AddComponent<GameSession>();
            session.StartMatch(GameMode.Championship, 0, 0, Color.cyan);
            yield return null;
            Assert.AreEqual(4, Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None).Length);
            session.RestartMatch();
            yield return null;
            Assert.AreEqual(4, Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1, session.PlayerLap);
        }

        [UnityTest]
        public IEnumerator SceneryBuildsFiniteGeometryForEveryThemeAndLayout()
        {
            root = new GameObject("Scenery rotation regression");
            var route = new System.Collections.Generic.List<Vector3> {
                new Vector3(-40, 0, -40), new Vector3(40, 0, -40),
                new Vector3(40, 0, 40), new Vector3(-40, 0, 40)
            };
            for (int theme = 0; theme < 5; theme++)
                foreach (bool racing in new[] { false, true })
                {
                    var layout = new GameObject("Theme " + theme + " racing " + racing);
                    layout.transform.SetParent(root.transform);
                    WorldScenery.Build(layout.transform, route, theme, racing);
                    yield return null;
                    var filters = layout.GetComponentsInChildren<MeshFilter>();
                    Assert.IsNotEmpty(filters, layout.name);
                    foreach (var filter in filters)
                    {
                        var mesh = filter.sharedMesh;
                        Assert.Greater(mesh.vertexCount, 0, filter.name);
                        Assert.Greater(mesh.bounds.size.sqrMagnitude, 0, filter.name);
                        foreach (var vertex in mesh.vertices)
                            Assert.IsFalse(float.IsNaN(vertex.sqrMagnitude) || float.IsInfinity(vertex.sqrMagnitude), filter.name);
                    }
                    LogAssert.NoUnexpectedReceived();
                    Object.Destroy(layout);
                    yield return null;
                }
        }

        [UnityTest]
        public IEnumerator EveryEnvironmentAndModeCreatesIntegratedWorld()
        {
            root = new GameObject("Test worlds");
            var session = root.AddComponent<GameSession>();
            for (int environment = 0; environment < 5; environment++)
                for (int mode = 0; mode < System.Enum.GetValues(typeof(GameMode)).Length; mode++)
                {
                    session.StartMatch((GameMode)mode, environment, environment % 6, Color.cyan);
                    yield return null;
                    Assert.IsTrue(session.Running);
                    Assert.IsNotNull(session.Player);
                    Assert.IsFalse(session.Finished);
                    Assert.IsFalse(float.IsNaN(session.Player.transform.position.x));
                    var world = session.Player.transform.parent;
                    Assert.IsNotNull(world.GetComponentInChildren<WorldScenery>());
                    Assert.GreaterOrEqual(world.GetComponentsInChildren<TextMesh>().Length, 2, "Each venue needs visible location signs.");
                    if (session.IsCircuit) Assert.AreEqual(CircuitLayout.Build(environment).Count, session.Circuit.Count);
                    var colliders = session.Player.transform.parent.GetComponentsInChildren<BoxCollider>();
                    Assert.IsNotEmpty(colliders);
                    foreach (var collider in colliders)
                    {
                        Quaternion rotation = collider.transform.rotation;
                        Assert.AreEqual(1f, Quaternion.Dot(rotation, rotation), .0001f, collider.name);
                    }
                }
        }

        [UnityTest]
        public IEnumerator BattleScoresImpactOnceDuringCooldown()
        {
            root = new GameObject("Test battle");
            var session = root.AddComponent<GameSession>();
            session.StartMatch(GameMode.BoostBattle, 3, 0, Color.cyan);
            yield return new WaitForSeconds(3.2f);
            VehicleController rival = null;
            foreach (var car in Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None))
                if (!car.IsPlayer) { rival = car; break; }
            Assert.IsNotNull(rival);
            session.Player.GetComponent<Rigidbody>().linearVelocity = Vector3.forward * 20;
            rival.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            int before = session.PlayerScore;
            session.RegisterImpact(session.Player, rival, 15);
            Assert.AreEqual(before + 1, session.PlayerScore);
            session.RegisterImpact(session.Player, rival, 15);
            Assert.AreEqual(before + 1, session.PlayerScore);
        }

        [UnityTest]
        public IEnumerator HomeTilesFitAndRadioPreviewRestoresMenuPlaylist()
        {
            root=new GameObject("Presentation regression",typeof(GameBootstrap));
            yield return null;yield return null;
            Canvas.ForceUpdateCanvases();
            var tiles=GameObject.Find("Home event tiles");Assert.IsNotNull(tiles);
            var scroll=tiles.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            var buttons=tiles.GetComponentsInChildren<UnityEngine.UI.Button>();Assert.AreEqual(10,buttons.Length);
            foreach(var button in buttons)
            {
                var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,button.transform);
                Assert.GreaterOrEqual(bounds.min.x,scroll.viewport.rect.xMin-1,button.name);
                Assert.LessOrEqual(bounds.max.x,scroll.viewport.rect.xMax+1,button.name);
                Assert.GreaterOrEqual(bounds.min.y,scroll.viewport.rect.yMin-1,button.name);
                Assert.LessOrEqual(bounds.max.y,scroll.viewport.rect.yMax+1,button.name);
            }
            UnityEngine.UI.Button Find(string name)
            {
                foreach(var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                    if(button.name==name)return button;
                Assert.Fail("Missing button: "+name);return null;
            }
            Find("SOUNDTRACK").onClick.Invoke();yield return null;
            Find("PREVIEW RACE PLAYLIST").onClick.Invoke();yield return null;yield return null;
            Assert.AreEqual("Race",SoundDirector.Instance.PlaylistName);
            Assert.IsFalse(GameSession.Instance.Running);
            Find("BACK").onClick.Invoke();yield return null;yield return null;
            Assert.AreEqual("Menu",SoundDirector.Instance.PlaylistName);
        }

        [UnityTest]
        public IEnumerator MainMenuStartsAnEventThroughItsButtons()
        {
            root = new GameObject("Test bootstrap");
            root.AddComponent<GameBootstrap>();
            yield return null;
            yield return null;
            UnityEngine.UI.Button play = null;
            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if (button.name == "PLAY") play = button;
            Assert.IsNotNull(play);
            play.onClick.Invoke();
            yield return null;
            UnityEngine.UI.Button start = null;
            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if (button.name == "START EVENT") start = button;
            Assert.IsNotNull(start);
            start.onClick.Invoke();
            yield return null;
            Assert.IsTrue(GameSession.Instance.Running);
            Assert.IsNotNull(GameSession.Instance.Player);
        }

        [UnityTest]
        public IEnumerator NetworkWorldHasNoLocalAiOrTimeAttackGhostAndDoesNotAutoStart()
        {
            root = new GameObject("Test network world");
            var session = root.AddComponent<GameSession>();
            foreach (var mode in new[] { GameMode.TimeAttack, GameMode.ArenaClash })
            {
                session.StartMatch(mode, 3, 0, Color.cyan, true);
                yield return null;
                Assert.IsTrue(session.NetworkControlled);
                Assert.AreEqual(1, Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None).Length);
                Assert.IsNull(Object.FindFirstObjectByType<GhostReplay>());
                Assert.IsFalse(session.Player.ControlsEnabled);
                session.SetPaused(true);
                Assert.IsFalse(session.Paused, "A local pause must not stop a network world.");
                Assert.AreEqual(1, Time.timeScale);
                Assert.AreEqual(0, session.Elapsed);
                if (mode == GameMode.ArenaClash) Assert.IsNotNull(session.NetworkCore);
                session.ReturnToMenu();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator NetworkProxyPresentationDoesNotEnableLocalPhysics()
        {
            root = new GameObject("Test proxy session");
            var session = root.AddComponent<GameSession>();
            session.StartMatch(GameMode.FreePlay, 3, 0, Color.cyan, true);
            var car = session.Player;
            car.GetComponent<Rigidbody>().isKinematic = true;
            car.SetNetworkPresentation(Vector3.forward * 20, .4f, true, true);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(72, car.SpeedKmh, .01f);
            Assert.AreEqual(.4f, car.BoostAmount, .01f);
            Assert.IsTrue(car.IsBoosting);
            Assert.IsFalse(car.ControlsEnabled);
            Assert.IsTrue(car.GetComponent<Rigidbody>().isKinematic);
        }

        [UnityTest]
        public IEnumerator SoundtrackImportsAndSwitchesBetweenMenuAndRace()
        {
            root = new GameObject("Test soundtrack");
            root.AddComponent<GameBootstrap>();
            yield return null;
            yield return null;
            var sound = SoundDirector.Instance;
            Assert.IsNotNull(sound);
            Assert.GreaterOrEqual(sound.MenuTrackCount, 1);
            Assert.GreaterOrEqual(sound.RaceTrackCount, 1);
            Assert.AreEqual("Menu", sound.PlaylistName);
            foreach (var clip in Resources.LoadAll<AudioClip>("Music"))
            {
                Assert.Greater(clip.length, .1f, clip.name);
                Assert.AreEqual(AudioClipLoadType.Streaming, clip.loadType, clip.name);
            }
            var session = GameSession.Instance;
            session.StartMatch(GameMode.FreePlay, 3, 0, Color.cyan);
            yield return null;
            yield return null;
            Assert.AreEqual("Race", sound.PlaylistName);
            string track = sound.NowPlaying;
            session.SetPaused(true);
            yield return null;
            Assert.AreEqual("Race", sound.PlaylistName);
            Assert.AreEqual(track, sound.NowPlaying);
            session.ReturnToMenu();
            yield return null;
            yield return null;
            Assert.AreEqual("Menu", sound.PlaylistName);
            var imported = Resources.Load<AudioClip>(MusicLibrary.MenuTracks[0]);
            Object.Destroy(sound);
            yield return null;
            Assert.IsTrue(imported != null, "Destroying the director must not destroy imported music assets.");
        }

        [UnityTest]
        public IEnumerator CareerTrophyCabinetShowsAllCupsAndReturnsToTheTour()
        {
            root = new GameObject("Trophy cabinet test"); root.AddComponent<GameBootstrap>();
            yield return null; yield return null;
            UnityEngine.UI.Button FindButton(string name)
            {
                foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                    if (button.name == name) return button;
                Assert.Fail("Missing button: " + name); return null;
            }
            FindButton("CAREER TOUR").onClick.Invoke(); yield return null;
            FindButton("TOUR PROGRESS").onClick.Invoke(); yield return null;
            string text = "";
            foreach (var label in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None)) text += label.text;
            StringAssert.Contains("ROOKIE CUP", text);
            StringAssert.Contains("VELOCITY FINALS", text);
            StringAssert.Contains("LOCKED", text);
            FindButton("BACK TO TOUR").onClick.Invoke(); yield return null;
            Assert.IsNotNull(FindButton("START CAREER EVENT"));
            Assert.IsFalse(GameSession.Instance.Running);
        }

        [UnityTest]
        public IEnumerator MenuFocusScrollsIntoViewAndReturningDoesNotLeakShowrooms()
        {
            root = new GameObject("Menu layout test"); root.AddComponent<GameBootstrap>();
            yield return null; yield return null;
            UnityEngine.UI.Button FindButton(string name)
            {
                foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                    if (button.name == name) return button;
                Assert.Fail("Missing button: " + name); return null;
            }
            for (int visit = 0; visit < 3; visit++)
            {
                FindButton("HOW TO PLAY").onClick.Invoke(); yield return null; yield return null;
                var back = FindButton("BACK");
                var scroll = back.GetComponentInParent<UnityEngine.UI.ScrollRect>();
                Assert.IsNotNull(scroll);
                Canvas.ForceUpdateCanvases();
                scroll.content.anchoredPosition = Vector2.zero;
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(back.gameObject);
                yield return null;
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, back.transform);
                Assert.GreaterOrEqual(bounds.min.y, scroll.viewport.rect.yMin - 1);
                Assert.LessOrEqual(bounds.max.y, scroll.viewport.rect.yMax + 1);
                Assert.AreEqual(1, Object.FindObjectsByType<GaragePreview>(FindObjectsSortMode.None).Length);
                back.onClick.Invoke(); yield return null; yield return null;
                Assert.AreEqual(1, Object.FindObjectsByType<GaragePreview>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(1, Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None).Length);
            }
        }

        [UnityTest]
        public IEnumerator RaceHudHidesWhilePausedAndRestoresOnResume()
        {
            root = new GameObject("HUD layout test"); root.AddComponent<GameBootstrap>();
            yield return null; yield return null;
            var session = GameSession.Instance;
            // Start through the menu so its showroom is disposed just as it is during real play.
            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if (button.name == "PLAY") { button.onClick.Invoke(); break; }
            yield return null;
            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if (button.name == "START EVENT") { button.onClick.Invoke(); break; }
            yield return null; yield return null;
            var hud = GameObject.Find("Speed and boost card"); Assert.IsNotNull(hud);
            session.SetPaused(true); yield return null; yield return null;
            Assert.IsFalse(hud.activeInHierarchy);
            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if (button.name == "RESUME") { button.onClick.Invoke(); break; }
            yield return null; yield return null;
            Assert.IsTrue(hud.activeInHierarchy);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(GameSession).GetField("elapsed",flags).SetValue(session,9f);
            yield return null;
            Assert.IsNull(GameObject.Find("Driving hints card"),"Opening help should leave the road clear after eight seconds.");
            Assert.IsNotNull(GameObject.Find("Race information card"));
            Assert.IsNotNull(GameObject.Find("Boost segment 11"));
            Assert.IsFalse(session.Paused);
        }

        [UnityTest]
        public IEnumerator WorldSignsFaceTheCameraAndHideWhenTooCloseOrBehind()
        {
            root = new GameObject("Sign readability test");
            var cameraObject = new GameObject("Sign camera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform); cameraObject.tag = "MainCamera";
            var sign = GameObject.CreatePrimitive(PrimitiveType.Quad); sign.transform.SetParent(root.transform);
            sign.AddComponent<ReadableWorldSign>();
            sign.transform.position = Vector3.forward * 50;
            yield return null; yield return null;
            Assert.IsTrue(sign.GetComponent<MeshRenderer>().enabled);
            Assert.Less(Quaternion.Angle(cameraObject.transform.rotation,sign.transform.rotation), .1f);
            sign.transform.position = Vector3.forward * 10;
            yield return null; yield return null;
            Assert.IsFalse(sign.GetComponent<MeshRenderer>().enabled);
            sign.transform.position = Vector3.back * 50;
            yield return null; yield return null;
            Assert.IsFalse(sign.GetComponent<MeshRenderer>().enabled);
        }

        [UnityTest]
        public IEnumerator ImportedVehiclesHaveReadableGeometryAndMeterScale()
        {
            root = new GameObject("Model import validation");
            foreach (string name in GameSession.VehicleNames)
            {
                var asset = Resources.Load<GameObject>("VehicleModels/" + name);
                Assert.IsNotNull(asset, name);
                var model = Object.Instantiate(asset, root.transform);
                yield return null;
                var renderers = model.GetComponentsInChildren<Renderer>();
                Assert.IsNotEmpty(renderers, name);
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                Assert.That(bounds.size.x, Is.InRange(1f, 4f), name + " width");
                Assert.That(bounds.size.z, Is.InRange(2.5f, 6f), name + " length");
                Assert.That(bounds.size.y, Is.InRange(.5f, 3f), name + " height");
                foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
                    Assert.IsTrue(filter.sharedMesh.isReadable, name + " must support batching");
                Object.Destroy(model);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AiStartsMovingAfterCountdown()
        {
            root = new GameObject("Test AI");
            var session = root.AddComponent<GameSession>();
            session.StartMatch(GameMode.Championship, 0, 0, Color.cyan);
            VehicleController opponent = null;
            foreach (var car in Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None))
                if (!car.IsPlayer) { opponent = car; break; }
            Assert.IsNotNull(opponent);
            Vector3 before = opponent.transform.position;
            yield return new WaitForSeconds(6);
            Assert.Greater(Vector3.Distance(before, opponent.transform.position), 2f);
        }
    }
}
