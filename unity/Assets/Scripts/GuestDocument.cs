using System;
using System.Collections.Generic;

/// <summary>
/// The "Papers, Please"-style ID card shown to the player for each
/// booking. IsTrouble is the hidden ground truth — the player only sees
/// ClaimedType/Occupation/CarriedItem and the IsSuspicious flag (which
/// doesn't perfectly correlate with IsTrouble, so blindly rejecting every
/// suspicious-looking guest is a losing strategy).
/// </summary>
[Serializable]
public class GuestDocument
{
    public string Occupation;
    public string CarriedItem;
    public bool IsSuspicious;
    public bool IsTrouble;
}

public static class GuestDocumentGenerator
{
    static readonly System.Random Rng = new System.Random();

    const double TroubleChance = 0.25;     // fraction of arrivals that are actually trouble
    const double TroubleTellChance = 0.7;  // trouble guests show a visible red flag this often (rest slip through clean)
    const double FalseFlagChance = 0.08;   // legit guests rarely carry something odd too (red herring)

    static readonly Dictionary<GuestTypeKey, string[]> NormalOccupations = new Dictionary<GuestTypeKey, string[]>
    {
        { GuestTypeKey.Backpacker, new[] { "Öğrenci", "Fotoğrafçı", "Serbest Yazar" } },
        { GuestTypeKey.Family,     new[] { "Öğretmen", "Çiftçi", "Emekli" } },
        { GuestTypeKey.Business,   new[] { "Yatırımcı", "Avukat", "Satış Müdürü" } },
    };

    static readonly Dictionary<GuestTypeKey, string[]> NormalItems = new Dictionary<GuestTypeKey, string[]>
    {
        { GuestTypeKey.Backpacker, new[] { "Uyku Tulumu", "Harita", "Fotoğraf Makinesi" } },
        { GuestTypeKey.Family,     new[] { "Piknik Sepeti", "Oyuncak", "Aile Fotoğrafı" } },
        { GuestTypeKey.Business,   new[] { "Evrak Çantası", "Dizüstü Bilgisayar", "Kartvizit Kutusu" } },
    };

    static readonly string[] TroubleOccupations = { "Kaçak Avcı", "Silah Taciri", "Belgesiz Gezgin", "Kaçakçı" };
    static readonly string[] TroubleItems = { "Bilinmeyen Paket", "Belgesiz Silah", "Gizli Çanta", "Şüpheli Kutu" };

    /// <param name="troubleChanceBonus">Added to the base trouble chance (e.g. GameManager's day-based difficulty ramp) — kept as a plain parameter, not a GameManager reference, so this class stays UnityEngine-free.</param>
    public static GuestDocument Generate(GuestTypeKey type, double troubleChanceBonus = 0)
    {
        double troubleChance = Math.Min(0.9, TroubleChance + troubleChanceBonus);
        bool isTrouble = Rng.NextDouble() < troubleChance;
        var doc = new GuestDocument { IsTrouble = isTrouble };

        if (isTrouble && Rng.NextDouble() < TroubleTellChance)
        {
            doc.IsSuspicious = true;
            doc.Occupation = Pick(TroubleOccupations);
            doc.CarriedItem = Pick(TroubleItems);
        }
        else if (!isTrouble && Rng.NextDouble() < FalseFlagChance)
        {
            doc.IsSuspicious = true;
            doc.Occupation = Pick(NormalOccupations[type]);
            doc.CarriedItem = Pick(TroubleItems); // legit guest, odd item — red herring
        }
        else
        {
            doc.IsSuspicious = false;
            doc.Occupation = Pick(NormalOccupations[type]);
            doc.CarriedItem = Pick(NormalItems[type]);
        }
        return doc;
    }

    static string Pick(string[] arr) => arr[Rng.Next(arr.Length)];
}
