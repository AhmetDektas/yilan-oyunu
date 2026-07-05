using System;
using System.Collections.Generic;
using System.Linq;

public class AchievementDef
{
    public string Id;
    public string Label;
    public string Icon;
    public string Desc;
    public Func<GameState, bool> Check;
    public double RewardMoney;
    public double RewardReputation;
}

public static class Achievements
{
    public static readonly List<AchievementDef> All = new List<AchievementDef>
    {
        new AchievementDef { Id = "first_tenant",  Label = "İlk Misafir", Icon = "🏨", Desc = "İlk misafirini ağırla.",
            Check = s => s.units.Any(u => u.tenant != null), RewardMoney = 200 },
        new AchievementDef { Id = "first_hunt",    Label = "İlk Av",      Icon = "🏹", Desc = "İlk kez avlan.",
            Check = s => s.totalMeatCollected > 0, RewardMoney = 100 },
        new AchievementDef { Id = "master_hunter", Label = "Usta Avcı",  Icon = "🥩", Desc = "Toplamda 500 et topla.",
            Check = s => s.totalMeatCollected >= 500, RewardMoney = 1500 },
        new AchievementDef { Id = "day_10",  Label = "10 Gün",  Icon = "📅", Desc = "10 gün boyunca oteli yönet.",
            Check = s => s.day >= 10, RewardMoney = 500 },
        new AchievementDef { Id = "day_50",  Label = "50 Gün",  Icon = "📆", Desc = "50 gün boyunca oteli yönet.",
            Check = s => s.day >= 50, RewardMoney = 2000 },
        new AchievementDef { Id = "day_100", Label = "100 Gün", Icon = "🗓️", Desc = "100 gün boyunca oteli yönet.",
            Check = s => s.day >= 100, RewardMoney = 5000 },
        new AchievementDef { Id = "money_10k",  Label = "10.000₺ Kazanç",  Icon = "💵", Desc = "Toplamda 10.000₺ kazan.",
            Check = s => s.totalEarned >= 10000, RewardReputation = 5 },
        new AchievementDef { Id = "money_100k", Label = "100.000₺ Kazanç", Icon = "💰", Desc = "Toplamda 100.000₺ kazan.",
            Check = s => s.totalEarned >= 100000, RewardReputation = 10 },
        new AchievementDef { Id = "full_house", Label = "Tam Doluluk", Icon = "🏘️", Desc = "Tüm odaları doldur.",
            Check = s => s.units.Count > 0 && s.units.All(u => u.tenant != null), RewardMoney = 1000 },
        new AchievementDef { Id = "full_staff", Label = "Kadro Tam", Icon = "👷", Desc = "Tüm personeli işe al.",
            Check = s => s.staff.Values.All(st => st.Hired), RewardReputation = 10 },
        new AchievementDef { Id = "mixed_tenants", Label = "Karma Misafir", Icon = "🌈", Desc = "Aynı anda üç farklı misafir tipini otelde bulundur.",
            Check = s => s.units.Where(u => u.tenant != null).Select(u => u.tenant.type).Distinct().Count() >= 3, RewardReputation = 8 },
    };
}
