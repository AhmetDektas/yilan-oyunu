using UnityEngine;

/// <summary>
/// Simple screen switcher for the bottom tab bar (Harita / Otel / Personel /
/// Olanaklar / Başarım). Assign the five root panel GameObjects in the same
/// order as your tab buttons; wire each button's OnClick to ShowTab(index).
/// </summary>
public class TabController : MonoBehaviour
{
    public GameObject[] panels;

    void Start() => ShowTab(0);

    public void ShowTab(int index)
    {
        for (int i = 0; i < panels.Length; i++)
            panels[i].SetActive(i == index);
    }
}
