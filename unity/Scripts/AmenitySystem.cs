using System;
using System.Collections.Generic;

public class AmenityDef
{
    public string Label;
    public string Icon;
    public string Desc;
    public double BaseCost;
    public int Max;
}

public static class AmenitySystem
{
    public const double CostRatio = 1.75;

    public static readonly Dictionary<UpgradeKey, AmenityDef> Defs = new Dictionary<UpgradeKey, AmenityDef>
    {
        { UpgradeKey.Rent,       new AmenityDef { Label = "Sıcak Su Sistemi", Icon = "🚿", Desc = "Misafirlerin ödediği ücreti %5 artırır (seviye başı).", BaseCost = 2200, Max = 20 } },
        { UpgradeKey.Applicant,  new AmenityDef { Label = "Rezervasyon Ağı",  Icon = "📶", Desc = "Yeni misafir rezervasyon şansını artırır.",              BaseCost = 1800, Max = 20 } },
        { UpgradeKey.Insulation, new AmenityDef { Label = "Isıtma Sistemi",   Icon = "🔥", Desc = "Otel yıpranmasını ve arıza şansını azaltır.",            BaseCost = 1500, Max = 20 } },
        { UpgradeKey.Ad,         new AmenityDef { Label = "Misafir İlişkileri", Icon = "🤝", Desc = "Kötü olayların etkisini azaltır, itibar kazancını artırır.", BaseCost = 2000, Max = 20 } },
        { UpgradeKey.Wall,       new AmenityDef { Label = "Çit & Duvar", Icon = "🧱", Desc = "Hayvanların otele yaklaşma ihtimalini ve saldırı hasarını azaltır.", BaseCost = 2500, Max = 20 } },
    };

    public static double Cost(GameState state, UpgradeKey key) =>
        Math.Round(Defs[key].BaseCost * Math.Pow(CostRatio, state.upgrades[key]));

    /// <summary>Multiplier applied to animal approach chance and raid severity — lower is safer.</summary>
    public static double WallFactor(int level) => Math.Max(0.15, 1 - 0.04 * level);
}
