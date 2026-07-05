using System;
using System.Collections.Generic;

public enum StaffKey { Avci, Kapici, Temizlikci, Guvenlik }

[Serializable]
public class StaffMember
{
    public bool Hired;
    public int Level = 1;
    public double Xp;
}

public class StaffDef
{
    public string Label;
    public string Icon;
    public string Desc;
    public double HireCost;
    public double Wage;
}

public static class StaffSystem
{
    public const int MaxLevel = 15;

    public static readonly Dictionary<StaffKey, StaffDef> Defs = new Dictionary<StaffKey, StaffDef>
    {
        { StaffKey.Avci,       new StaffDef { Label = "Avcı",       Icon = "🏹", Desc = "Her gün otomatik olarak et toplar. Seviye arttıkça verimi artar.", HireCost = 2800, Wage = 50 } },
        { StaffKey.Kapici,     new StaffDef { Label = "Teknisyen",  Icon = "🧰", Desc = "Oda arızalarını odun harcamadan anında onarır. Seviye arttıkça daha çok durum yeniler.", HireCost = 3000, Wage = 80 } },
        { StaffKey.Temizlikci, new StaffDef { Label = "Temizlikçi", Icon = "🧹", Desc = "Otel yıpranma hızını azaltır. Seviye arttıkça etkisi güçlenir.", HireCost = 2000, Wage = 60 } },
        { StaffKey.Guvenlik,   new StaffDef { Label = "Güvenlik",   Icon = "🛡️", Desc = "Hayvan saldırısı/kötü olay ve misafir kaybı ihtimalini azaltır. Seviye arttıkça etkisi güçlenir.", HireCost = 2500, Wage = 70 } },
    };

    public static double XpToNext(int level) => Math.Round(60 * Math.Pow(1.22, level - 1));

    public static double AvciYield(int level) => 3 + level;
    public static double KapiciRestore(int level) => 15 + level * 3;
    public static double TemizlikciFactor(int level) => Math.Max(0.15, 0.5 - 0.02 * level);
    public static double GuvenlikFactor(int level) => Math.Max(0.15, 0.5 - 0.02 * level);

    /// <summary>Adds XP and applies any level-ups. Returns true if the member leveled up.</summary>
    public static bool GainXp(StaffMember staff, double amount)
    {
        if (!staff.Hired || staff.Level >= MaxLevel) return false;
        staff.Xp += amount;
        bool leveledUp = false;
        while (staff.Level < MaxLevel && staff.Xp >= XpToNext(staff.Level))
        {
            staff.Xp -= XpToNext(staff.Level);
            staff.Level++;
            leveledUp = true;
        }
        return leveledUp;
    }
}
