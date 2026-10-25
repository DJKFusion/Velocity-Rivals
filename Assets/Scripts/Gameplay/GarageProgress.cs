using System;

namespace VelocityRivals
{
    [Serializable]
    public sealed class GarageProgress
    {
        public const int MaxCredits = 9999999;
        public int credits = 500;
        public int purchasedCars = 1;
        public int[] upgrades = new int[18];
        public void Normalize()
        {
            credits=Math.Max(0,Math.Min(MaxCredits,credits));
            purchasedCars=Math.Max(1,Math.Min(6,purchasedCars));
            var levels=new int[18];
            if(upgrades!=null)for(int i=0;i<Math.Min(18,upgrades.Length);i++)levels[i]=Math.Max(0,Math.Min(3,upgrades[i]));
            upgrades=levels;
        }
        public int Level(int car,int category) => car>=0 && car<6 && category>=0 && category<3 ? upgrades[car*3+category] : 0;
        public int UpgradePrice(int car,int category) => Level(car,category)>=3 ? 0 : 150*(Level(car,category)+1);
        public int Signature(int car) => Level(car,0)+Level(car,1)*4+Level(car,2)*16;
        public static int CarPrice(int ownedCars) => ownedCars>=1 && ownedCars<6 ? 200+ownedCars*300 : 0;
        public bool BuyNextCar(int ownedCars)
        {
            int owned=Math.Max(purchasedCars,ownedCars),price=CarPrice(owned);
            if(price<=0 || credits<price)return false;
            credits-=price;purchasedCars=owned+1;return true;
        }
        public bool BuyUpgrade(int car,int category,int ownedCars)
        {
            if(car<0 || car>=Math.Min(6,ownedCars) || category<0 || category>2)return false;
            int price=UpgradePrice(car,category);
            if(price<=0 || credits<price)return false;
            credits-=price;upgrades[car*3+category]++;return true;
        }
        public int Credit(int amount)
        {
            int gained=Math.Max(0,Math.Min(MaxCredits-credits,amount));credits+=gained;return gained;
        }
        public static int EventReward(bool completed,bool won,int score)
            => !completed ? 0 : 150+(won?150:0)+Math.Max(0,Math.Min(100,score/50));
        public static int MedalReward(int before,int after)
            => Math.Max(0,Math.Min(3,after)-Math.Max(0,before))*100;
        public GarageProgress Copy() => new GarageProgress { credits=credits,purchasedCars=purchasedCars,upgrades=(int[])upgrades.Clone() };
    }
}
