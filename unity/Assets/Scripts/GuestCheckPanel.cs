using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// "Papers, Please"-style ID check popup. RoomCardUI calls Open() when
/// its room has a pending booking instead of accepting directly; the
/// player reviews the claimed occupation/carried item, the procedurally
/// drawn portrait (see PortraitGenerator — no art assets needed) and the
/// visible suspicious flag, if any, then taps İçeri Al / Reddet. The
/// consequence (including the hidden IsTrouble reveal) is resolved by
/// GameManager.ResolveEntryDecision.
/// </summary>
public class GuestCheckPanel : MonoBehaviour
{
    public GameObject panelRoot;
    public Image portraitImage;
    public TMP_Text nameText;
    public TMP_Text typeText;
    public TMP_Text occupationText;
    public TMP_Text itemText;
    public TMP_Text rentText;
    public GameObject suspiciousFlagIcon;
    public Button allowButton;
    public Button denyButton;

    int currentUnitId = -1;
    RoomListUI roomList;

    public void Open(RoomUnit u, RoomListUI list)
    {
        if (u.applicant == null || u.applicant.doc == null) return;
        currentUnitId = u.id;
        roomList = list;

        var booking = u.applicant;
        var doc = booking.doc;

        nameText.text = booking.name;
        typeText.text = GuestTypes.Defs[booking.type].Label;
        occupationText.text = "Meslek: " + doc.Occupation;
        itemText.text = "Eşya: " + doc.CarriedItem;
        rentText.text = $"Bütçe ≤ {booking.maxRent:N0}₺  ·  İstenen {u.rent:N0}₺";
        if (suspiciousFlagIcon != null) suspiciousFlagIcon.SetActive(doc.IsSuspicious);
        if (portraitImage != null) portraitImage.sprite = PortraitGenerator.Generate(booking.name, doc.IsSuspicious);

        allowButton.interactable = u.rent <= booking.maxRent;
        allowButton.onClick.RemoveAllListeners();
        allowButton.onClick.AddListener(() => Resolve(true));
        denyButton.onClick.RemoveAllListeners();
        denyButton.onClick.AddListener(() => Resolve(false));

        panelRoot.SetActive(true);
    }

    void Resolve(bool allowIn)
    {
        GameManager.Instance.ResolveEntryDecision(currentUnitId, allowIn);
        panelRoot.SetActive(false);
        if (roomList != null) roomList.Refresh();
    }
}
