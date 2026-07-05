using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Subscribes to GameManager.OnGameOver and shows a simple "iflas" panel
/// with the final day count and a restart button. Keep panelRoot
/// inactive in the Canvas by default — this script hides/shows it.
/// </summary>
public class GameOverPanel : MonoBehaviour
{
    public GameObject panelRoot;
    public TMP_Text messageText;
    public Button restartButton;

    void Start()
    {
        panelRoot.SetActive(false);
        GameManager.Instance.OnGameOver += HandleGameOver;
        restartButton.onClick.AddListener(() =>
        {
            GameManager.Instance.Restart();
            panelRoot.SetActive(false);
        });
    }

    void HandleGameOver()
    {
        messageText.text = $"{GameManager.Instance.State.day} gün yönettin. Otel iflas etti ve devretmek zorunda kaldın.";
        panelRoot.SetActive(true);
    }
}
