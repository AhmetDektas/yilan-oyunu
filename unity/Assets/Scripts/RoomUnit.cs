using System;
using System.Collections.Generic;

public enum IssueType { Leak, Boiler, Noise, Generator, Pest }

public class IssueTypeInfo
{
    public string Label;
    public string Icon;
    public double WoodCost;
}

public static class IssueTypeData
{
    public static readonly Dictionary<IssueType, IssueTypeInfo> Defs = new Dictionary<IssueType, IssueTypeInfo>
    {
        { IssueType.Leak,      new IssueTypeInfo { Label = "Su Tesisatı Arızası",  Icon = "💧", WoodCost = 20 } },
        { IssueType.Boiler,    new IssueTypeInfo { Label = "Isıtma/Kazan Arızası", Icon = "🔥", WoodCost = 35 } },
        { IssueType.Noise,     new IssueTypeInfo { Label = "Gürültü Şikayeti",     Icon = "🔊", WoodCost = 10 } },
        { IssueType.Generator, new IssueTypeInfo { Label = "Jeneratör Arızası",    Icon = "⚡", WoodCost = 25 } },
        { IssueType.Pest,      new IssueTypeInfo { Label = "Haşere İstilası",      Icon = "🐜", WoodCost = 8 } },
    };
}

[Serializable]
public class Guest
{
    public string name;
    public GuestTypeKey type;
    public double happiness = 70;
    public int unpaidStreak;
    /// <summary>True if this guest was admitted despite a suspicious ID check — they will eventually strike (steal money and flee) on a random later day.</summary>
    public bool isTrouble;
}

[Serializable]
public class Booking
{
    public string name;
    public GuestTypeKey type;
    public double maxRent;
    public GuestDocument doc;
}

[Serializable]
public class RoomUnit
{
    public int id;
    public double rent = 3200; // nightly rate
    public double condition = 95;
    public IssueType? issue;
    public Guest tenant;
    public Booking applicant;
}
