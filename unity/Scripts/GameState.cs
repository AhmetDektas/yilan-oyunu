using System;
using System.Collections.Generic;

public enum UpgradeKey { Rent, Applicant, Insulation, Ad }

[Serializable]
public class GameState
{
    public int day = 1;
    public double money = 6000;
    public double wood = 60;
    public double meat = 0;
    public double reputation = 50;
    public int badMoneyStreak;
    public double totalEarned;
    public double totalMeatCollected;
    public double totalWoodChopped;
    public float dayProgress;
    public int speed = 1;

    public List<RoomUnit> units = new List<RoomUnit>();
    public Dictionary<StaffKey, StaffMember> staff = new Dictionary<StaffKey, StaffMember>();
    public Dictionary<UpgradeKey, int> upgrades = new Dictionary<UpgradeKey, int>();
    public HashSet<string> achievements = new HashSet<string>();
    public List<string> log = new List<string>();
}
