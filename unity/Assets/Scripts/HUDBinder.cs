using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Binds the top HUD (day/money/reputation/wood/meat + day-progress bar +
/// carry indicator) to GameManager/PlayerController. Assign the TMP_Text
/// fields to the Canvas Text objects in the Inspector; polling in Update()
/// keeps this simple since HUD text updates are cheap.
/// </summary>
public class HUDBinder : MonoBehaviour
{
    [Header("Üst bar")]
    public TMP_Text dayText;
    public TMP_Text moneyText;
    public TMP_Text reputationText;
    public TMP_Text woodText;
    public TMP_Text meatText;
    public Slider dayProgressSlider;

    [Header("Taşıma çubuğu")]
    public PlayerController player;
    public TMP_Text carryWoodText;
    public TMP_Text carryMeatText;

    void Update()
    {
        var s = GameManager.Instance.State;
        dayText.text = $"Gün {s.day}";
        moneyText.text = FormatMoney(s.money);
        reputationText.text = Mathf.RoundToInt((float)s.reputation).ToString();
        woodText.text = Mathf.FloorToInt((float)s.wood).ToString();
        meatText.text = Mathf.FloorToInt((float)s.meat).ToString();
        if (dayProgressSlider != null) dayProgressSlider.value = Mathf.Clamp01(s.dayProgress);

        if (player != null)
        {
            if (carryWoodText != null) carryWoodText.text = $"{player.CarryWood}/{player.carryCap}";
            if (carryMeatText != null) carryMeatText.text = $"{player.CarryMeat}/{player.carryCap}";
        }
    }

    string FormatMoney(double n)
    {
        if (n >= 1000000) return (n / 1000000).ToString("0.00") + "M₺";
        return n.ToString("N0") + "₺";
    }
}
