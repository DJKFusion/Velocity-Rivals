using System;
using System.Collections.Generic;

namespace VelocityRivals
{
    public sealed partial class VelocityUI
    {
        static readonly string[] UpgradeNames={"ENGINE","HANDLING","BOOST"};
        static readonly string[] UpgradeEffects={"Each stage: +3% top speed and +5% acceleration.","Each stage: +8% grip and +3% steering response.","Each stage: +8% boost force."};
        void Workshop(Action returnTo,string notice="")
        {
            var p=SaveStore.Current;
            Page("Performance workshop",$"{p.garage.credits:N0} CREDITS • THREE STAGES PER UPGRADE\nTime Attack always uses stock performance. Other modes use your upgrades.");
            var cars=new List<string>();for(int i=0;i<p.UnlockedVehicles;i++)cars.Add(GameSession.VehicleNames[i]);
            Focus(Cycle("Vehicle",cars,()=>p.selectedVehicle,value=>{p.selectedVehicle=value;SaveStore.Save();Workshop(returnTo);}));
            if(!string.IsNullOrEmpty(notice))Label(panel,notice,23,75);
            for(int category=0;category<3;category++)
            {
                int selectedCategory=category,level=p.garage.Level(p.selectedVehicle,category),price=p.garage.UpgradePrice(p.selectedVehicle,category);
                Label(panel,UpgradeNames[category]+$"  {level}/3\n"+UpgradeEffects[category],23,80);
                var button=Button(level>=3?"FULLY UPGRADED":$"UPGRADE {UpgradeNames[category]} — {price:N0} CR",()=>ConfirmUpgrade(selectedCategory,returnTo));
                button.interactable=level<3 && p.garage.credits>=price;
                if(level<3 && p.garage.credits<price)Label(panel,$"Earn {price-p.garage.credits:N0} more credits to upgrade.",18,35);
            }
            Button("BACK",()=>Garage(returnTo));
        }
        void ConfirmUpgrade(int category,Action returnTo)
        {
            var p=SaveStore.Current;int car=p.selectedVehicle,level=p.garage.Level(car,category),price=p.garage.UpgradePrice(car,category);
            Page("Install "+UpgradeNames[category].ToLowerInvariant()+" upgrade?",$"{GameSession.VehicleNames[car]} • STAGE {level+1}/3 • {price:N0} CREDITS\n"+UpgradeEffects[category]);
            Label(panel,$"Balance after purchase: {p.garage.credits-price:N0} credits.\nThis upgrade is saved for this car and applies when you start an event.\nTime Attack remains stock.",24,120);
            Focus(Button("CONFIRM UPGRADE",()=>{
                bool bought=SaveStore.PurchaseUpgrade(car,category);
                Workshop(returnTo,bought?"UPGRADE INSTALLED":SaveStore.LastError??"Purchase unavailable. Your credits have not been spent.");
            }));
            Button("CANCEL",()=>Workshop(returnTo));pageBack=()=>Workshop(returnTo);
        }
        void Dealership(Action returnTo,string notice="")
        {
            var p=SaveStore.Current;int owned=p.UnlockedVehicles;
            Page("Vehicle dealership",$"{p.garage.credits:N0} CREDITS • {owned}/6 CARS OWNED\nBuy the next car early, or earn its XP unlock for free. Owned cars stay yours.");
            if(!string.IsNullOrEmpty(notice))Label(panel,notice,23,70);
            for(int i=0;i<6;i++)
                Label(panel,GameSession.VehicleNames[i].ToUpperInvariant()+" — "+(i<owned?"OWNED":i==owned?$"NEXT • {GarageProgress.CarPrice(owned):N0} CR or {owned*500:N0} XP":$"UNLOCK EARLIER CARS / {i*500:N0} XP"),24,55);
            if(owned<6)
            {
                int price=GarageProgress.CarPrice(owned);
                var buy=Button($"BUY {GameSession.VehicleNames[owned].ToUpperInvariant()} — {price:N0} CR",()=>ConfirmCar(returnTo));
                buy.interactable=p.garage.credits>=price;
                if(buy.interactable)Focus(buy);
                else { Label(panel,$"Earn {price-p.garage.credits:N0} more credits, or reach the XP unlock.",23,70); }
            }
            var back=Button("BACK",()=>Garage(returnTo));
            if(owned==6 || p.garage.credits<GarageProgress.CarPrice(owned))Focus(back);
        }
        void ConfirmCar(Action returnTo)
        {
            var p=SaveStore.Current;int next=p.UnlockedVehicles;
            if(next>=6){Dealership(returnTo);return;}
            int price=GarageProgress.CarPrice(next);
            Page("Buy "+GameSession.VehicleNames[next]+"?",$"{price:N0} CREDITS\nBalance after purchase: {p.garage.credits-price:N0} credits. This car becomes your selected vehicle.");
            Focus(Button("CONFIRM PURCHASE",()=>{
                bool bought=SaveStore.PurchaseCar();
                Dealership(returnTo,bought?"CAR PURCHASED — ready in your garage":SaveStore.LastError??"Purchase unavailable. Your credits have not been spent.");
            }));
            Button("CANCEL",()=>Dealership(returnTo));pageBack=()=>Dealership(returnTo);
        }
    }
}
