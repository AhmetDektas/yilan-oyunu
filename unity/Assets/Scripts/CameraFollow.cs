using UnityEngine;

/// <summary>
/// Follows a target (the player) with a fixed offset and smooth Lerp
/// damping, run in LateUpdate so it settles after the target has already
/// moved this frame (no jitter). Pattern adapted from
/// github.com/KaganAyten/Click-MoveSource's CameraFollow.cs.
///
/// Pairs with IsometricCameraRig (that one sets the angle/orthographic
/// size; this one keeps the camera positioned relative to the player).
/// Workflow: position the Main Camera by hand in the Editor until the
/// isometric framing looks right, assign Target, then right-click this
/// component → "Offset'i Şu Anki Konumdan Hesapla" to snapshot that
/// relative position as the follow offset.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 10f, -10f);
    public float followSpeed = 5f;

    void Awake()
    {
        if (target == null)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followSpeed);
    }

    [ContextMenu("Offset'i Şu Anki Konumdan Hesapla")]
    void ComputeOffsetFromCurrentPosition()
    {
        if (target == null) return;
        offset = transform.position - target.position;
    }
}
