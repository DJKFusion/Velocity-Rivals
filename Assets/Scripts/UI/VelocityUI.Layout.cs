using UnityEngine;
using UnityEngine.UI;

namespace VelocityRivals
{
    public sealed partial class VelocityUI
    {
        RectTransform pageRoot, menuViewport, showcase;
        ScrollRect menuScroll;
        CanvasGroup pageFade;
        float pageOpened;
        Text showcaseName, showcaseStats, showcaseProgress, speedText, boostText, timingText, warningText;
        Text positionText, positionCaption, eventCaption, radioText, footerRadio, itemText;
        GameObject itemCard;
        GameObject radioCard;
        string lastRadioTrack;
        float radioChangedAt;
        readonly Image[] boostSegments = new Image[12];
        GameObject raceInfoCard, speedCard, timingCard, hintCard;
        VehicleStats displayedStats;
        int displayedVehicle = -1;
        System.Action pageBack;

        RectTransform Plate(string name,Transform parent,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;
            return go.GetComponent<RectTransform>();
        }
        static void BoxRect(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        }
        Text FixedText(Transform parent,string text,int size,float x,float y,float width,float height,Color color)
        {
            var label=Label(parent,text,size,height);BoxRect(label.rectTransform,x,y,width,height);
            label.alignment=TextAnchor.MiddleLeft;label.color=color;return label;
        }
        void BuildPageFrame(string heading,string subtitle)
        {
            ClearPanel();
            var root=new GameObject(heading,typeof(RectTransform),typeof(CanvasRenderer),typeof(MenuBackdrop),typeof(CanvasGroup),typeof(RectMask2D));
            root.transform.SetParent(canvas.transform,false);pageRoot=root.GetComponent<RectTransform>();
            pageRoot.anchorMin=pageRoot.anchorMax=pageRoot.pivot=new Vector2(.5f,.5f);pageRoot.sizeDelta=new Vector2(1660,920);
            root.GetComponent<MenuBackdrop>().raycastTarget=true;
            pageFade=root.GetComponent<CanvasGroup>();pageOpened=Time.unscaledTime;
            pageFade.alpha=SaveStore.Current.reducedEffects?1:0;
            var topLine=Plate("Accent line",pageRoot,cyan);BoxRect(topLine,0,0,1660,3);
            FixedText(pageRoot,"V / R     MOTORSPORT DIVISION",17,48,18,800,28,cyan);
            var edition=FixedText(pageRoot,SaveStore.Current.garage.credits.ToString("N0")+" CR    /    DRIVER LEVEL "+SaveStore.Current.Level,18,1100,18,510,28,new Color(.55f,.68f,.77f));
            edition.alignment=TextAnchor.MiddleRight;
            title=FixedText(pageRoot,heading.ToUpperInvariant(),46,48,58,1540,66,Color.white);title.fontStyle=FontStyle.Bold;
            int lines=0;foreach(string line in subtitle.Split('\n')) lines+=Mathf.Max(1,Mathf.CeilToInt(line.Length/105f));
            float subHeight=Mathf.Clamp(lines*29,55,190);
            FixedText(pageRoot,subtitle,23,50,128,1540,subHeight,new Color(.7f,.8f,.87f));
            float bodyTop=142+subHeight;

            var scrollObject=new GameObject("Menu scroll",typeof(RectTransform),typeof(ScrollRect));scrollObject.transform.SetParent(pageRoot,false);
            var scrollRect=scrollObject.GetComponent<RectTransform>();Stretch(scrollRect,0);
            scrollRect.offsetMin=new Vector2(48,48);scrollRect.offsetMax=new Vector2(-560,-bodyTop);
            menuScroll=scrollObject.GetComponent<ScrollRect>();menuScroll.horizontal=false;menuScroll.vertical=true;
            menuScroll.movementType=ScrollRect.MovementType.Clamped;menuScroll.scrollSensitivity=36;menuScroll.inertia=false;
            var view=new GameObject("Menu viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D));view.transform.SetParent(scrollObject.transform,false);
            menuViewport=view.GetComponent<RectTransform>();Stretch(menuViewport,0);view.GetComponent<Image>().color=new Color(0,0,0,.01f);
            var content=new GameObject("Menu content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));content.transform.SetParent(view.transform,false);
            panel=content.GetComponent<RectTransform>();panel.anchorMin=new Vector2(0,1);panel.anchorMax=Vector2.one;panel.pivot=new Vector2(.5f,1);
            panel.sizeDelta=Vector2.zero;panel.anchoredPosition=Vector2.zero;
            var layout=content.GetComponent<VerticalLayoutGroup>();layout.spacing=12;layout.padding=new RectOffset(4,12,4,4);
            layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            menuScroll.viewport=menuViewport;menuScroll.content=panel;
            var bar=Plate("Scroll track",scrollObject.transform,new Color(.08f,.13f,.17f));
            bar.GetComponent<Image>().raycastTarget=true;
            bar.anchorMin=new Vector2(1,0);bar.anchorMax=Vector2.one;bar.offsetMin=new Vector2(-6,0);bar.offsetMax=Vector2.zero;
            var handle=Plate("Scroll handle",bar,new Color(.18f,.65f,.73f));Stretch(handle,0);handle.GetComponent<Image>().raycastTarget=true;
            var scrollbar=bar.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle;scrollbar.targetGraphic=handle.GetComponent<Image>();
            scrollbar.direction=Scrollbar.Direction.BottomToTop;
            scrollbar.navigation=new Navigation { mode=Navigation.Mode.None };
            menuScroll.verticalScrollbar=scrollbar;menuScroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;

            showcase=Plate("Car showcase",pageRoot,new Color(.015f,.028f,.05f,.9f));
            showcase.anchorMin=Vector2.zero;showcase.anchorMax=Vector2.one;
            showcase.offsetMin=new Vector2(1148,48);showcase.offsetMax=new Vector2(-48,-bodyTop);
            BuildShowcase(false);
            FixedText(pageRoot,"NAVIGATE  arrows / D-pad     SELECT  Enter / A     BACK  Esc / B",15,48,885,1000,24,new Color(.5f,.65f,.73f));
            footerRadio=FixedText(pageRoot,"",14,1050,881,560,30,new Color(.5f,.75f,.8f));
            footerRadio.alignment=TextAnchor.MiddleRight;
        }
        void BuildShowcase(bool wide)
        {
            foreach(Transform child in showcase) { child.gameObject.SetActive(false);Destroy(child.gameObject); }
            float width=pageRoot.rect.width+showcase.offsetMax.x-showcase.offsetMin.x;
            float height=pageRoot.rect.height+showcase.offsetMax.y-showcase.offsetMin.y;
            float previewHeight=wide?Mathf.Max(220,height-230):Mathf.Clamp(height-355,140,235);
            FixedText(showcase,"VELOCITY MOTORWORKS  /  YOUR GARAGE",wide?18:15,24,14,width-48,30,cyan);
            showcaseName=FixedText(showcase,"",38,24,48,width-48,56,Color.white);showcaseName.fontStyle=FontStyle.Bold;
            var preview=new GameObject("Vehicle showroom",typeof(RectTransform),typeof(RawImage),typeof(GaragePreview));preview.transform.SetParent(showcase,false);
            var rect=preview.GetComponent<RectTransform>();BoxRect(rect,12,110,width-24,previewHeight);
            preview.GetComponent<RawImage>().raycastTarget=false;
            float below=120+previewHeight;
            showcaseStats=FixedText(showcase,"",wide?19:21,24,below,wide?width*.49f-30:width-48,72,new Color(.7f,.82f,.9f));
            showcaseProgress=FixedText(showcase,"",wide?18:22,wide?width*.51f:24,wide?below:below+83,wide?width*.49f-24:width-48,wide?100:140,Color.white);
            RefreshShowcase();
        }
        void MakeHomeLayout()
        {
            menuScroll.GetComponent<RectTransform>().offsetMax=new Vector2(-940,menuScroll.GetComponent<RectTransform>().offsetMax.y);
            showcase.offsetMin=new Vector2(744,48);BuildShowcase(true);
            var logo=Resources.Load<Texture2D>("Branding/VelocityRivalsLogo");
            if(logo)
            {
                var mark=new GameObject("Velocity Rivals logo",typeof(RectTransform),typeof(RawImage));mark.transform.SetParent(showcase,false);
                BoxRect(mark.GetComponent<RectTransform>(),754,20,84,84);
                mark.GetComponent<RawImage>().texture=logo;mark.GetComponent<RawImage>().raycastTarget=false;
            }
            var tiles=new GameObject("Home event tiles",typeof(RectTransform),typeof(GridLayoutGroup));
            tiles.transform.SetParent(panel,false);
            panel=tiles.GetComponent<RectTransform>();
            var grid=tiles.GetComponent<GridLayoutGroup>();
            grid.cellSize=new Vector2(316,112);grid.spacing=new Vector2(12,12);
            grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;
            grid.padding=new RectOffset(4,4,4,4);
        }

        void RefreshShowcase()
        {
            if(!showcaseName)return;
            var p=SaveStore.Current;showcaseName.text=GameSession.VehicleNames[p.selectedVehicle].ToUpperInvariant();
            if(displayedVehicle!=p.selectedVehicle)
            {
                displayedVehicle=p.selectedVehicle;
                displayedStats=Resources.Load<VehicleStats>("VehicleStats/"+GameSession.VehicleNames[p.selectedVehicle]);
            }
            var stats=displayedStats;
            showcaseStats.text=stats?$"STOCK SPEED  {stats.MaxSpeed*3.6f:0} km/h • ACCEL  {stats.Acceleration:0} m/s²\nENGINE {p.garage.Level(p.selectedVehicle,0)}/3 • HANDLING {p.garage.Level(p.selectedVehicle,1)}/3 • BOOST {p.garage.Level(p.selectedVehicle,2)}/3":"";
            showcaseProgress.text=$"{p.garage.credits:N0} CR   •   LEVEL {p.Level}   /   {p.xp} XP\n{p.career.TotalMedals}/36 MEDAL POINTS   •   {p.UnlockedVehicles}/6 CARS\n"+
                (p.UnlockedVehicles<6?$"NEXT CAR: {p.UnlockedVehicles*500-p.xp} XP or visit dealership":"FULL GARAGE UNLOCKED")+
                "\n"+(p.career.Complete?"TOUR COMPLETE — chase every gold":"NEXT: "+CareerTour.Get(p.career.NextEvent).Name);
        }
        void BuildResultShowcase()
        {
            foreach(Transform child in showcase){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            showcaseName=showcaseStats=showcaseProgress=null;
            Color accent=session.GameOver?new Color(1,.55f,.3f):new Color(1,.8f,.32f);
            FixedText(showcase,"RACE CONTROL  /  EVENT REPORT",15,24,18,416,30,accent);
            string outcome=session.IsTraining?"READY TO RACE":session.GameOver?"TRY AGAIN":session.CanAdvanceChampionship?"ROUND COMPLETE":session.ResultWon?"VICTORY":"FINISH RECORDED";
            var headline=FixedText(showcase,outcome,36,24,65,416,100,Color.white);headline.fontStyle=FontStyle.Bold;
            var line=Plate("Result divider",showcase,accent);BoxRect(line,24,185,416,3);
            if(session.CurrentMode==GameMode.Championship && !session.GameOver)
            {
                int[] order={0,1,2,3};
                System.Array.Sort(order,(a,b)=>{int byPoints=session.ChampionshipPoints(b).CompareTo(session.ChampionshipPoints(a));return byPoints!=0?byPoints:a.CompareTo(b);});
                string[] names={"YOU","ION","PRISM","COMET"};
                for(int i=0;i<order.Length;i++)
                {
                    int driver=order[i];
                    var row=Plate("Championship standing "+i,showcase,driver==0?new Color(.04f,.2f,.26f):new Color(.04f,.07f,.1f));
                    BoxRect(row,24,210+i*57,416,49);
                    FixedText(row,(i+1).ToString("00")+"   "+names[driver],22,12,0,260,49,driver==0?cyan:Color.white);
                    var points=FixedText(row,session.ChampionshipPoints(driver)+" PTS",22,270,0,130,49,Color.white);points.alignment=TextAnchor.MiddleRight;
                }
                FixedText(showcase,session.CanAdvanceChampionship?"NEXT ROUND  /  "+(session.ChampionshipRound+1)+" OF 3":"FINAL CHAMPIONSHIP STANDINGS",17,24,452,416,40,accent);
                return;
            }
            string reward=session.IsTraining?"DRIVING SCHOOL COMPLETE":session.CanAdvanceChampionship?"POINTS RECORDED":$"+{session.ResultCredits:N0} CR    /    +{session.ResultXp} XP";
            FixedText(showcase,reward,25,24,210,416,85,accent);
            string detail=session.LastCareerMedal>0?"CAREER MEDAL  /  "+CareerTour.MedalName(session.LastCareerMedal).ToUpperInvariant():session.GameOver?"Reset. Refocus. Take the next start.":"Build your garage. Chase your next best.";
            FixedText(showcase,detail,21,24,315,416,80,new Color(.7f,.82f,.9f));
            FixedText(showcase,$"WALLET  {SaveStore.Current.garage.credits:N0} CR\nTOUR  {SaveStore.Current.career.TotalMedals} / 36 MEDAL POINTS",19,24,425,416,75,Color.white);
        }
        void BuildRaceCards()
        {
            var info=Plate("Race information card",canvas.transform,new Color(.018f,.03f,.055f,.9f));
            BoxRect(info,28,25,570,154);raceInfoCard=info.gameObject;
            var stripe=Plate("Race accent",info,cyan);BoxRect(stripe,0,0,4,154);
            positionCaption=FixedText(info,"POSITION",13,20,12,116,24,cyan);
            positionText=FixedText(info,"01",62,18,35,116,80,Color.white);positionText.fontStyle=FontStyle.Bold;
            var divider=Plate("Race divider",info,new Color(.2f,.35f,.43f));BoxRect(divider,144,20,1,110);
            eventCaption=FixedText(info,"",17,166,14,386,28,cyan);
            hud.transform.SetParent(info,false);BoxRect(hud.rectTransform,166,48,380,95);hud.fontSize=20;hud.alignment=TextAnchor.UpperLeft;hud.resizeTextForBestFit=true;hud.resizeTextMinSize=16;hud.resizeTextMaxSize=20;
            var speed=Plate("Speed and boost card",canvas.transform,new Color(.018f,.03f,.055f,.9f));
            speed.anchorMin=speed.anchorMax=new Vector2(1,0);speed.pivot=new Vector2(1,0);
            speed.anchoredPosition=new Vector2(-28,32);speed.sizeDelta=new Vector2(340,210);speedCard=speed.gameObject;
            var speedAccent=Plate("Speed accent",speed,cyan);BoxRect(speedAccent,0,0,340,3);
            speedText=FixedText(speed,"0",84,20,7,220,100,Color.white);speedText.fontStyle=FontStyle.Bold;
            FixedText(speed,"KM/H",16,250,64,70,30,new Color(.65f,.8f,.88f));
            boostText=FixedText(speed,"BOOST",17,22,112,296,48,cyan);
            for(int i=0;i<boostSegments.Length;i++)
            {
                var segment=Plate("Boost segment "+i,speed,cyan);BoxRect(segment,22+i*25,172,21,12);
                boostSegments[i]=segment.GetComponent<Image>();
            }
            var timing=Plate("Lap timing card",canvas.transform,new Color(.018f,.03f,.055f,.85f));BoxRect(timing,28,190,570,62);timingCard=timing.gameObject;
            timingText=FixedText(timing,"",18,18,5,536,52,Color.white);
            var help=Plate("Driving hints card",canvas.transform,new Color(.018f,.03f,.055f,.88f));
            help.anchorMin=help.anchorMax=new Vector2(.5f,0);help.pivot=new Vector2(.5f,0);help.anchoredPosition=new Vector2(-60,28);help.sizeDelta=new Vector2(1030,80);
            hint.transform.SetParent(help,false);Stretch(hint.rectTransform,10);hint.fontSize=18;hintCard=help.gameObject;
            warningText=Label(canvas.transform,"",30,60);warningText.rectTransform.anchorMin=warningText.rectTransform.anchorMax=new Vector2(.5f,.76f);
            warningText.rectTransform.sizeDelta=new Vector2(1000,70);warningText.color=new Color(1,.72f,.3f);warningText.fontStyle=FontStyle.Bold;
            var shadow=warningText.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.85f);shadow.effectDistance=new Vector2(2,-2);
            var radio=Plate("Now playing toast",canvas.transform,new Color(.018f,.03f,.055f,.92f));
            radio.anchorMin=radio.anchorMax=new Vector2(0,0);radio.pivot=Vector2.zero;radio.anchoredPosition=new Vector2(28,28);radio.sizeDelta=new Vector2(460,85);
            radioText=FixedText(radio,"",17,18,8,424,68,new Color(.65f,.83f,.9f));radioCard=radio.gameObject;
            var slot=Plate("Held powerup",canvas.transform,new Color(.018f,.03f,.055f,.92f));
            slot.anchorMin=slot.anchorMax=new Vector2(1,0);slot.pivot=new Vector2(1,0);slot.anchoredPosition=new Vector2(-28,256);slot.sizeDelta=new Vector2(340,105);
            itemText=FixedText(slot,"",19,20,10,300,85,cyan);itemCard=slot.gameObject;itemCard.SetActive(false);
            raceInfoCard.SetActive(false);speedCard.SetActive(false);timingCard.SetActive(false);hintCard.SetActive(false);radioCard.SetActive(false);
        }
        void RefreshPresentation()
        {
            if(pageFade)pageFade.alpha=SaveStore.Current.reducedEffects?1:Mathf.Clamp01((Time.unscaledTime-pageOpened)/.16f);
            RefreshShowcase();
            if(footerRadio && sound)footerRadio.text="ON AIR  /  "+sound.NowPlaying+"   [N]";
            bool driving=session.Running&&!session.Paused&&!session.Finished&&session.Player;
            raceInfoCard.SetActive(driving);speedCard.SetActive(driving);
            itemCard.SetActive(driving&&session.HasItems);
            if(itemCard.activeSelf)
            {
                var held=session.HeldItem;
                itemText.color=held==PowerupKind.None?new Color(.6f,.75f,.83f):RaceItems.Tint(held);
                itemText.text=held==PowerupKind.None?"ITEM SLOT EMPTY\nDrive through a coloured crate":held.ToString().ToUpperInvariant()+" READY\n[F] / [RB]  USE ITEM";
                if(!string.IsNullOrEmpty(session.ItemEffect))itemText.text+="\n"+session.ItemEffect;
            }
            bool helpVisible=driving&&(session.Elapsed<8||session.RecoveringFromBoundary||session.IsTraining);
            timingCard.SetActive(driving&&session.IsCircuit);hintCard.SetActive(helpVisible);
            if(helpVisible)radioChangedAt=Time.unscaledTime;
            if(sound && lastRadioTrack!=sound.NowPlaying){lastRadioTrack=sound.NowPlaying;radioChangedAt=Time.unscaledTime;}
            radioCard.SetActive(driving&&!helpVisible&&Time.unscaledTime-radioChangedAt<7);
            if(radioCard.activeSelf)radioText.text="ON AIR  /  "+sound.PlaylistName.ToUpperInvariant()+"    [N] SKIP\n"+lastRadioTrack;
            warningText.text=!driving?"":session.WrongWay?"WRONG WAY  •  Follow the yellow checkpoint":session.RaceNotice;
            if(!driving)return;
            eventCaption.text=ModeTitle(session.CurrentMode);
            bool race=session.CurrentMode==GameMode.Championship||session.CurrentMode==GameMode.Elimination;
            positionCaption.text=race?"POSITION":session.CurrentMode==GameMode.TimeAttack?"LAP":"SCORE";
            positionText.text=race?session.RacePosition.ToString("00"):session.CurrentMode==GameMode.TimeAttack?session.PlayerLap.ToString("00"):session.PlayerScore.ToString();
            positionText.resizeTextForBestFit=true;positionText.resizeTextMinSize=26;positionText.resizeTextMaxSize=62;
            positionText.color=session.CurrentMode==GameMode.Elimination&&session.RacePosition==session.ActiveRaceCars&&session.EliminationRemaining<=10?new Color(1,.4f,.25f):Color.white;
            speedText.text=session.Player.SpeedKmh.ToString("0");
            float boost=Mathf.Clamp01(session.Player.BoostAmount);
            boostText.text="BOOST  "+Mathf.RoundToInt(boost*100)+"%";
            if(session.Player.DraftStrength>.05f)boostText.text+="\nSLIPSTREAM  +"+Mathf.RoundToInt(session.Player.DraftStrength*10)+"% SPEED";
            for(int i=0;i<boostSegments.Length;i++)boostSegments[i].color=boost>i/(float)boostSegments.Length?cyan:new Color(.12f,.2f,.26f);
            if(session.IsCircuit)timingText.text=$"CURRENT LAP  {session.LapTimes.CurrentLap(session.Elapsed):0.00}s\nLAST  {LapDisplay(session.LapTimes.LastLap)}     BEST  {LapDisplay(session.LapTimes.BestLap)}";
        }
        string CompactRaceStatus()
        {
            if(session.CountdownRemaining>0)return "GET READY";
            if(session.IsTraining)return "DRIVING SCHOOL  /  STEP "+Mathf.Min(4,session.Lesson.Step+1)+" OF 4";
            switch(session.CurrentMode)
            {
                case GameMode.Championship: return $"ROUND {session.ChampionshipRound}/3  •  LAP {session.PlayerLap}/3\nGATE {session.NextCheckpoint}/{session.Circuit.Count}";
                case GameMode.TimeAttack: return $"LAP {session.PlayerLap}/3  •  PERSONAL GHOST\nGATE {session.NextCheckpoint}/{session.Circuit.Count}";
                case GameMode.Elimination: return $"{session.ActiveRaceCars} CARS REMAIN\nGATE {session.NextCheckpoint}/{session.Circuit.Count}";
                case GameMode.ArenaClash: return $"BLUE {session.PlayerScore}  —  {session.RivalScore} ORANGE\nFIRST TO FIVE";
                case GameMode.BoostBattle: return $"RIVALS {session.RivalScore}  •  FIRST TO 15\n{session.CombatStatus}";
                case GameMode.StuntRush: return "TARGET 1,000  •  LAND TO SCORE";
                default: return "EXPLORE  •  PRACTISE  •  DRIVE";
            }
        }
        static string ModeTitle(GameMode mode) => mode==GameMode.TimeAttack?"TIME ATTACK":mode==GameMode.ArenaClash?"ARENA CLASH":mode==GameMode.BoostBattle?"BOOST BATTLE":mode==GameMode.StuntRush?"STUNT RUSH":mode==GameMode.FreePlay?"FREE PLAY":mode.ToString().ToUpperInvariant();
    }
}
