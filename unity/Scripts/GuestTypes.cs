using System;
using System.Collections.Generic;

public enum GuestTypeKey { Backpacker, Family, Business }

public class GuestTypeDef
{
    public string Label;
    public string Icon;
    public int Weight;
    public double RentFactorMin;
    public double RentFactorMax;
    public double BaseHappiness;
    public double RentSensitivity;
    public double IssueSensitivity;
    public double Patience;
}

public static class GuestTypes
{
    public static readonly Dictionary<GuestTypeKey, GuestTypeDef> Defs = new Dictionary<GuestTypeKey, GuestTypeDef>
    {
        { GuestTypeKey.Backpacker, new GuestTypeDef {
            Label = "Sırt Çantalı Gezgin", Icon = "🎒", Weight = 35,
            RentFactorMin = 0.65, RentFactorMax = 0.95, BaseHappiness = 78,
            RentSensitivity = 1.4, IssueSensitivity = 0.6, Patience = 0.75,
        } },
        { GuestTypeKey.Family, new GuestTypeDef {
            Label = "Aile", Icon = "👪", Weight = 40,
            RentFactorMin = 0.9, RentFactorMax = 1.25, BaseHappiness = 70,
            RentSensitivity = 1.0, IssueSensitivity = 1.3, Patience = 1.0,
        } },
        { GuestTypeKey.Business, new GuestTypeDef {
            Label = "İş İnsanı", Icon = "💼", Weight = 25,
            RentFactorMin = 1.1, RentFactorMax = 1.6, BaseHappiness = 62,
            RentSensitivity = 0.6, IssueSensitivity = 1.6, Patience = 1.3,
        } },
    };

    static readonly System.Random Rng = new System.Random();

    public static GuestTypeKey PickRandom()
    {
        int totalWeight = 0;
        foreach (var d in Defs.Values) totalWeight += d.Weight;
        int r = Rng.Next(totalWeight);
        foreach (var kv in Defs)
        {
            if (r < kv.Value.Weight) return kv.Key;
            r -= kv.Value.Weight;
        }
        return GuestTypeKey.Family;
    }
}
