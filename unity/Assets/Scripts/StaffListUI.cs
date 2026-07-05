using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaffListUI : MonoBehaviour
{
    public Transform contentParent;
    public GameObject staffCardPrefab;

    readonly List<StaffCardUI> cards = new List<StaffCardUI>();

    void OnEnable() => Refresh();

    public void Refresh()
    {
        var keys = (StaffKey[])Enum.GetValues(typeof(StaffKey));
        while (cards.Count < keys.Length)
        {
            var go = Instantiate(staffCardPrefab, contentParent);
            cards.Add(go.GetComponent<StaffCardUI>());
        }
        for (int i = 0; i < keys.Length; i++)
        {
            cards[i].gameObject.SetActive(true);
            cards[i].Bind(keys[i], this);
        }
    }
}

public class StaffCardUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text footText;
    public Slider xpBar;
    public Button actionButton;
    public TMP_Text actionLabel;

    StaffKey key;

    public void Bind(StaffKey k, StaffListUI list)
    {
        key = k;
        var def = StaffSystem.Defs[k];
        var s = GameManager.Instance.State.staff[k];

        actionButton.onClick.RemoveAllListeners();

        if (!s.Hired)
        {
            titleText.text = def.Label;
            descText.text = def.Desc;
            footText.text = $"İşe alım: {def.HireCost:N0}₺ + {def.Wage:N0}₺/gün";
            if (xpBar != null) xpBar.gameObject.SetActive(false);
            actionLabel.text = "İşe Al";
            actionButton.interactable = GameManager.Instance.State.money >= def.HireCost;
            actionButton.onClick.AddListener(() => { GameManager.Instance.HireStaff(key); list.Refresh(); });
        }
        else
        {
            bool maxed = s.Level >= StaffSystem.MaxLevel;
            double xpNeed = StaffSystem.XpToNext(s.Level);
            titleText.text = $"{def.Label} · Sv.{s.Level}{(maxed ? " (MAX)" : "")}";
            descText.text = StaffEffectDesc(k, s.Level);
            footText.text = maxed ? "Maksimum seviye" : $"XP {Mathf.FloorToInt((float)s.Xp)}/{xpNeed:N0}";
            if (xpBar != null) { xpBar.gameObject.SetActive(true); xpBar.value = maxed ? 1f : (float)(s.Xp / xpNeed); }
            actionLabel.text = "Çalışıyor";
            actionButton.interactable = false;
        }
    }

    string StaffEffectDesc(StaffKey k, int level)
    {
        switch (k)
        {
            case StaffKey.Avci: return $"Günde {StaffSystem.AvciYield(level):N0} et topluyor";
            case StaffKey.Kapici: return $"Onarımda +{StaffSystem.KapiciRestore(level):N0} durum yeniler";
            case StaffKey.Temizlikci: return $"Yıpranmayı %{(1 - StaffSystem.TemizlikciFactor(level)) * 100:N0} azaltıyor";
            case StaffKey.Guvenlik: return $"Kötü olay/misafir kaybı riskini %{(1 - StaffSystem.GuvenlikFactor(level)) * 100:N0} azaltıyor";
            default: return "";
        }
    }
}
