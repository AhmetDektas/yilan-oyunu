using UnityEngine;

/// <summary>
/// A marker for a buildable archer-tower slot. Walk the player into its
/// trigger to build a tower there for `cost` money (if affordable) — same
/// "walk there, it happens" rhythm as DropZone. The site consumes itself
/// once built so it can't be re-built.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TowerBuildSite : MonoBehaviour
{
    public GameObject archerTowerPrefab;
    public double cost = 4000;

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        if (!GameManager.Instance.TryBuildArcherTower(cost)) return;

        Instantiate(archerTowerPrefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
