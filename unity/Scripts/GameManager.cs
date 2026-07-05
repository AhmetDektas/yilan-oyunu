using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Core idle-sim loop, ported from apartman-yoneticisi.html. Attach to a single
/// empty GameObject in the scene (e.g. "GameManager"). UI/scene code should
/// subscribe to OnDayProcessed / OnLog rather than poll every frame.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Zamanlama")]
    public float dayDurationSeconds = 20f;

    [Header("Ekonomi")]
    public int startingUnitCount = 8;
    public double startingMoney = 6000;
    public double baseRent = 3200;
    public double dailyExpenseBase = 250;

    public GameState State { get; private set; }

    public event Action OnDayProcessed;
    public event Action<string> OnLog;

    readonly System.Random rng = new System.Random();
    static readonly string[] TenantNames = {
        "Ahmet Yılmaz", "Ayşe Kaya", "Mehmet Demir", "Fatma Şahin", "Ali Çelik",
        "Zeynep Arslan", "Mustafa Doğan", "Emine Aydın", "Hüseyin Öztürk", "Hatice Yıldız",
    };

    void Awake()
    {
        Instance = this;
        State = CreateFreshState();
    }

    void Update()
    {
        float dayMs = dayDurationSeconds / Mathf.Max(1, State.speed);
        State.dayProgress += Time.deltaTime / dayMs;
        while (State.dayProgress >= 1f)
        {
            State.dayProgress -= 1f;
            ProcessDay();
        }
    }

    GameState CreateFreshState()
    {
        var s = new GameState { money = startingMoney, reputation = 50 };
        for (int i = 0; i < startingUnitCount; i++)
            s.units.Add(new ApartmentUnit { id = i + 1, rent = baseRent, condition = 90 + rng.Next(10) });
        foreach (StaffKey k in Enum.GetValues(typeof(StaffKey))) s.staff[k] = new StaffMember();
        foreach (UpgradeKey k in Enum.GetValues(typeof(UpgradeKey))) s.upgrades[k] = 0;
        return s;
    }

    public void ForceCompleteDay()
    {
        State.dayProgress = 1f;
        ProcessDay();
    }

    void Log(string msg)
    {
        State.log.Insert(0, msg);
        if (State.log.Count > 30) State.log.RemoveAt(State.log.Count - 1);
        OnLog?.Invoke(msg);
    }

    void ProcessDay()
    {
        double income = 0, expenses = dailyExpenseBase;
        foreach (var kv in State.staff)
        {
            if (!kv.Value.Hired) continue;
            expenses += StaffSystem.Defs[kv.Key].Wage;
            StaffSystem.GainXp(kv.Value, 8);
        }

        double decayMul = DecayMultiplier();
        double issueMul = IssueChanceMultiplier();
        double rentMul = RentMultiplier();

        foreach (var u in State.units)
        {
            u.condition = Math.Max(0, u.condition - rng.Next(1, 4) * decayMul);

            if (u.issue == null)
            {
                double baseChance = u.condition < 40 ? 0.22 : u.condition < 70 ? 0.10 : 0.04;
                if (rng.NextDouble() < baseChance * issueMul)
                    TriggerIssue(u);
            }

            if (u.tenant != null) ProcessTenant(u, rentMul, ref income);
            else if (u.applicant == null) MaybeSpawnApplicant(u);
        }

        State.money += income - expenses;
        State.totalEarned += income;
        Log($"Gün {State.day} kapandı: +{income:N0}₺ gelir, -{expenses:N0}₺ gider.");

        State.badMoneyStreak = State.money < 0 ? State.badMoneyStreak + 1 : 0;
        State.day++;
        OnDayProcessed?.Invoke();
    }

    void TriggerIssue(ApartmentUnit u)
    {
        var issue = (IssueType)rng.Next(Enum.GetValues(typeof(IssueType)).Length);
        var info = IssueTypeData.Defs[issue];
        var kapici = State.staff[StaffKey.Kapici];

        if (kapici.Hired)
        {
            u.condition = Math.Min(100, u.condition + StaffSystem.KapiciRestore(kapici.Level));
            StaffSystem.GainXp(kapici, 6);
            Log($"Daire {u.id}: {info.Icon} {info.Label} oluştu, kapıcı hemen onardı.");
            CharacterWalker.Instance?.WalkToUnit(u.id, "🔧");
        }
        else
        {
            u.issue = issue;
            Log($"Daire {u.id}: {info.Icon} {info.Label} oluştu!");
            if (u.tenant != null) u.tenant.happiness -= 10;
        }
    }

    void ProcessTenant(ApartmentUnit u, double rentMul, ref double income)
    {
        var type = TenantTypes.Defs[u.tenant.type];

        if (u.rent > baseRent * 1.15) u.tenant.happiness -= 3 * type.RentSensitivity;
        else if (u.rent < baseRent * 0.9) u.tenant.happiness += 2;
        if (u.issue != null) u.tenant.happiness -= 5 * type.IssueSensitivity;
        u.tenant.happiness = Math.Max(0, Math.Min(100, u.tenant.happiness));

        double payThreshold = 15 / type.Patience;
        if (u.tenant.happiness > payThreshold || rng.NextDouble() > 0.3)
        {
            income += u.rent * rentMul;
            u.tenant.unpaidStreak = 0;
        }
        else
        {
            u.tenant.unpaidStreak++;
            Log($"Daire {u.id}: {u.tenant.name} kirayı ödemedi.");
        }

        var guvenlik = State.staff[StaffKey.Guvenlik];
        double gFactor = guvenlik.Hired ? StaffSystem.GuvenlikFactor(guvenlik.Level) : 1;
        double moveOutChance = Math.Min(1, gFactor / type.Patience);
        if (u.tenant.happiness <= 0 && rng.NextDouble() < moveOutChance)
        {
            Log($"Daire {u.id}: {u.tenant.name} memnuniyetsizlikten taşındı.");
            State.reputation = Math.Max(0, State.reputation - 2);
            u.tenant = null;
        }
    }

    void MaybeSpawnApplicant(ApartmentUnit u)
    {
        double chance = Math.Min(0.85, 0.15 + State.reputation / 400 + ApplicantBonus());
        if (rng.NextDouble() >= chance) return;

        var type = TenantTypes.PickRandom();
        var def = TenantTypes.Defs[type];
        double maxRent = Math.Round(u.rent * (def.RentFactorMin + rng.NextDouble() * (def.RentFactorMax - def.RentFactorMin)));
        string name = TenantNames[rng.Next(TenantNames.Length)];
        u.applicant = new Applicant { name = name, type = type, maxRent = maxRent };
        Log($"Daire {u.id}: {def.Icon} {name} ({def.Label}) başvurdu.");
    }

    double RentMultiplier() => 1 + 0.05 * State.upgrades[UpgradeKey.Rent];
    double ApplicantBonus() => 0.03 * State.upgrades[UpgradeKey.Applicant];

    double DecayMultiplier()
    {
        double m = 1 - 0.07 * State.upgrades[UpgradeKey.Insulation];
        var temizlikci = State.staff[StaffKey.Temizlikci];
        if (temizlikci.Hired) m *= StaffSystem.TemizlikciFactor(temizlikci.Level);
        return Math.Max(0.15, m);
    }

    double IssueChanceMultiplier() => Math.Max(0.25, 1 - 0.06 * State.upgrades[UpgradeKey.Insulation]);

    // ---- Player actions ----

    public void AcceptApplicant(int unitId)
    {
        var u = State.units.First(x => x.id == unitId);
        if (u.applicant == null || u.rent > u.applicant.maxRent) return;
        var def = TenantTypes.Defs[u.applicant.type];
        u.tenant = new Tenant { name = u.applicant.name, type = u.applicant.type, happiness = def.BaseHappiness };
        Log($"Daire {unitId}: {def.Icon} {u.tenant.name} taşındı.");
        u.applicant = null;
        CharacterWalker.Instance?.WalkToUnit(unitId, "🔑");
    }

    public void RejectApplicant(int unitId)
    {
        var u = State.units.First(x => x.id == unitId);
        if (u.applicant == null) return;
        Log($"Daire {unitId}: {u.applicant.name} reddedildi.");
        u.applicant = null;
    }

    public void RepairUnit(int unitId)
    {
        var u = State.units.First(x => x.id == unitId);
        if (u.issue == null) return;
        double cost = IssueTypeData.Defs[u.issue.Value].Cost;
        if (State.money < cost) return;
        State.money -= cost;
        u.condition = Math.Min(100, u.condition + 25);
        if (u.tenant != null) u.tenant.happiness = Math.Min(100, u.tenant.happiness + 15);
        Log($"Daire {unitId}: {IssueTypeData.Defs[u.issue.Value].Label} giderildi.");
        u.issue = null;
        CharacterWalker.Instance?.WalkToUnit(unitId, "🔧");
    }

    public void HireStaff(StaffKey key)
    {
        var def = StaffSystem.Defs[key];
        var s = State.staff[key];
        if (s.Hired || State.money < def.HireCost) return;
        State.money -= def.HireCost;
        s.Hired = true;
        s.Level = 1;
        s.Xp = 0;
        Log($"{def.Label} işe alındı.");
    }
}
