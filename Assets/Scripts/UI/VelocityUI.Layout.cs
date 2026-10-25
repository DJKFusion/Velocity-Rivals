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
        RectTransform boostFill;
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
            var root=new GameObject(heading,typeof(RectTransform),typeof(MenuBackdrop),typeof(CanvasGroup),typeof(RectMask2D));
            root.transform.SetParent(canvas.transform,false);pageRoot=root.GetComponent<RectTransform>();
            pageRoot.anchorMin=pageRoot.anchorMax=pageRoot.pivot=new Vector2(.5f,.5f);pageRoot.sizeDelta=new Vector2(1660,920);
            root.GetComponent<MenuBackdrop>().raycastTarget=true;
            pageFade=root.GetComponent<CanvasGroup>();pageOpened=Time.unscaledTime;
            pageFade.alpha=SaveStore.Current.reducedEffects?1:0;
            var topLine=Plate("Accent line",pageRoot,cyan);BoxRect(topLine,0,0,1660,3);
            FixedText(pageRoot,"V / R     VELOCITY RIVALS",19,48,18,800,28,cyan);
            var edition=FixedText(pageRoot,"SINGLE PLAYER  /  "+Application.version,18,1100,18,510,28,new Color(.55f,.68f,.77f));
            edition.alignment=TextAnchor.MiddleRight;
            title=FixedText(pageRoot,heading.ToUpperInvariant(),46,48,58,1540,66,Color.white);
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
            FixedText(pageRoot,"NAVIGATE  arrows / D-pad     SELECT  Enter / A     BACK  Esc / B     SCROLL  mouse wheel",16,48,885,1500,24,new Color(.5f,.65f,.73f));
        }
        void BuildShowcase(bool wide)
        {
            foreach(Transform child in showcase) { child.gameObject.SetActive(false);Destroy(child.gameObject); }
            float width=wide?1016:464;
            float height=pageRoot.rect.height+showcase.offsetMax.y-showcase.offsetMin.y;
            float previewHeight=Mathf.Clamp(height-355,140,wide?310:235);
            FixedText(showcase,"YOUR GARAGE",18,24,14,width-48,30,cyan);
            showcaseName=FixedText(showcase,"",38,24,48,width-48,56,Color.white);
            var preview=new GameObject("Vehicle showroom",typeof(RectTransform),typeof(RawImage),typeof(GaragePreview));preview.transform.SetParent(showcase,false);
            var rect=preview.GetComponent<RectTransform>();BoxRect(rect,12,110,width-24,previewHeight);
            preview.GetComponent<RawImage>().raycastTarget=false;
            float below=120+previewHeight;
            showcaseStats=FixedText(showcase,"",21,24,below,width-48,72,new Color(.7f,.82f,.9f));
            showcaseProgress=FixedText(showcase,"",22,24,below+83,width-48,140,Color.white);
            RefreshShowcase();
        }
        void MakeHomeLayout()
        {
            var logo = Resources.Load<Texture2D>("Branding/VelocityRivalsLogo");
            if (logo)
            {
                var mark = new GameObject("Velocity Rivals logo",typeof(RectTransform),typeof(RawImage));
                mark.transform.SetParent(pageRoot,false);
                BoxRect(mark.GetComponent<RectTransform>(),1280,40,150,150);
                mark.GetComponent<RawImage>().texture=logo;mark.GetComponent<RawImage>().raycastTarget=false;
            }
            menuScroll.GetComponent<RectTransform>().offsetMax=new Vector2(-1120,menuScroll.GetComponent<RectTransform>().offsetMax.y);
            showcase.offsetMin=new Vector2(596,48);BuildShowcase(true);
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
            showcaseStats.text=stats?$"UNBOOSTED SPEED  {stats.MaxSpeed*3.6f:0} km/h\nACCELERATION  {stats.Acceleration:0} m/s²    STEERING  {stats.Steering:0}°/s":"";
            showcaseProgress.text=$"LEVEL {p.Level}   /   {p.xp} XP\n{p.career.TotalMedals}/36 MEDAL POINTS   •   {p.UnlockedVehicles}/6 CARS\n"+
                (p.UnlockedVehicles<6?$"{500-p.xp%500} XP to your next car":"FULL GARAGE UNLOCKED")+
                "\n"+(p.career.Complete?"TOUR COMPLETE — chase every gold":"NEXT: "+CareerTour.Get(p.career.NextEvent).Name);
        }
        void BuildRaceCards()
        {
            var info=Plate("Race information card",canvas.transform,new Color(.015f,.025f,.04f,.82f));
            BoxRect(info,28,25,720,175);raceInfoCard=info.gameObject;
            hud.transform.SetParent(info,false);BoxRect(hud.rectTransform,20,12,680,150);hud.fontSize=24;hud.alignment=TextAnchor.UpperLeft;
            var speed=Plate("Speed and boost card",canvas.transform,new Color(.015f,.025f,.04f,.88f));
            speed.anchorMin=speed.anchorMax=new Vector2(1,0);speed.pivot=new Vector2(1,0);
            speed.anchoredPosition=new Vector2(-28,105);speed.sizeDelta=new Vector2(340,190);speedCard=speed.gameObject;
            speedText=FixedText(speed,"0",70,22,8,200,88,Color.white);
            FixedText(speed,"KM/H",19,230,53,90,30,new Color(.65f,.8f,.88f));
            boostText=FixedText(speed,"BOOST",20,22,105,290,35,cyan);
            var track=Plate("Boost track",speed,new Color(.1f,.17f,.22f));BoxRect(track,22,151,296,10);
            boostFill=Plate("Boost level",track,cyan);boostFill.anchorMin=Vector2.zero;boostFill.anchorMax=Vector2.one;boostFill.offsetMin=boostFill.offsetMax=Vector2.zero;
            var timing=Plate("Lap timing card",canvas.transform,new Color(.015f,.025f,.04f,.82f));BoxRect(timing,28,210,940,44);timingCard=timing.gameObject;
            timingText=FixedText(timing,"",23,16,0,910,44,Color.white);
            var help=Plate("Driving hints card",canvas.transform,new Color(.015f,.025f,.04f,.8f));
            help.anchorMin=Vector2.zero;help.anchorMax=new Vector2(1,0);help.pivot=new Vector2(.5f,0);help.anchoredPosition=new Vector2(0,15);help.sizeDelta=new Vector2(-56,75);
            hint.transform.SetParent(help,false);Stretch(hint.rectTransform,6);hintCard=help.gameObject;
            warningText=Label(canvas.transform,"",28,55);warningText.rectTransform.anchorMin=warningText.rectTransform.anchorMax=new Vector2(.5f,.8f);
            warningText.rectTransform.sizeDelta=new Vector2(1000,55);warningText.color=new Color(1,.65f,.2f);
            raceInfoCard.SetActive(false);speedCard.SetActive(false);timingCard.SetActive(false);hintCard.SetActive(false);
        }
        void RefreshPresentation()
        {
            if(pageFade)pageFade.alpha=SaveStore.Current.reducedEffects?1:Mathf.Clamp01((Time.unscaledTime-pageOpened)/.16f);
            RefreshShowcase();
            bool driving=session.Running&&!session.Paused&&!session.Finished&&session.Player;
            raceInfoCard.SetActive(driving);speedCard.SetActive(driving);
            timingCard.SetActive(driving&&session.IsCircuit);hintCard.SetActive(driving);
            warningText.text=!driving?"":session.WrongWay?"WRONG WAY  •  Follow the yellow checkpoint":session.RaceNotice;
            if(!driving)return;
            speedText.text=session.Player.SpeedKmh.ToString("0");boostText.text="BOOST  "+Mathf.RoundToInt(session.Player.BoostAmount*100)+"%";
            boostFill.anchorMax=new Vector2(Mathf.Clamp01(session.Player.BoostAmount),1);
            if(session.IsCircuit)timingText.text=$"LAP  {session.LapTimes.CurrentLap(session.Elapsed):0.00}s    LAST  {LapDisplay(session.LapTimes.LastLap)}    BEST  {LapDisplay(session.LapTimes.BestLap)}";
        }
    }
}
