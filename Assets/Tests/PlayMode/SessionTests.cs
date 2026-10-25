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
        public IEnumerator EscapedArenaCoreResetsWithoutScoring()
        {
            root=new GameObject("Core recovery test");var session=root.AddComponent<GameSession>();
            session.StartMatch(GameMode.ArenaClash,3,0,Color.cyan);
            typeof(GameSession).GetField("countdown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(session,0f);
            var core=GameObject.Find("Energy core").GetComponent<Rigidbody>();
            foreach(var position in new[]{new Vector3(49,2,0),new Vector3(0,2,60),new Vector3(0,-6,45)})
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
                Assert.IsFalse(session.IsOutsidePlayableArea(new Vector3(40, 8, 45)));
                Assert.IsTrue(session.IsOutsidePlayableArea(new Vector3(47, 1, 0)));
                Assert.IsTrue(session.IsOutsidePlayableArea(new Vector3(0, 1, 52)));
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
                for (int mode = 0; mode < 6; mode++)
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
            Assert.GreaterOrEqual(sound.RaceTrackCount, 2);
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
            var imported = Resources.LoadAll<AudioClip>("Music/Menu")[0];
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
