using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmenityListUI : MonoBehaviour
{
    public Transform contentParent;
    public GameObject amenityCardPrefab;

    readonly List<AmenityCardUI> cards = new List<AmenityCardUI>();

    void OnEnable() => Refresh();

    public void Refresh()
    {
        var keys = (UpgradeKey[])Enum.GetValues(typeof(UpgradeKey));
        while (cards.Count < keys.Length)
        {
            var go = Instantiate(amenityCardPrefab, contentParent);
            cards.Add(go.GetComponent<AmenityCardUI>());
        }
        for (int i = 0; i < keys.Length; i++)
        {
            cards[i].gameObject.SetActive(true);
            cards[i].Bind(keys[i], this);
        }
    }
}

public class AmenityCardUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text levelText;
    public Button buyButton;
    public TMP_Text buyLabel;

    UpgradeKey key;

    public void Bind(UpgradeKey k, AmenityListUI list)
    {
        key = k;
        var def = AmenitySystem.Defs[k];
        var state = GameManager.Instance.State;
        int level = state.upgrades[k];
        bool maxed = level >= def.Max;
        double cost = AmenitySystem.Cost(state, k);

        titleText.text = def.Label;
        descText.text = def.Desc;
        levelText.text = $"Seviye {level}/{def.Max}";
        buyLabel.text = maxed ? "MAX" : $"{cost:N0}₺";
        buyButton.interactable = !maxed && state.money >= cost;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => { GameManager.Instance.BuyAmenity(key); list.Refresh(); });
    }
}
