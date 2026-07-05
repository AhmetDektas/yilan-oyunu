using UnityEngine;

/// <summary>
/// A marker for a buildable archer-tower slot. Walk the player into its
/// trigger to build a tower there for `cost` money (if affordable) — same
/// "walk there, it happens" rhythm as DropZone. The site consumes itself
/// once built so it can't be re-built.
///
/// Set a unique siteId per site in the Inspector — TowerPersistence uses
/// it to match a saved tower back to this exact slot on next launch (and
/// to destroy the slot so a restored tower doesn't get a duplicate).
/// </summary>
[RequireComponent(typeof(Collider))]
public class TowerBuildSite : MonoBehaviour
{
    public string siteId;
    public GameObject archerTowerPrefab;
    public double cost = 4000;

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        if (!GameManager.Instance.TryBuildArcherTower(cost)) return;

        var go = Instantiate(archerTowerPrefab, transform.position, transform.rotation);
        var tower = go.GetComponent<ArcherTower>();
        if (tower != null) tower.SiteId = siteId;
        Destroy(gameObject);
    }
}
