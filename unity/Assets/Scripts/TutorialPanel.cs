using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time "nasıl oynanır" panel shown the first time the game ever
/// launches (tracked via a PlayerPrefs flag, independent of the save
/// system so it still shows even before any GameState save exists).
/// Assign panelRoot (a Panel with explanatory TMP_Text already authored
/// in the Editor — no dynamic text needed here) and a dismiss Button.
/// </summary>
public class TutorialPanel : MonoBehaviour
{
    const string SeenKey = "OrmanOtelTutorialSeen";

    public GameObject panelRoot;
    public Button dismissButton;

    void Start()
    {
        bool alreadySeen = PlayerPrefs.GetInt(SeenKey, 0) == 1;
        panelRoot.SetActive(!alreadySeen);

        if (dismissButton != null)
            dismissButton.onClick.AddListener(Dismiss);
    }

    void Dismiss()
    {
        PlayerPrefs.SetInt(SeenKey, 1);
        PlayerPrefs.Save();
        panelRoot.SetActive(false);
    }
}
