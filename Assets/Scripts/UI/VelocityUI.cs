using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VelocityRivals
{
    /// <summary>Runtime-built controller-navigable UI; no hand-wired scene references.</summary>
    public sealed partial class VelocityUI : MonoBehaviour
    {
        private GameSession session;
        private Canvas canvas;
        private RectTransform panel;
        private Font font;
        private Text hud;
        private Text hint;
        private Text title;
        private Text nowPlaying;
        private Text mapLegend;
        private RectTransform soundtrackProgress;
        private Text countdownLabel;
        private int careerSelection;
        private int recordsPage;
        private GameObject mapPanel;
        private SoundDirector sound;
        private GameMode selectedMode;
        private int selectedEnvironment;
        private bool showingResults;
        private readonly Color cyan = new Color(0.12f, 0.9f, 0.95f);
        public static readonly Color[] Paints = {
            new Color(0.05f, 0.85f, 1), new Color(1, 0.18f, 0.32f), new Color(0.55f, 0.25f, 1),
            new Color(1, 0.75f, 0.1f), new Color(0.2f, 1, 0.4f), Color.white,
            new Color(1, 0.4f, 0.05f), new Color(0.12f, 0.15f, 0.22f)
        };
        private static readonly string[] PaintNames = { "Ion cyan", "Solar crimson", "Ultraviolet", "Championship gold", "Reactor green", "Arctic white", "Plasma orange", "Carbon midnight" };

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            session = FindFirstObjectByType<GameSession>();
            if (!session) session = gameObject.AddComponent<GameSession>();
            sound = FindFirstObjectByType<SoundDirector>();
            if (!sound) sound = gameObject.AddComponent<SoundDirector>();
            if (!FindFirstObjectByType<EventSystem>())
            {
                var events = new GameObject("UI input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var canvasObject = new GameObject("Velocity UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            hud = Label(canvas.transform, "", 28, 160);
            var hr = hud.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0, 1);
            hr.pivot = new Vector2(0, 1);
            hr.anchoredPosition = new Vector2(35, -24);
            hr.sizeDelta = new Vector2(1450, 260);
            hud.alignment = TextAnchor.UpperLeft;
            hint = Label(canvas.transform, "", 20, 55);
            var br = hint.rectTransform;
            br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(1, 0);
            br.pivot = new Vector2(0.5f, 0);
            br.anchoredPosition = new Vector2(0, 20); br.sizeDelta = new Vector2(-50, 65);
            mapPanel = new GameObject("Race map", typeof(RectTransform), typeof(CanvasRenderer), typeof(RaceMap));
            mapPanel.transform.SetParent(canvas.transform, false);
            var map = mapPanel.GetComponent<RaceMap>();
            map.Session = session;
            map.raycastTarget = false;
            var mapRect = map.rectTransform;
            mapRect.anchorMin = mapRect.anchorMax = mapRect.pivot = Vector2.one;
            mapRect.anchoredPosition = new Vector2(-30, -30);
            mapRect.sizeDelta = new Vector2(220, 220);
            var mapCaption = Label(mapPanel.transform, "YOU ▲   •   NEXT GATE ■", 15, 55);
            mapLegend=mapCaption;
            mapCaption.rectTransform.anchorMin = new Vector2(0, 0);
            mapCaption.rectTransform.anchorMax = new Vector2(1, 0);
            mapCaption.rectTransform.pivot = new Vector2(.5f, 1);
            mapCaption.rectTransform.sizeDelta = new Vector2(30, 55);
            mapPanel.SetActive(false);
            BuildRaceCards();
            countdownLabel = Label(canvas.transform, "", 96, 150);
            countdownLabel.rectTransform.anchorMin = countdownLabel.rectTransform.anchorMax = new Vector2(.5f,.5f);
            countdownLabel.rectTransform.sizeDelta = new Vector2(600,180);
            countdownLabel.rectTransform.anchoredPosition = new Vector2(0,100);
            countdownLabel.color = cyan;
            session.MatchEnded += OnMatchEnded;
            ApplyQuality(SaveStore.Current.quality);
            ApplyDisplay();
            MainMenu();
        }

        private void Update()
        {
            if (!session) return;
            RefreshPresentation();
            countdownLabel.fontSize=session.ResumeRemaining>0?54:96;
            countdownLabel.text = session.Running && !session.Paused && !session.Finished && session.CountdownRemaining > 0 ? Mathf.CeilToInt(session.CountdownRemaining).ToString() : "";
            if (session.ResumeRemaining > 0) countdownLabel.text="RESUME IN\n"+Mathf.CeilToInt(session.ResumeRemaining);
            if (session.ActionEnabled && session.Elapsed < .7f) countdownLabel.text = "GO!";
            mapPanel.SetActive(session.Running && !session.Finished && !session.Paused);
            if (nowPlaying) nowPlaying.text = sound.PlaylistName.ToUpperInvariant() + " PLAYLIST\n" + sound.NowPlaying;
            if (soundtrackProgress) soundtrackProgress.anchorMax=new Vector2(sound.TrackProgress,1);
            if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame) sound.NextTrack();

            bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool back = escape || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
            bool start = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
            if (session.Running && !session.Finished)
            {
                if (session.Paused && back) { if (pageBack != null) pageBack(); else Resume(); }
                else if (escape || start) { if (session.Paused) Resume(); else PauseMenu(); }
            }
            else if (back && pageBack != null) pageBack();
            if (session.Paused && !panel) PauseMenu();
            if (session.Running && session.Player)
            {
                mapLegend.text=session.LocationName.ToUpperInvariant()+"\nYOU ▲   /   NEXT GATE ■";
                string clock = session.CurrentMode == GameMode.FreePlay ? "PRACTICE" : session.CurrentMode==GameMode.Elimination ? $"{session.EliminationRemaining:0.0}s TO CUTOFF" : $"{session.RemainingTime:0.0}s REMAINING";
                hud.text = clock + "\n" + CompactRaceStatus();
                hint.text = "WASD / triggers + left stick: drive   •   Shift / B: boost   •   Space / A: jump   •   Ctrl / X: drift\nR / Y: recover   •   Q/E / LB + stick: air roll   •   Esc / Start: pause";
                if (session.Elapsed < 8 && !session.Finished) hint.text = session.EventObjective + "\nWASD / triggers + stick: drive • Shift / B: boost • Esc / Start: pause";
                if (session.RecoveringFromBoundary) hint.text = "OUT OF BOUNDS — RETURNED TO SAFETY\nYour checkpoint progress is preserved. The event timer keeps running.";
                else if (session.IsTraining) hint.text = session.Lesson.Instruction;
            }
            else { hud.text = ""; hint.text = ""; }
        }

        private void OnDestroy() { if (session) session.MatchEnded -= OnMatchEnded; }

        private void ClearPanel()
        {
            if (pageRoot) { pageRoot.gameObject.SetActive(false); Destroy(pageRoot.gameObject); }
            if(sound)sound.EndPlaylistPreview();
            soundtrackProgress=null;
            title = null; nowPlaying = null; panel = null; pageRoot = null; pageBack = null;
            showcaseName = null; showcaseStats = null; showcaseProgress = null; pageFade = null; footerRadio=null;
        }
        private static string LapDisplay(float seconds) => seconds > 0 ? seconds.ToString("0.00") + "s" : "—";
        private void Page(string heading, string subtitle) => BuildPageFrame(heading, subtitle);

        private Text Label(Transform parent, string text, int size, float height)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size;
            label.color = Color.white; label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return label;
        }

        private Button Button(string caption, Action action)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(panel, false);
            go.GetComponent<Image>().color = Color.white;
            go.GetComponent<LayoutElement>().preferredHeight = 62;
            go.GetComponent<LayoutElement>().minHeight = 62;
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(.055f,.095f,.14f); colors.highlightedColor = new Color(.08f,.28f,.35f);
            colors.selectedColor = new Color(.08f,.28f,.35f); colors.pressedColor = new Color(.12f,.4f,.46f);
            colors.disabledColor = new Color(.04f,.055f,.075f); colors.fadeDuration=.12f;
            button.colors = colors;
            var text = Label(go.transform, caption, 22, 0);text.fontStyle=FontStyle.Bold;
            Stretch(text.rectTransform, 12);
            text.rectTransform.offsetMin = new Vector2(24, 8); text.rectTransform.offsetMax = new Vector2(-38, -8);
            text.alignment = TextAnchor.MiddleLeft;
            var edge=Plate("Button edge",go.transform,new Color(.2f,.38f,.48f,.5f));
            edge.anchorMin=new Vector2(0,1);edge.anchorMax=Vector2.one;edge.offsetMin=new Vector2(0,-1);edge.offsetMax=Vector2.zero;
            var accent = Plate("Focus accent", go.transform, cyan);
            accent.anchorMin = Vector2.zero; accent.anchorMax = new Vector2(0,1); accent.offsetMin = Vector2.zero; accent.offsetMax = new Vector2(4,0);
            var focus = go.AddComponent<MenuFocusReveal>(); focus.Accent = accent.GetComponent<Image>(); focus.Accent.enabled = false;
            if (caption == "BACK" || caption.StartsWith("BACK TO") || (caption == "MAIN MENU" && pageBack == null)) pageBack = action;
            button.onClick.AddListener(() => { sound.Click(); action(); });
            return button;
        }

        private static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin); rect.offsetMax = new Vector2(-margin, -margin);
        }
        private void Focus(Button button)
        {
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
        private Button Cycle(string name, IList<string> values, Func<int> read, Action<int> write)
        {
            Button button = null;
            button = Button(name + ": " + values[Mathf.Clamp(read(), 0, values.Count - 1)], () => {
                int next = (read() + 1) % values.Count;
                button.GetComponentInChildren<Text>().text = name + ": " + values[next];
                write(next); // A selection callback may replace the page and deactivate this button.
            });
            return button;
        }

        private void MainMenu()
        {
            showingResults = false;
            if (session.Running || session.Finished) session.ReturnToMenu();
            Page("Velocity Rivals", $"SINGLE PLAYER • LEVEL {SaveStore.Current.Level} • {SaveStore.Current.xp} XP\n{SaveStore.Current.career.TotalMedals}/{CareerTour.Count * 3} career medal points");
            panel.GetComponent<VerticalLayoutGroup>().spacing = 8;
            MakeHomeLayout();
            Focus(HomeTile("CAREER TOUR","12 EVENTS / 4 CUPS", () => { careerSelection = SaveStore.Current.career.NextEvent; CareerMenu(); }, true));
            HomeTile("PLAY","7 MODES / 5 WORLDS", ModeMenu);
            HomeTile("GARAGE","CARS / PAINT / UPGRADES", Garage);
            HomeTile("RECORDS & ACHIEVEMENTS","YOUR PERSONAL BESTS", Records);
            var training=HomeTile("DRIVING SCHOOL",SaveStore.Current.trainingComplete ? "TRAINING COMPLETE" : "NEW DRIVER? START HERE", StartDrivingSchool);
            if(!SaveStore.Current.trainingComplete && SaveStore.Current.events==0)Focus(training);
            HomeTile("SOUNDTRACK","VELOCITY RADIO / PLAYLISTS", () => Soundtrack(false));
            HomeTile("SETTINGS","DISPLAY / AUDIO / CAMERA", () => Settings(false));
            HomeTile("HOW TO PLAY","CONTROLS / EVENT RULES", () => HowToPlay(false));
            HomeTile("CREDITS","THE GAME / THE MUSIC", Credits);
            HomeTile("QUIT","SAVE AND EXIT", ConfirmQuit);
            if (!string.IsNullOrEmpty(SaveStore.LastError)) Label(panel, SaveStore.LastError, 18, 70);
            pageBack = ConfirmQuit;
        }

        private Button HomeTile(string caption,string detail,Action action,bool primary=false)
        {
            var button=Button(caption,action);
            var label=button.GetComponentInChildren<Text>();
            label.fontSize=caption.Length>20?18:23;
            label.rectTransform.offsetMin=new Vector2(22,42);label.rectTransform.offsetMax=new Vector2(-20,-12);
            var subtitle=FixedText(button.transform,detail,13,22,76,280,24,new Color(.58f,.75f,.83f));
            if(primary)
            {
                var colors=button.colors;colors.normalColor=new Color(.04f,.2f,.26f);button.colors=colors;
                subtitle.color=cyan;
            }
            return button;
        }

        private void ConfirmQuit()
        {
            Page("Leave the garage?", "Your completed events and garage choices are saved on this computer.");
            Focus(Button("BACK", MainMenu));
            Button("SAVE & QUIT", QuitGame);
        }
        private void QuitGame()
        {
            if (!SaveStore.Save())
            {
                Page("Progress could not be saved", SaveStore.LastError);
                Focus(Button("TRY SAVING AGAIN", QuitGame));
                Button("BACK", MainMenu);
                Button("QUIT WITHOUT SAVING", ExitApplication);
                return;
            }
            ExitApplication();
        }
        private void ExitApplication()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void CareerMenu()
        {
            if (session.Running || session.Finished) session.ReturnToMenu();
            var progress = SaveStore.Current.career;
            careerSelection = Mathf.Clamp(careerSelection, 0, progress.NextEvent);
            var entry = CareerTour.Get(careerSelection);
            Page(progress.Complete ? "Velocity Tour complete" : "Career Tour", $"{progress.TotalMedals}/{CareerTour.Count * 3} MEDAL POINTS • {entry.Cup.ToUpperInvariant()}\nBronze unlocks the next event. Replay any unlocked event for gold.");
            panel.GetComponent<VerticalLayoutGroup>().spacing = 8;
            Label(panel, $"EVENT {careerSelection + 1}/{CareerTour.Count} — {entry.Name}\n{GameSession.EnvironmentNames[entry.Environment]} • {CareerTour.MedalName(progress.medals[careerSelection])}\n{entry.Goal}", 26, 150);
            Focus(Button("START CAREER EVENT", StartCareer));
            if (careerSelection > 0) Button("PREVIOUS EVENT", () => { careerSelection--; CareerMenu(); });
            if (careerSelection < progress.NextEvent) Button("NEXT EVENT", () => { careerSelection++; CareerMenu(); });
            Button("GARAGE", () => Garage(CareerMenu));
            Button("TOUR PROGRESS", TourProgress);
            Button("MAIN MENU", MainMenu);
            ShowCourse(entry.Environment,entry.Mode);
        }

        private void TourProgress()
        {
            var progress = SaveStore.Current.career;
            Page("Your trophy cabinet", $"{progress.TotalMedals}/{CareerTour.Count * 3} medal points • Bronze opens the next event");
            for (int cup = 0; cup < 4; cup++)
            {
                string text = CareerTour.Get(cup * 3).Cup.ToUpperInvariant();
                for (int j = 0; j < 3; j++)
                {
                    int i = cup * 3 + j;
                    text += "\n" + CareerTour.Get(i).Name + " — " + (progress.IsUnlocked(i) ? CareerTour.MedalName(progress.medals[i]) : "LOCKED");
                }
                Label(panel, text, 23, 120);
            }
            Focus(Button("BACK TO TOUR", CareerMenu));
            if (progress.Complete) Button("TOUR FINALE", TourFinale);
        }

        private void TourFinale()
        {
            Page("Velocity champion", "ROOKIE • CHALLENGER • RIVAL • VELOCITY FINALS\nAll four cups complete. Your place in the tour is earned.");
            Label(panel, $"{SaveStore.Current.career.TotalMedals}/36 MEDAL POINTS\n{SaveStore.Current.UnlockedVehicles}/6 CARS UNLOCKED\nThank you for playing Velocity Rivals.\nReplay the tour to earn every gold medal, or set a new Time Attack record.", 27, 180);
            Focus(Button("VIEW TROPHIES", TourProgress));
            Button("CREDITS", Credits);
            Button("MAIN MENU", MainMenu);
        }

        private void StartCareer()
        {
            ClearPanel(); showingResults = false;
            var p = SaveStore.Current;
            session.StartCareerEvent(careerSelection, p.selectedVehicle, CosmeticPacks.SelectedColor(p));
            ApplyCarCosmetics();
        }

        private void ApplyCarCosmetics()
        {
            var p = SaveStore.Current;
            if (session.Player) session.Player.SetCosmetics(p.wheels, p.trail, p.decal, p.spoiler, p.finish);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }

        private void RestartCurrent()
        {
            ClearPanel(); showingResults = false; session.RestartMatch(); ApplyCarCosmetics();
        }

        private void ModeMenu()
        {
            Page("Choose your event", "Championship • arena sport • combat • stunts • time trial • training • elimination");
            Focus(Cycle("Mode", new[] { "Velocity Championship", "Arena Clash", "Boost Battle", "Stunt Rush", "Time Attack", "Free Play", "Elimination" }, () => (int)selectedMode, value => selectedMode = (GameMode)value));
            Cycle("Environment", GameSession.EnvironmentNames, () => selectedEnvironment, value => { selectedEnvironment = value; ShowCourse(selectedEnvironment,selectedMode); });
            Cycle("AI difficulty", new[] { "Rookie", "Rival", "Elite" }, () => SaveStore.Current.difficulty, value => { SaveStore.Current.difficulty = value; SaveStore.Save(); });
            var vehicles = new List<string>();
            for (int i = 0; i < SaveStore.Current.UnlockedVehicles; i++) vehicles.Add(GameSession.VehicleNames[i]);
            Cycle("Vehicle", vehicles, () => SaveStore.Current.selectedVehicle, value => { SaveStore.Current.selectedVehicle = value; SaveStore.Save(); ShowCourse(selectedEnvironment,selectedMode); });
            var briefing = Label(panel,EventBrief(selectedMode),23,170);
            // Keep the briefing live when cycling the mode without rebuilding focus or the selected button.
            var modeButton = panel.GetChild(0).GetComponent<UnityEngine.UI.Button>();
            modeButton.onClick.AddListener(() => { briefing.text=EventBrief(selectedMode);ShowCourse(selectedEnvironment,selectedMode); });
            Button("START EVENT", StartMatch);
            Button("BACK", MainMenu);
            ShowCourse(selectedEnvironment,selectedMode);
        }

        private void StartMatch()
        {
            ClearPanel();
            showingResults = false;
            var p = SaveStore.Current;
            session.StartMatch(selectedMode, selectedEnvironment, p.selectedVehicle, CosmeticPacks.SelectedColor(p));
            if (session.Player) session.Player.SetCosmetics(p.wheels, p.trail, p.decal, p.spoiler, p.finish);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }

        private void Garage() => Garage(MainMenu);
        private void Garage(Action returnTo)
        {
            var p = SaveStore.Current;
            Page("Garage", $"{p.garage.credits:N0} CREDITS • {p.UnlockedVehicles}/6 CARS OWNED\nCustomise your car, install upgrades or visit the dealership.");
            panel.GetComponent<VerticalLayoutGroup>().spacing = 6;
            var vehicles = new List<string>();
            for (int i = 0; i < p.UnlockedVehicles; i++) vehicles.Add(GameSession.VehicleNames[i]);
            Focus(Cycle("Vehicle", vehicles, () => p.selectedVehicle, value => { p.selectedVehicle = value; SaveStore.Save(); }));
            Button("PERFORMANCE WORKSHOP",()=>Workshop(returnTo));
            Button("VEHICLE DEALERSHIP",()=>Dealership(returnTo));
            Button("COSMETIC STORE",()=>CosmeticStore(returnTo));
            Cycle("Paint", PaintNames, () => p.paint, value => { p.paint = value; p.cosmeticPaint = ""; SaveStore.Save(); });
            Cycle("Wheels", new[] { "Street", "Sport", "Aero" }, () => p.wheels, value => { p.wheels = value; SaveStore.Save(); });
            Cycle("Boost trail", new[] { "Ion", "Plasma", "Solar" }, () => p.trail, value => { p.trail = value; SaveStore.Save(); });
            Cycle("Decal", new[] { "Clean", "Twin stripes", "Race panel" }, () => p.decal, value => { p.decal = value; SaveStore.Save(); });
            Cycle("Spoiler", new[] { "Low profile", "Racing wing", "Aero fin" }, () => p.spoiler, value => { p.spoiler = value; SaveStore.Save(); });
            Cycle("Finish", new[] { "Gloss", "Matte", "Metallic" }, () => p.finish, value => { p.finish = value; SaveStore.Save(); });
            Button("BACK", returnTo);
            pageBack = returnTo;
        }

        private void Settings(bool paused)
        {
            var p = SaveStore.Current;
            Page("Settings", "Audio levels and quality are saved on this computer.");
            string[] volumes = { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };
            Focus(Cycle("Master volume", volumes, () => Mathf.RoundToInt(p.masterVolume * 10), value => { p.masterVolume = value / 10f; sound.ApplyVolumes(); SaveStore.Save(); }));
            Cycle("Music volume", volumes, () => Mathf.RoundToInt(p.musicVolume * 10), value => { p.musicVolume = value / 10f; sound.ApplyVolumes(); SaveStore.Save(); });
            Button("SOUNDTRACK", () => Soundtrack(paused));
            Cycle("Effects volume", volumes, () => Mathf.RoundToInt(p.effectsVolume * 10), value => { p.effectsVolume = value / 10f; sound.ApplyVolumes(); SaveStore.Save(); });
            Button("DISPLAY & EFFECTS", () => DisplaySettings(paused));
            Cycle("Steering sensitivity", new[] { "50%", "75%", "100%", "125%", "150%" }, () => Mathf.RoundToInt((p.steeringSensitivity - .5f) * 4), value => { p.steeringSensitivity = .5f + value * .25f; SaveStore.Save(); });
            Cycle("Camera sensitivity", new[] { "50%", "75%", "100%", "125%", "150%" }, () => Mathf.RoundToInt((p.cameraSensitivity - .5f) * 4), value => { p.cameraSensitivity = .5f + value * .25f; SaveStore.Save(); });
            Cycle("Controller vibration", new[] { "Off", "Low", "Medium", "High" }, () => Mathf.RoundToInt(p.vibration * 3), value => { p.vibration=value/3f; SaveStore.Save(); });
            Cycle("Camera vertical", new[] { "Normal", "Inverted" }, () => p.invertCameraY ? 1 : 0, value => { p.invertCameraY = value == 1; SaveStore.Save(); });
            Label(panel, "Keyboard: WASD drive, Shift boost, Space jump, Ctrl drift, Q/E roll, R recover.\nGamepad: RT/LT drive, stick steer, A jump, B boost, X drift, Y recover.\nEsc / Start pauses. Full controls are under HOW TO PLAY.", 22, 140);
            Button("BACK", () => { if (paused) PauseMenu(); else MainMenu(); });
        }

        private void DisplaySettings(bool paused)
        {
            var p = SaveStore.Current;
            Page("Display & effects", "Choose the detail and effects that feel comfortable on your computer.");
            Focus(Cycle("Graphics", new[] { "Performance", "Balanced", "Quality" }, () => p.quality, value => { p.quality = value; ApplyQuality(value); SaveStore.Save(); }));
            Cycle("Display", new[] { "Windowed", "Borderless fullscreen" }, () => p.fullscreen ? 1 : 0, value => { p.fullscreen = value == 1; ApplyDisplay(); SaveStore.Save(); });
            Cycle("Visual effects", new[] { "Full", "Reduced" }, () => p.reducedEffects ? 1 : 0, value => { p.reducedEffects = value == 1; SaveStore.Save(); });
            Label(panel, "Reduced effects turns off bloom, vignette, impact bursts, drift smoke, showroom rotation and boost camera widening.\nPerformance reduces render resolution, shadows and anti-aliasing, and turns off headlights.", 24, 140);
            Button("BACK", () => Settings(paused));
        }

        private static void ApplyDisplay()
        {
            if (!Application.isEditor) Screen.fullScreenMode = SaveStore.Current.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        private void Soundtrack(bool paused)
        {
            Page("Soundtrack", "VELOCITY RADIO • ambient / house / breakbeat / drum & bass / synthwave");
            nowPlaying = Label(panel, "", 32, 120);
            var progressTrack=Plate("Track playback progress",panel,new Color(.1f,.18f,.24f));
            progressTrack.gameObject.AddComponent<LayoutElement>().preferredHeight=6;
            soundtrackProgress=Plate("Track elapsed",progressTrack,cyan);Stretch(soundtrackProgress,0);
            Label(panel, $"{sound.MenuTrackCount} menu tracks • {sound.RaceTrackCount} race tracks\nEach playlist shuffles without repeating a track until the cycle ends. Press N to skip.", 24, 100);
            Focus(Button("NEXT TRACK", () => sound.NextTrack()));
            Button("PREVIEW MENU PLAYLIST",()=>sound.PreviewPlaylist(false));
            Button("PREVIEW RACE PLAYLIST",()=>sound.PreviewPlaylist(true));
            string[] volumes = { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };
            Cycle("Music volume", volumes, () => Mathf.RoundToInt(SaveStore.Current.musicVolume * 10), value => {
                SaveStore.Current.musicVolume = value / 10f; sound.ApplyVolumes(); SaveStore.Save();
            });
            Button("BACK", () => Settings(paused));
        }

        private void HowToPlay(bool paused)
        {
            Page("How to play", "Start with Free Play in Cyber Dome to learn your car.");
            Label(panel,"LOOK AROUND\nRight stick rotates the camera. Click the right stick (R3), or press C, to centre it.\nAfter releasing the stick, the camera returns behind your car.",23,115);
            Label(panel, "DRIVE\nW / S accelerate and reverse • A / D steer\nShift boost • Ctrl handbrake • Space jump, press again for an aerial jump\nQ / E roll in the air • R recover your car\nController: triggers + left stick, B boost, X drift, A jump, Y recover.", 24, 205);
            Label(panel, "WIN YOUR EVENT\nChampionship / Time Attack: follow the yellow map gate and complete three laps.\nArena Clash: push the white core into the orange goal. First to five wins.\nBoost Battle: hit opponents at speed; collect shields, overdrive and pulse pickups.\nStunt Rush: jump, rotate and land upright. Reach 1,000 points before time runs out.", 23, 215);
            Label(panel, "Esc / Start pauses. Earn XP to unlock cars in the garage.\nCyan on the map is you, orange is another vehicle, yellow is the next gate.", 23, 85);
            Label(panel,"CORNERING\nRelease boost before a bend. Steering retains control at speed and gently reduces speed in hard turns.\nTap Ctrl / X for a tighter drift; release it to regain grip. S / LT brakes and reverses.",23,135);
            Label(panel,"HELD ITEMS — CHAMPIONSHIP / ELIMINATION / BOOST BATTLE\nCollect one coloured crate, then press F / RB to activate it. Cyan: shield. Orange: overdrive. Purple: pulse.\nIn races, shield blocks pulses; overdrive adds boost fuel and a grounded speed kick. Pulse pushes nearby rivals.\nIn combat, shield blocks damage, overdrive doubles damage and pulse damages nearby rivals. Items are lost on knockout.",21,180);
            Label(panel,"RACE CRAFT\nChampionship / Elimination: follow a moving rival 5–30 metres ahead to build slipstream, then pull out to pass.\nElimination removes last place every 45 seconds. Keep passing ordered checkpoints; the last survivor wins.",23,145);
            if (!paused) Button("START PRACTICE", StartDrivingSchool);
            Focus(Button("BACK", () => { if (paused) PauseMenu(); else MainMenu(); }));
        }

        private static void ApplyQuality(int quality)
        {
            int q = Mathf.Clamp(quality, 0, 2);
            if (QualitySettings.names.Length > 0) QualitySettings.SetQualityLevel(Mathf.Min(q, QualitySettings.names.Length - 1), true);
            QualitySettings.shadowDistance = q == 0 ? 30 : q == 1 ? 80 : 150;
            QualitySettings.lodBias = q == 0 ? 0.6f : q == 1 ? 1 : 1.5f;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline)
            {
                pipeline.renderScale = q == 0 ? .75f : 1f;
                pipeline.msaaSampleCount = q == 0 ? 1 : q == 1 ? 2 : 4;
                pipeline.shadowDistance = q == 0 ? 30 : q == 1 ? 80 : 150;
            }
            Application.targetFrameRate = 60;
        }

        private void PauseMenu()
        {
            if (!session.Paused) session.SetPaused(true);
            Page("Paused", session.PauseReason);
            Focus(Button("RESUME", Resume));
            Button("RESTART EVENT", () => ConfirmAbandon(true));
            Button("SETTINGS", () => Settings(true));
            Button("HOW TO PLAY", () => HowToPlay(true));
            Button("MAIN MENU", () => ConfirmAbandon(false));
            pageBack = Resume;
        }
        private void ConfirmAbandon(bool restart)
        {
            Page(restart ? "Restart this event?" : "Leave this event?",
                "This unfinished event will not earn a medal. Restarting a championship starts again from round one.\nYour previously saved medals, records and unlocked cars are kept.");
            Focus(Button("KEEP PLAYING", Resume));
            pageBack = Resume;
            Button(restart ? "RESTART NOW" : "LEAVE EVENT", restart ? (Action)RestartCurrent : MainMenu);
        }
        private void Resume() { ClearPanel(); session.ResumeSafely(); if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); }
        private void OnMatchEnded()
        {
            if (showingResults) return;
            showingResults = true;
            sound.Finish();
            Page(session.IsTraining ? "Training complete" : session.GameOver ? "GAME OVER" : session.FirstTourCompletion ? "Tour complete" : "Event complete", session.ResultText);
            BuildResultShowcase();
            string reward = session.IsTraining ? "DRIVING SCHOOL COMPLETED" : session.CanAdvanceChampionship ? "Points recorded — XP awarded after the final round" : "+" + session.ResultXp + " XP";
            Label(panel, $"{reward}\nLEVEL {SaveStore.Current.Level} • {SaveStore.Current.xp} XP • {SaveStore.Current.UnlockedVehicles}/6 CARS", 28, 95);
            if(!session.IsTraining && !session.CanAdvanceChampionship)
                Label(panel,$"+{session.ResultCredits:N0} CREDITS\nEvent: {session.ResultCredits-session.MedalCredits:N0} • New medal bonus: {session.MedalCredits:N0}\nWallet: {SaveStore.Current.garage.credits:N0} credits",24,110);
            if (!string.IsNullOrEmpty(session.UnlockedCarNames)) Label(panel, "NEW CAR UNLOCKED: " + session.UnlockedCarNames, 26, 55);
            if (!string.IsNullOrEmpty(SaveStore.LastError)) Label(panel, SaveStore.LastError, 18, 70);
            if (session.IsTraining)
            {
                Focus(Button("START CAREER", () => { careerSelection=SaveStore.Current.career.NextEvent;CareerMenu(); }));
                Button("REPEAT TRAINING",StartDrivingSchool);
            }
            else if (session.CanAdvanceChampionship)
            {
                Focus(Button("NEXT CHAMPIONSHIP ROUND", () => {
                    ClearPanel(); showingResults = false; session.AdvanceChampionship();
                    var p = SaveStore.Current;
                    if (session.Player) session.Player.SetCosmetics(p.wheels, p.trail, p.decal, p.spoiler, p.finish);
                    if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                }));
                Button("RESTART CHAMPIONSHIP", RestartCurrent);
            }
            else if (session.GameOver)
            {
                Label(panel, "Try again — your previously earned medals and unlocked cars are safe.", 24, 80);
                Focus(Button("TRY AGAIN", RestartCurrent));
                if (session.CareerEventIndex >= 0) Button("CAREER TOUR", CareerMenu);
            }
            else if (session.CareerEventIndex >= 0)
            {
                if (session.FirstTourCompletion) Focus(Button("TOUR FINALE", TourFinale));
                else Focus(Button("CONTINUE TOUR", () => { careerSelection = SaveStore.Current.career.NextEvent; CareerMenu(); }));
                Button("REPLAY EVENT", RestartCurrent);
            }
            else Focus(Button("RACE AGAIN", RestartCurrent));
            if(!session.CanAdvanceChampionship)Button("GARAGE & UPGRADES",()=>{session.ReturnToMenu();Garage();});
            Button("MAIN MENU", MainMenu);
        }

        private void Records()
        {
            var records = SaveStore.Current.records;
            int pages = Mathf.Max(1, (records.Count + 3) / 4);
            recordsPage = Mathf.Clamp(recordsPage, 0, pages - 1);
            Page("Personal records", $"Saved on this computer • Page {recordsPage + 1}/{pages}\nTimes are separated by event, environment and vehicle.");
            string text = "";
            for (int i = recordsPage * 4; i < Mathf.Min(records.Count, recordsPage * 4 + 4); i++)
            {
                var r = records[i];
                string environment = r.environment >= 0 && r.environment < 5 ? GameSession.EnvironmentNames[r.environment] : "Earlier version";
                string vehicle = r.vehicle >= 0 && r.vehicle < 6 ? GameSession.VehicleNames[r.vehicle] : "All vehicles";
                vehicle += r.upgradeSignature==0 ? " • STOCK" : $" • E{r.upgradeSignature%4}/H{r.upgradeSignature/4%4}/B{r.upgradeSignature/16%4}";
                if (r.environment >= 0 && r.layoutRevision != CircuitLayout.Revision) environment += " (old layout)";
                if ((r.mode=="Championship" || r.mode=="Elimination" || r.mode=="BoostBattle") && r.raceRulesRevision!=RaceCraft.RulesVersion) environment += " (earlier race rules)";
                if(r.handlingRevision!=HandlingModel.Revision)vehicle+=" (earlier handling)";
                text += $"{r.mode} • {environment} • {vehicle}\n{r.finishes} finishes • Best {LapDisplay(r.bestSeconds)} • Score {r.bestScore}\n\n";
            }
            Label(panel, string.IsNullOrEmpty(text) ? "Finish your first event to set a record." : text, 22, 340);
            Label(panel, "ACHIEVEMENTS\n" + (SaveStore.Current.achievements.Count > 0 ? string.Join(" • ", SaveStore.Current.achievements) : "First finish • First victory • Ten victories • Full garage • Stunt specialist"), 22, 100);
            Focus(Button("BACK", MainMenu));
            if (recordsPage > 0) Button("PREVIOUS PAGE", () => { recordsPage--; Records(); });
            if (recordsPage + 1 < pages) Button("NEXT PAGE", () => { recordsPage++; Records(); });
        }
        private void Credits()
        {
            Page("Credits", $"VELOCITY RIVALS {Application.version} • Original game project for DJKFusion");
            Label(panel, "Original vehicles, environments and electronic soundtrack.\nMidnight Grid • Skyline Dreams • Solar Drift\nNeon Pursuit • Afterburn • Hyperdrive\nChrome Horizon • Ion Storm • Final Overdrive\nElectric Afterglow • Nightshift Run • Apex Signal\n\nBuilt with Unity and the Universal Render Pipeline.\nVehicle models created with Blender.\n\nGame project for DJKFusion.", 25, 350);
            Focus(Button("BACK", MainMenu));
        }
    }
}
