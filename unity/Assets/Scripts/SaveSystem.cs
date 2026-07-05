using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// JSON save/load via Unity's built-in JsonUtility + PlayerPrefs — no
/// extra package needed. Written as explicit DTOs rather than serializing
/// GameState directly because JsonUtility can't handle Dictionary,
/// HashSet, or nullable value types (RoomUnit.issue is IssueType?).
/// </summary>
public static class SaveSystem
{
    const string PrefsKey = "OrmanOtelSave_v1";
    const string TowerPrefsKey = "OrmanOtelTowers_v1";

    [Serializable]
    public class SaveData
    {
        public int day;
        public double money, wood, meat, reputation;
        public int badMoneyStreak;
        public double totalEarned, totalMeatCollected, totalWoodChopped;
        public float dayProgress;
        public int speed;
        public long savedAtTicks;

        public List<RoomSave> units = new List<RoomSave>();
        public List<StaffSave> staff = new List<StaffSave>();
        public List<UpgradeSave> upgrades = new List<UpgradeSave>();
        public List<string> achievements = new List<string>();
    }

    [Serializable]
    public class RoomSave
    {
        public int id;
        public double rent, condition;
        public int issue = -1; // -1 = none, else (int)IssueType

        public bool hasTenant;
        public string tenantName;
        public int tenantType;
        public double tenantHappiness;
        public int tenantUnpaidStreak;
        public bool tenantIsTrouble;

        public bool hasApplicant;
        public string applicantName;
        public int applicantType;
        public double applicantMaxRent;
        public string docOccupation;
        public string docItem;
        public bool docSuspicious;
        public bool docTrouble;
    }

    [Serializable]
    public class StaffSave { public int key; public bool hired; public int level; public double xp; }

    [Serializable]
    public class UpgradeSave { public int key; public int level; }

    public static bool HasSave() => PlayerPrefs.HasKey(PrefsKey);

    public static void Save(GameState state)
    {
        var data = new SaveData
        {
            day = state.day,
            money = state.money,
            wood = state.wood,
            meat = state.meat,
            reputation = state.reputation,
            badMoneyStreak = state.badMoneyStreak,
            totalEarned = state.totalEarned,
            totalMeatCollected = state.totalMeatCollected,
            totalWoodChopped = state.totalWoodChopped,
            dayProgress = state.dayProgress,
            speed = state.speed,
            savedAtTicks = DateTime.UtcNow.Ticks,
        };

        foreach (var u in state.units)
        {
            var rs = new RoomSave
            {
                id = u.id,
                rent = u.rent,
                condition = u.condition,
                issue = u.issue.HasValue ? (int)u.issue.Value : -1,
            };
            if (u.tenant != null)
            {
                rs.hasTenant = true;
                rs.tenantName = u.tenant.name;
                rs.tenantType = (int)u.tenant.type;
                rs.tenantHappiness = u.tenant.happiness;
                rs.tenantUnpaidStreak = u.tenant.unpaidStreak;
                rs.tenantIsTrouble = u.tenant.isTrouble;
            }
            if (u.applicant != null)
            {
                rs.hasApplicant = true;
                rs.applicantName = u.applicant.name;
                rs.applicantType = (int)u.applicant.type;
                rs.applicantMaxRent = u.applicant.maxRent;
                if (u.applicant.doc != null)
                {
                    rs.docOccupation = u.applicant.doc.Occupation;
                    rs.docItem = u.applicant.doc.CarriedItem;
                    rs.docSuspicious = u.applicant.doc.IsSuspicious;
                    rs.docTrouble = u.applicant.doc.IsTrouble;
                }
            }
            data.units.Add(rs);
        }

        foreach (var kv in state.staff)
            data.staff.Add(new StaffSave { key = (int)kv.Key, hired = kv.Value.Hired, level = kv.Value.Level, xp = kv.Value.Xp });

        foreach (var kv in state.upgrades)
            data.upgrades.Add(new UpgradeSave { key = (int)kv.Key, level = kv.Value });

        data.achievements = state.achievements.ToList();

        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    /// <returns>Loaded state, or null if there's no save.</returns>
    public static GameState Load()
    {
        if (!HasSave()) return null;
        var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(PrefsKey));

        var state = new GameState
        {
            day = data.day,
            money = data.money,
            wood = data.wood,
            meat = data.meat,
            reputation = data.reputation,
            badMoneyStreak = data.badMoneyStreak,
            totalEarned = data.totalEarned,
            totalMeatCollected = data.totalMeatCollected,
            totalWoodChopped = data.totalWoodChopped,
            dayProgress = data.dayProgress,
            speed = data.speed,
        };

        foreach (var rs in data.units)
        {
            var u = new RoomUnit
            {
                id = rs.id,
                rent = rs.rent,
                condition = rs.condition,
                issue = rs.issue >= 0 ? (IssueType?)rs.issue : null,
            };
            if (rs.hasTenant)
            {
                u.tenant = new Guest
                {
                    name = rs.tenantName,
                    type = (GuestTypeKey)rs.tenantType,
                    happiness = rs.tenantHappiness,
                    unpaidStreak = rs.tenantUnpaidStreak,
                    isTrouble = rs.tenantIsTrouble,
                };
            }
            if (rs.hasApplicant)
            {
                u.applicant = new Booking
                {
                    name = rs.applicantName,
                    type = (GuestTypeKey)rs.applicantType,
                    maxRent = rs.applicantMaxRent,
                    doc = new GuestDocument
                    {
                        Occupation = rs.docOccupation,
                        CarriedItem = rs.docItem,
                        IsSuspicious = rs.docSuspicious,
                        IsTrouble = rs.docTrouble,
                    },
                };
            }
            state.units.Add(u);
        }

        foreach (var ss in data.staff)
            state.staff[(StaffKey)ss.key] = new StaffMember { Hired = ss.hired, Level = ss.level, Xp = ss.xp };

        foreach (var us in data.upgrades)
            state.upgrades[(UpgradeKey)us.key] = us.level;

        foreach (var a in data.achievements)
            state.achievements.Add(a);

        return state;
    }

    /// <summary>Real-world seconds since this save was written — use for an offline-progress feature if you want one.</summary>
    public static double SecondsSinceSave()
    {
        if (!HasSave()) return 0;
        var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(PrefsKey));
        return TimeSpan.FromTicks(DateTime.UtcNow.Ticks - data.savedAtTicks).TotalSeconds;
    }

    public static void DeleteSave() => PlayerPrefs.DeleteKey(PrefsKey);

    // ---- Archer towers (separate from the economy save above: these are
    // live scene objects — position + siteId + stored meat — not part of
    // GameState) ----

    [Serializable]
    public class TowerSave
    {
        public string siteId;
        public float x, y, z;
        public int storedMeat;
    }

    [Serializable]
    class TowerSaveList { public List<TowerSave> towers = new List<TowerSave>(); }

    public static void SaveTowers(List<TowerSave> towers)
    {
        var wrapper = new TowerSaveList { towers = towers };
        PlayerPrefs.SetString(TowerPrefsKey, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    /// <returns>Saved tower list, or null if there's nothing saved yet.</returns>
    public static List<TowerSave> LoadTowers()
    {
        if (!PlayerPrefs.HasKey(TowerPrefsKey)) return null;
        var wrapper = JsonUtility.FromJson<TowerSaveList>(PlayerPrefs.GetString(TowerPrefsKey));
        return wrapper.towers;
    }

    public static void DeleteTowerSave() => PlayerPrefs.DeleteKey(TowerPrefsKey);
}
