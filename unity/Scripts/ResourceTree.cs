using UnityEngine;

/// <summary>
/// Marker component for a choppable tree. All chop cooldown/amount logic
/// lives in PlayerController — this just needs a Collider2D so the
/// player's tap-raycast (Physics2D.OverlapPoint) can find it.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ResourceTree : MonoBehaviour
{
}
