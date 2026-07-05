using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Populates a ScrollView Content with one card per room, reusing a
/// prefab (assign a prefab with a RoomCardUI component). Call Refresh()
/// whenever the room list changes (e.g. after ExpandHotel, or just call
/// it from a periodic UI-tick if you'd rather not wire every action).
/// </summary>
public class RoomListUI : MonoBehaviour
{
    public Transform contentParent;
    public GameObject roomCardPrefab;

    readonly List<RoomCardUI> cards = new List<RoomCardUI>();

    void OnEnable() => Refresh();

    public void Refresh()
    {
        var units = GameManager.Instance.State.units;
        while (cards.Count < units.Count)
        {
            var go = Instantiate(roomCardPrefab, contentParent);
            cards.Add(go.GetComponent<RoomCardUI>());
        }
        for (int i = 0; i < units.Count; i++)
        {
            cards[i].gameObject.SetActive(true);
            cards[i].Bind(units[i], this);
        }
        for (int i = units.Count; i < cards.Count; i++)
            cards[i].gameObject.SetActive(false);
    }
}

public class RoomCardUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text statusText;
    public TMP_Text rentText;
    public Button actionButton;
    public TMP_Text actionButtonLabel;

    int unitId;

    public void Bind(RoomUnit u, RoomListUI list)
    {
        unitId = u.id;
        titleText.text = $"Oda {u.id}";
        rentText.text = $"{u.rent:N0}₺/gece";

        if (u.tenant != null)
        {
            var def = GuestTypes.Defs[u.tenant.type];
            statusText.text = $"{def.Icon} {u.tenant.name} ({Mathf.RoundToInt((float)u.tenant.happiness)}%)";
        }
        else if (u.applicant != null)
        {
            statusText.text = $"🙋 Rezervasyon: {u.applicant.name}";
        }
        else
        {
            statusText.text = "BOŞ";
        }

        actionButton.onClick.RemoveAllListeners();

        if (u.issue != null)
        {
            double cost = IssueTypeData.Defs[u.issue.Value].WoodCost;
            actionButtonLabel.text = $"Tamir Et ({cost:N0} 🪵)";
            actionButton.interactable = GameManager.Instance.State.wood >= cost;
            actionButton.onClick.AddListener(() => { GameManager.Instance.RepairUnit(unitId); list.Refresh(); });
        }
        else if (u.applicant != null)
        {
            actionButtonLabel.text = "Kabul Et";
            actionButton.interactable = u.rent <= u.applicant.maxRent;
            actionButton.onClick.AddListener(() => { GameManager.Instance.AcceptBooking(unitId); list.Refresh(); });
        }
        else if (u.tenant != null)
        {
            actionButtonLabel.text = "Misafiri Çıkar";
            actionButton.interactable = true;
            actionButton.onClick.AddListener(() => { GameManager.Instance.EvictGuest(unitId); list.Refresh(); });
        }
        else
        {
            actionButtonLabel.text = "-";
            actionButton.interactable = false;
        }
    }
}
