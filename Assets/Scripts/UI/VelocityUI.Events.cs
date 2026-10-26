using UnityEngine;

namespace VelocityRivals
{
    public sealed partial class VelocityUI
    {
        static string EventBrief(GameMode mode) => mode == GameMode.Championship ?
            "THREE ROUNDS • THREE LAPS EACH\nRace three rivals through ordered checkpoints. Places earn 10 / 6 / 4 / 2 points. Use slipstream to set up passes. Each round has a ten-minute limit." :
            mode == GameMode.Elimination ? "FOUR CARS • LAST SURVIVOR WINS\nEvery 45 seconds, last place is eliminated. Ordered checkpoints determine position. Draft behind rivals, use recharge lanes and escape last place before the cutoff." :
            mode == GameMode.TimeAttack ? "THREE LAPS • STOCK PERFORMANCE\nFollow the yellow checkpoint. Complete three laps within ten minutes to save a time and race your fastest ghost. Workshop upgrades are disabled here." :
            mode == GameMode.ArenaClash ? "FIRST TO FIVE • THREE MINUTES\nYou and your blue teammate face the orange team. Push the white energy core into the orange goal. Highest score wins at time-out." :
            mode == GameMode.BoostBattle ? "FIRST TO FIFTEEN • TWO MINUTES THIRTY\nBoost into rivals to damage their armour. Knockouts score points. Cyan pickups shield you, orange doubles damage, and purple releases a pulse." :
            mode == GameMode.StuntRush ? "TWO MINUTES • TARGET 1,000 POINTS\nUse the ramps, jump and roll in the air, then land upright to bank stunt points. A second jump adds an aerial dodge." :
            "NO TIMER • NO OPPONENTS\nExplore the enclosed practice arena. Experiment with boost, drift and air control. Pause to restart or return to the menu.";

        void ShowCourse(int environment, GameMode mode)
        {
            foreach (Transform child in showcase) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            showcaseName = showcaseStats = showcaseProgress = null;
            FixedText(showcase,"EVENT BRIEFING",18,24,16,416,30,cyan);
            FixedText(showcase,GameSession.EnvironmentNames[environment].ToUpperInvariant(),27,24,52,416,84,Color.white);
            var map = new GameObject("Course preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(CoursePreview));
            map.transform.SetParent(showcase,false);BoxRect(map.GetComponent<RectTransform>(),24,140,416,240);
            map.GetComponent<CoursePreview>().raycastTarget=false;
            map.GetComponent<CoursePreview>().Configure(environment,mode==GameMode.Championship||mode==GameMode.TimeAttack||mode==GameMode.Elimination);
            bool circuit=mode==GameMode.Championship||mode==GameMode.TimeAttack||mode==GameMode.Elimination;
            string dimensions="126 × 140 m ARENA";
            if(circuit)
            {
                var route=CircuitLayout.Build(environment);float metres=0;
                for(int i=0;i<route.Count;i++)metres+=CircuitLayout.Distance(route[i],route[(i+1)%route.Count]);
                dimensions=$"{metres/1000f:0.00} km / LAP  •  WHITE: START";
            }
            FixedText(showcase,dimensions,17,24,390,416,30,cyan);
            FixedText(showcase,"SELECTED CAR\n"+GameSession.VehicleNames[SaveStore.Current.selectedVehicle].ToUpperInvariant(),24,24,440,416,80,Color.white);
        }
        void StartDrivingSchool()
        {
            ClearPanel();showingResults=false;
            var p=SaveStore.Current;
            session.StartTraining(p.selectedVehicle,CosmeticPacks.SelectedColor(p));ApplyCarCosmetics();
        }
    }
}
