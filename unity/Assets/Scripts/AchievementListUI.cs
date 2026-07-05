using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AchievementListUI : MonoBehaviour
{
    public Transform contentParent;
    public GameObject achievementCardPrefab;

    readonly List<AchievementCardUI> cards = new List<AchievementCardUI>();

    void OnEnable() => Refresh();

    public void Refresh()
    {
        while (cards.Count < Achievements.All.Count)
        {
            var go = Instantiate(achievementCardPrefab, contentParent);
            cards.Add(go.GetComponent<AchievementCardUI>());
        }
        for (int i = 0; i < Achievements.All.Count; i++)
        {
            cards[i].gameObject.SetActive(true);
            cards[i].Bind(Achievements.All[i]);
        }
    }
}

public class AchievementCardUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text statusText;
    public CanvasGroup canvasGroup;

    public void Bind(AchievementDef a)
    {
        bool unlocked = GameManager.Instance.State.achievements.Contains(a.Id);
        titleText.text = $"{a.Icon} {a.Label}";
        descText.text = a.Desc;
        statusText.text = unlocked ? "✅" : "🔒";
        if (canvasGroup != null) canvasGroup.alpha = unlocked ? 1f : 0.5f;
    }
}
