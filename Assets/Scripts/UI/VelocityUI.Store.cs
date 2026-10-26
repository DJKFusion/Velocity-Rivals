using System;
using UnityEngine;

namespace VelocityRivals
{
    public sealed partial class VelocityUI
    {
        void CosmeticStore(Action returnTo, string notice="")
        {
            CosmeticPacks.Reload();
            Page("Car style store", "OPTIONAL COSMETICS • SAME RACING PERFORMANCE\nYour included paints, cars and earned upgrades stay available.");
            Label(panel,CosmeticCatalog.PackName.ToUpperInvariant()+"\nFour paint colours for all six cars. No speed or handling advantage.",26,90);
            Label(panel,"Aurora jade • Sunset rose • Polar pearl • Electric violet",23,55);
            Label(panel,CosmeticPacks.Status,23,60);
            if(!string.IsNullOrEmpty(notice)) Label(panel,notice,22,65);
            if(CosmeticPacks.Installed)
            {
                for(int i=0;i<CosmeticCatalog.Names.Length;i++)
                {
                    int index=i;
                    Button((SaveStore.Current.cosmeticPaint==CosmeticCatalog.PaintId(i)?"SELECTED • ":"EQUIP • ")+CosmeticCatalog.Names[i].ToUpperInvariant(),()=>{
                        var p=SaveStore.Current;string before=p.cosmeticPaint;p.cosmeticPaint=CosmeticCatalog.PaintId(index);
                        if(!SaveStore.Save()) { p.cosmeticPaint=before; CosmeticStore(returnTo,"Could not save your selection."); }
                        else CosmeticStore(returnTo,"Paint equipped. View your car in the garage.");
                    });
                }
            }
            else
            {
                bool configured=CosmeticCatalog.ValidCheckout(CosmeticCatalog.CheckoutUrl);
                var buy=Button(configured?"VIEW PRICE & BUY ON ITCH.IO ↗":"SALES NOT AVAILABLE YET",()=>CheckoutInfo(returnTo));
                buy.interactable=configured;
                Label(panel,"Payment and the final price are shown on itch.io. After buying, download and extract the pack, then copy its JSON file into the pack folder below. Reload to install.",22,100);
            }
            Button("OPEN PACK FOLDER",()=>{
                try { System.IO.Directory.CreateDirectory(CosmeticPacks.InstallDirectory); Application.OpenURL(new Uri(CosmeticPacks.InstallDirectory+System.IO.Path.DirectorySeparatorChar).AbsoluteUri); }
                catch(Exception e) when(e is System.IO.IOException || e is UnauthorizedAccessException) { CosmeticStore(returnTo,"Cannot open the pack folder."); }
            });
            Button("RELOAD INSTALLED PACKS",()=>CosmeticStore(returnTo));
            Label(panel,"Already bought it? Download it again from your itch.io purchase page. Installing a pack does not charge you.",20,70);
            Focus(Button("BACK TO GARAGE",()=>Garage(returnTo)));pageBack=()=>Garage(returnTo);
        }
        void CheckoutInfo(Action returnTo)
        {
            Page("Continue to itch.io?","Your browser will open the pack’s itch.io page.\nReview its price and contents before paying. No payment takes place inside the game.");
            Focus(Button("OPEN ITCH.IO",()=>{
                if(CosmeticCatalog.ValidCheckout(CosmeticCatalog.CheckoutUrl)) Application.OpenURL(CosmeticCatalog.CheckoutUrl);
                CosmeticStore(returnTo,"After checkout, install your download and select RELOAD INSTALLED PACKS.");
            }));
            Button("CANCEL",()=>CosmeticStore(returnTo));pageBack=()=>CosmeticStore(returnTo);
        }
    }
}
