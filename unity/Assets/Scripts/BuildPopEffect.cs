using System.Collections;
using UnityEngine;

/// <summary>
/// Punchy "pop into existence" scale-in for newly built/spawned objects
/// (archer towers, new room cards) — an overshoot-then-settle animation,
/// the "tak!" snap feel from games like Wild Survival, instead of things
/// silently appearing at full size. Works on any Transform (3D world
/// objects and UI RectTransforms both drive localScale). Call
/// Play(GameObject) right after Instantiate — it adds the component
/// itself if the prefab doesn't already have one, so no prefab setup is
/// required. Not used for TowerPersistence's save-restore path on purpose:
/// a restored tower already existed, it isn't being "built" right now.
/// </summary>
public class BuildPopEffect : MonoBehaviour
{
    public float duration = 0.32f;
    public AnimationCurve curve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.6f, 1.15f),
        new Keyframe(1f, 1f));

    public static void Play(GameObject go)
    {
        var effect = go.GetComponent<BuildPopEffect>();
        if (effect == null) effect = go.AddComponent<BuildPopEffect>();
        effect.StartCoroutine(effect.Animate());
    }

    IEnumerator Animate()
    {
        Vector3 target = transform.localScale;
        if (target == Vector3.zero) target = Vector3.one;
        transform.localScale = Vector3.zero;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = curve.Evaluate(Mathf.Clamp01(t / duration));
            transform.localScale = target * k;
            yield return null;
        }
        transform.localScale = target;
    }
}
