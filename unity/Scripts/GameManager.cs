using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Core idle-sim economic loop, ported from apartman-yoneticisi.html (Orman
/// Otel build). Attach to a single empty GameObject in the scene (e.g.
/// "GameManager"). The real-time gathering map (PlayerController, Animal,
/// ResourceTree, DropZone) feeds wood/meat into this via DepositWood /
/// DepositMeat; everything else (rooms, staff, amenities, day tick) runs on
/// its own timer independent of the live map.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Zamanlama")]
    public float dayDurationSeconds = 20f;

    [Header("Ekonomi")]
    public int startingUnitCount = 8;
    public double startingMoney = 6000;
    public double startingWood = 60;
    public double baseRent = 3200;
    public double dailyExpenseBase = 250;
    public double mealPrice = 90;
    [Tooltip("Her gün, içeri alınmış bir 'sorunlu' misafirin vurup kaçma ihtimali.")]
    public double troubleStrikeChancePerDay = 0.25;

    public GameState State { get; private set; }

    public event Action OnDayProcessed;
    public event Action<string> OnLog;

    readonly System.Random rng = new System.Random();
    static readonly string[] GuestNames = {
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
        var s = new GameState { money = startingMoney, wood = startingWood, reputation = 50 };
        for (int i = 0; i < startingUnitCount; i++)
            s.units.Add(new RoomUnit { id = i + 1, rent = baseRent, condition = 90 + rng.Next(10) });
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

        var avci = State.staff[StaffKey.Avci];
        if (avci.Hired)
        {
            double y = StaffSystem.AvciYield(avci.Level);
            State.meat += y;
            State.totalMeatCollected += y;
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

            if (u.tenant != null && u.tenant.isTrouble && rng.NextDouble() < troubleStrikeChancePerDay)
            {
                TroubleGuestStrikes(u);
            }
            else if (u.tenant != null)
            {
                ProcessGuest(u, rentMul, ref income);
            }
            else if (u.applicant == null)
            {
                MaybeSpawnBooking(u);
            }
        }

        // Yemekhane: convert stocked meat into meals sold, scaled by occupancy.
        int occupied = State.units.Count(u => u.tenant != null);
        int mealDemand = Math.Max(1, occupied);
        int mealsSold = (int)Math.Min(Math.Floor(State.meat), mealDemand);
        if (mealsSold > 0)
        {
            State.meat -= mealsSold;
            income += mealsSold * mealPrice;
        }
        if (mealsSold < mealDemand)
        {
            Log("Yemekhanede et sıkıntısı yaşandı, bazı misafirler aç kaldı.");
            State.reputation = Math.Max(0, State.reputation - 1);
        }

        State.money += income - expenses;
        State.totalEarned += income;
        Log($"Gün {State.day} kapandı: +{income:N0}₺ gelir, -{expenses:N0}₺ gider.");

        if (rng.NextDouble() < 0.15) TriggerRandomEvent();

        State.badMoneyStreak = State.money < 0 ? State.badMoneyStreak + 1 : 0;
        State.day++;
        CheckAchievements();
        OnDayProcessed?.Invoke();
    }

    void TriggerIssue(RoomUnit u)
    {
        var issue = (IssueType)rng.Next(Enum.GetValues(typeof(IssueType)).Length);
        var info = IssueTypeData.Defs[issue];
        var kapici = State.staff[StaffKey.Kapici];

        if (kapici.Hired)
        {
            u.condition = Math.Min(100, u.condition + StaffSystem.KapiciRestore(kapici.Level));
            StaffSystem.GainXp(kapici, 6);
            Log($"Oda {u.id}: {info.Icon} {info.Label} oluştu, teknisyen hemen onardı.");
        }
        else
        {
            u.issue = issue;
            Log($"Oda {u.id}: {info.Icon} {info.Label} oluştu!");
            if (u.tenant != null) u.tenant.happiness -= 10;
        }
    }

    /// <summary>
    /// A guest let in despite a suspicious ID check finally makes their
    /// move: steals cash from the till and disappears (room becomes
    /// vacant, i.e. guest count drops too), on a random later day rather
    /// than the moment you admitted them.
    /// </summary>
    void TroubleGuestStrikes(RoomUnit u)
    {
        int stolen = (int)Math.Min(State.money, rng.Next(200, 801));
        State.money -= stolen;
        State.reputation = Math.Max(0, State.reputation - 4);
        Log($"😨 Oda {u.id}: {u.tenant.name} aslında sorun çıkarmaya gelmiş! {stolen:N0}₺ çalıp gece yarısı kayıplara karıştı.");
        u.tenant = null;
    }

    void ProcessGuest(RoomUnit u, double rentMul, ref double income)
    {
        var type = GuestTypes.Defs[u.tenant.type];

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
            Log($"Oda {u.id}: {u.tenant.name} ücreti ödemedi.");
        }

        double gFactor = GuvenlikFactor();
        double moveOutChance = Math.Min(1, gFactor / type.Patience);
        if (u.tenant.happiness <= 0 && rng.NextDouble() < moveOutChance)
        {
            Log($"Oda {u.id}: {u.tenant.name} memnuniyetsizlikten ayrıldı.");
            State.reputation = Math.Max(0, State.reputation - 2);
            u.tenant = null;
        }
    }

    void MaybeSpawnBooking(RoomUnit u)
    {
        double chance = Math.Min(0.85, 0.15 + State.reputation / 400 + ApplicantBonus());
        if (rng.NextDouble() >= chance) return;

        var type = GuestTypes.PickRandom();
        var def = GuestTypes.Defs[type];
        double maxRent = Math.Round(u.rent * (def.RentFactorMin + rng.NextDouble() * (def.RentFactorMax - def.RentFactorMin)));
        string name = GuestNames[rng.Next(GuestNames.Length)];
        var doc = GuestDocumentGenerator.Generate(type);
        u.applicant = new Booking { name = name, type = type, maxRent = maxRent, doc = doc };
        Log($"Oda {u.id}: {def.Icon} {name} ({def.Label}) rezervasyon yaptı, kimliği kontrol bekliyor.");
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

    double BadEventMultiplier()
    {
        double m = 1 - 0.06 * State.upgrades[UpgradeKey.Ad];
        var guvenlik = State.staff[StaffKey.Guvenlik];
        if (guvenlik.Hired) m *= StaffSystem.GuvenlikFactor(guvenlik.Level);
        return Math.Max(0.2, m);
    }

    /// <summary>Security staff's mitigation factor (1 = no mitigation, lower = safer). Used by Animal for raid odds/severity.</summary>
    public double GuvenlikFactor()
    {
        var g = State.staff[StaffKey.Guvenlik];
        return g.Hired ? StaffSystem.GuvenlikFactor(g.Level) : 1.0;
    }

    void TriggerRandomEvent()
    {
        double avgCondition = State.units.Average(u => u.condition);
        double bMul = BadEventMultiplier();
        int roll = rng.Next(4);
        switch (roll)
        {
            case 0:
                if (avgCondition < 55)
                {
                    double fine = Math.Round(rng.Next(500, 1501) * bMul);
                    State.money -= fine;
                    Log($"🏛️ Sağlık denetimi: otel bakımsız bulundu, {fine:N0}₺ ceza kesildi.");
                }
                else
                {
                    State.reputation = Math.Min(100, State.reputation + 3);
                    Log("🏛️ Sağlık denetimi başarıyla geçildi, itibarın arttı.");
                }
                break;
            case 1:
                State.reputation = Math.Min(100, State.reputation + 5);
                Log("📱 Gezginler otelden övgüyle bahsetti! İtibar arttı.");
                break;
            case 2:
                double bonus = rng.Next(30, 81);
                State.wood += bonus;
                State.totalWoodChopped += bonus;
                Log($"🪓 Ormanda değerli bir kereste yığını buldun: +{bonus:N0} odun.");
                break;
            default:
                State.reputation = Math.Max(0, State.reputation - 4 * bMul);
                Log("📝 Misafirlerden toplu şikayet geldi. İtibar düştü.");
                break;
        }
    }

    /// <summary>Called by Animal when it reaches the hotel unopposed.</summary>
    public void AnimalRaid()
    {
        double g = GuvenlikFactor();
        var u = State.units[rng.Next(State.units.Count)];
        u.condition = Math.Max(0, u.condition - rng.Next(10, 21) * g);
        int stolen = (int)Math.Min(State.meat, Math.Round(rng.Next(5, 16) * g));
        State.meat -= stolen;
        Log($"🐗 Bir hayvan otele saldırdı! Oda {u.id} hasar aldı, {stolen} et çalındı.");
    }

    public void DepositWood(int amount)
    {
        State.wood += amount;
        Log($"🪵 Depoya {amount} odun bırakıldı.");
    }

    public void DepositMeat(int amount)
    {
        State.meat += amount;
        Log($"🍽️ Yemekhaneye {amount} et bırakıldı.");
    }

    public void CheckAchievements()
    {
        foreach (var a in Achievements.All)
        {
            if (State.achievements.Contains(a.Id)) continue;
            if (!a.Check(State)) continue;
            State.achievements.Add(a.Id);
            if (a.RewardMoney > 0) { State.money += a.RewardMoney; State.totalEarned += a.RewardMoney; }
            if (a.RewardReputation > 0) State.reputation = Math.Min(100, State.reputation + a.RewardReputation);
            Log($"🏆 Başarım kazanıldı: {a.Label}!");
        }
    }

    // ---- Player actions (room management UI) ----

    public void AcceptBooking(int unitId, bool isTrouble = false)
    {
        var u = State.units.First(x => x.id == unitId);
        if (u.applicant == null || u.rent > u.applicant.maxRent) return;
        var def = GuestTypes.Defs[u.applicant.type];
        u.tenant = new Guest { name = u.applicant.name, type = u.applicant.type, happiness = def.BaseHappiness, isTrouble = isTrouble };
        Log($"Oda {unitId}: {def.Icon} {u.tenant.name} otele yerleşti.");
        u.applicant = null;
        CheckAchievements();
    }

    public void RejectBooking(int unitId)
    {
        var u = State.units.First(x => x.id == unitId);
        if (u.applicant == null) return;
        Log($"Oda {unitId}: {u.applicant.name} rezervasyonu reddedildi.");
        u.applicant = null;
    }

    /// <summary>
    /// "Papers, Please"-style verdict on a pending booking. The player
    /// only sees the visible clues (GuestDocument.IsSuspicious, claimed
    /// occupation/item) — IsTrouble is the hidden ground truth. Letting a
    /// trouble guest in doesn't punish you immediately: they move in
    /// like anyone else and only strike (steal money + vacate the room,
    /// see TroubleGuestStrikes) on a random later day, via
    /// troubleStrikeChancePerDay in ProcessDay. Correctly turning one
    /// away is rewarded; wrongly turning away a legitimate guest costs a
    /// little reputation too, so blanket rejection isn't a free strategy.
    /// </summary>
    public void ResolveEntryDecision(int unitId, bool allowIn)
    {
        var u = State.units.First(x => x.id == unitId);
        if (u.applicant == null || u.applicant.doc == null) return;
        var booking = u.applicant;
        var doc = booking.doc;

        if (allowIn)
        {
            AcceptBooking(unitId, doc.IsTrouble);
        }
        else
        {
            if (doc.IsTrouble)
            {
                State.reputation = Math.Min(100, State.reputation + 3);
                Log($"✅ {booking.name} reddedildi — şüpheliymiş, doğru karar verdin.");
            }
            else
            {
                State.reputation = Math.Max(0, State.reputation - 2);
                Log($"😕 {booking.name} haksız yere reddedildi, dedikodu yayıldı.");
            }
            u.applicant = null;
        }
    }

    public void RepairUnit(int unitId)
    {
        var u = State.units.First(x => x.id == unitId);
        if (u.issue == null) return;
        double cost = IssueTypeData.Defs[u.issue.Value].WoodCost;
        if (State.wood < cost) return;
        State.wood -= cost;
        u.condition = Math.Min(100, u.condition + 25);
        if (u.tenant != null) u.tenant.happiness = Math.Min(100, u.tenant.happiness + 15);
        Log($"Oda {unitId}: {IssueTypeData.Defs[u.issue.Value].Label} giderildi.");
        u.issue = null;
    }

    public void EvictGuest(int unitId)
    {
        var u = State.units.First(x => x.id == unitId);
        if (u.tenant == null) return;
        Log($"Oda {unitId}: {u.tenant.name} otelden çıkarıldı.");
        State.reputation = Math.Max(0, State.reputation - 3);
        u.tenant = null;
    }

    public void AdjustRent(int unitId, double delta)
    {
        var u = State.units.First(x => x.id == unitId);
        u.rent = Math.Max(500, u.rent + delta);
        if (u.tenant != null) u.tenant.happiness += delta > 0 ? -8 : 4;
    }

    public double ExpandMoneyCost() => Math.Round(2000 * Math.Pow(State.units.Count / 2.0, 1.6));
    public double ExpandWoodCost() => Math.Round(100 * Math.Pow(State.units.Count / 2.0, 1.3));
    public const int MaxUnits = 20;

    public void ExpandHotel()
    {
        double moneyCost = ExpandMoneyCost(), woodCost = ExpandWoodCost();
        if (State.units.Count >= MaxUnits || State.money < moneyCost || State.wood < woodCost) return;
        State.money -= moneyCost;
        State.wood -= woodCost;
        int startId = State.units.Count + 1;
        for (int i = 0; i < 2 && State.units.Count < MaxUnits; i++)
            State.units.Add(new RoomUnit { id = startId + i, rent = baseRent, condition = 100 });
        Log($"Yeni oda inşa edildi: Oda {startId}-{startId + 1}.");
        CheckAchievements();
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
        CheckAchievements();
    }

    public void BuyAmenity(UpgradeKey key)
    {
        var def = AmenitySystem.Defs[key];
        if (State.upgrades[key] >= def.Max) return;
        double cost = AmenitySystem.Cost(State, key);
        if (State.money < cost) return;
        State.money -= cost;
        State.upgrades[key]++;
        Log($"{def.Label} seviye {State.upgrades[key]} oldu.");
    }
}
