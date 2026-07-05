using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Subscribes to PlayerController.OnCarryFull and briefly shows a toast
/// message ("Odun kapasitesi doldu, depoya bırak!" etc.) so the player
/// gets feedback instead of chopping/attacking silently no-oping once
/// carryCap is hit. Assign a TMP_Text positioned wherever you'd like the
/// toast to appear (e.g. top-center of the Harita screen); it starts
/// hidden and fades itself out after `visibleSeconds`.
/// </summary>
public class CarryFullToast : MonoBehaviour
{
    public PlayerController player;
    public TMP_Text toastText;
    public float visibleSeconds = 2.5f;

    Coroutine hideRoutine;

    void Start()
    {
        if (toastText != null) toastText.gameObject.SetActive(false);
        if (player == null) player = FindObjectOfType<PlayerController>();
        if (player != null) player.OnCarryFull += HandleCarryFull;
    }

    void OnDestroy()
    {
        if (player != null) player.OnCarryFull -= HandleCarryFull;
    }

    void HandleCarryFull(string resource)
    {
        if (toastText == null) return;
        toastText.text = resource == "wood"
            ? "🪵 Odun kapasitesi doldu — Depo'ya bırakmalısın!"
            : "🥩 Et kapasitesi doldu — Yemekhane'ye bırakmalısın!";
        toastText.gameObject.SetActive(true);

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(visibleSeconds);
        toastText.gameObject.SetActive(false);
    }
}
