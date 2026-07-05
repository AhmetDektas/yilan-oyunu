using System;
using System.Collections.Generic;

public enum IssueType { Leak, Elevator, Noise, Power, Trash }

public class IssueTypeInfo
{
    public string Label;
    public string Icon;
    public double Cost;
}

public static class IssueTypeData
{
    public static readonly Dictionary<IssueType, IssueTypeInfo> Defs = new Dictionary<IssueType, IssueTypeInfo>
    {
        { IssueType.Leak,     new IssueTypeInfo { Label = "Su Kaçağı",         Icon = "💧", Cost = 600 } },
        { IssueType.Elevator, new IssueTypeInfo { Label = "Asansör Arızası",   Icon = "🛗", Cost = 1200 } },
        { IssueType.Noise,    new IssueTypeInfo { Label = "Gürültü Şikayeti",  Icon = "🔊", Cost = 200 } },
        { IssueType.Power,    new IssueTypeInfo { Label = "Elektrik Arızası",  Icon = "⚡", Cost = 500 } },
        { IssueType.Trash,    new IssueTypeInfo { Label = "Çöp Birikmesi",     Icon = "🗑️", Cost = 150 } },
    };
}

[Serializable]
public class Tenant
{
    public string name;
    public TenantTypeKey type;
    public double happiness = 70;
    public int unpaidStreak;
}

[Serializable]
public class Applicant
{
    public string name;
    public TenantTypeKey type;
    public double maxRent;
}

[Serializable]
public class ApartmentUnit
{
    public int id;
    public double rent = 3200;
    public double condition = 95;
    public IssueType? issue;
    public Tenant tenant;
    public Applicant applicant;
}
