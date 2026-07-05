using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to your Mixamo character GameObject. Drives the walk-to-unit
/// animation used by GameManager when a repair happens or a tenant moves in.
/// </summary>
public class CharacterWalker : MonoBehaviour
{
    public static CharacterWalker Instance { get; private set; }

    [Header("Referanslar")]
    public Animator animator;
    [Tooltip("Sırayla Daire 1, Daire 2, ... konumlarını işaret eden boş Transform'lar.")]
    public Transform[] unitMarkers;
    public Transform idleSpot;

    [Header("Hareket")]
    public float moveSpeed = 3f;
    public string isWalkingParam = "IsWalking";
    public string interactTrigger = "Interact";

    Coroutine currentRoutine;

    void Awake() => Instance = this;

    public void WalkToUnit(int unitId, string bubbleEmoji)
    {
        if (unitMarkers == null || unitId < 1 || unitId > unitMarkers.Length) return;
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(WalkRoutine(unitMarkers[unitId - 1].position, bubbleEmoji));
    }

    IEnumerator WalkRoutine(Vector3 target, string bubbleEmoji)
    {
        yield return MoveTo(target);

        if (animator != null) animator.SetTrigger(interactTrigger);
        ShowBubble(bubbleEmoji);
        yield return new WaitForSeconds(1.2f);

        if (idleSpot != null) yield return MoveTo(idleSpot.position);
    }

    IEnumerator MoveTo(Vector3 target)
    {
        if (animator != null) animator.SetBool(isWalkingParam, true);
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            Vector3 dir = target - transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
            yield return null;
        }
        if (animator != null) animator.SetBool(isWalkingParam, false);
    }

    void ShowBubble(string emoji)
    {
        // Minimal placeholder: swap this for a world-space TextMeshPro label,
        // a UI popup anchored via Camera.WorldToScreenPoint, or a particle burst.
        Debug.Log($"[Kapıcı] {emoji}");
    }
}
