using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VelocityRivals.Tests
{
    public sealed class SessionTests
    {
        GameObject root;
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (root) Object.Destroy(root);
            var camera = Camera.main;
            if (camera) Object.Destroy(camera.gameObject);
            Time.timeScale = 1;
            yield return null;
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
