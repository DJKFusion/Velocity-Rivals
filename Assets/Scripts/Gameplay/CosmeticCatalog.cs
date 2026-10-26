using System;

namespace VelocityRivals
{
    // Public product configuration only. Never put payment API secrets in a game client.
    public static class CosmeticCatalog
    {
        public const string PackId = "aurora-paints";
        public const string PackName = "Aurora Paint Collection";
        // Set to your published paid itch.io pack page before enabling checkout.
        public static readonly string CheckoutUrl = "";
        public static readonly string[] Names = { "Aurora jade", "Sunset rose", "Polar pearl", "Electric violet" };
        public static readonly string[] Colors = { "#31D8A0", "#ED6E98", "#BBD9F2", "#8459ED" };
        public static string PaintId(int index) => PackId + "/" + index;
        public static int Index(string id)
        {
            for (int i=0;i<Names.Length;i++) if(id==PaintId(i)) return i;
            return -1;
        }
        public static bool ValidCheckout(string url)
        {
            if (!Uri.TryCreate(url,UriKind.Absolute,out var uri)) return false;
            return uri.Scheme=="https" && uri.Host.EndsWith(".itch.io",StringComparison.OrdinalIgnoreCase)
                && uri.UserInfo.Length==0 && uri.IsDefaultPort && uri.AbsolutePath.Length>1;
        }
    }
}
