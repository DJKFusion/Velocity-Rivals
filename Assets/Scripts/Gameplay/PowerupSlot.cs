namespace VelocityRivals
{
    public enum PowerupKind { None, Shield, Overdrive, Pulse, OilSlick, ShockMine }
    /// <summary>One held item. Failed collection/use never replaces or consumes it.</summary>
    public sealed class PowerupSlot
    {
        public PowerupKind Held { get; private set; }
        public bool Collect(PowerupKind kind)
        {
            if(Held!=PowerupKind.None || kind<PowerupKind.Shield || kind>PowerupKind.ShockMine)return false;
            Held=kind;return true;
        }
        public bool Use(bool allowed,out PowerupKind kind)
        {
            kind=PowerupKind.None;
            if(!allowed || Held==PowerupKind.None)return false;
            kind=Held;Held=PowerupKind.None;return true;
        }
        public void Clear(){Held=PowerupKind.None;}
    }
}
