using UnityEngine;

/// <summary>
/// Marker component for a choppable tree. All chop cooldown/amount logic
/// lives in PlayerController — this just needs a (non-trigger) Collider
/// so the player's tap-raycast (Physics.Raycast) can find it, and ideally
/// should be marked Navigation Static + Not Walkable so the NavMesh bake
/// treats it as an obstacle.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ResourceTree : MonoBehaviour
{
}
