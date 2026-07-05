using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Persists built ArcherTowers (site id + position + stored meat) across
/// sessions — separate from GameManager/SaveSystem's economy save, since
/// towers are live scene objects, not part of GameState. Restores them at
/// scene start by finding the matching TowerBuildSite (by siteId) and
/// replacing it with a tower carrying the saved StoredMeat, same as when
/// it was originally built.
///
/// Place once in the scene (e.g. on the same GameObject as GameManager).
/// Uses its own OnApplicationPause/OnApplicationQuit/autosave timer
/// rather than hooking into GameManager, since Unity calls those
/// lifecycle methods on every component that defines them.
/// </summary>
public class TowerPersistence : MonoBehaviour
{
    [Tooltip("Aynı prefab TowerBuildSite'lardaki Archer Tower Prefab ile eşleşmeli.")]
    public GameObject archerTowerPrefab;
    public float autosaveIntervalSeconds = 30f;

    float timer;

    void Start() => RestoreTowers();

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= autosaveIntervalSeconds)
        {
            timer = 0f;
            SaveSystem.SaveTowers(GatherLiveTowers());
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) SaveSystem.SaveTowers(GatherLiveTowers());
    }

    void OnApplicationQuit() => SaveSystem.SaveTowers(GatherLiveTowers());

    List<SaveSystem.TowerSave> GatherLiveTowers()
    {
        var list = new List<SaveSystem.TowerSave>();
        foreach (var t in FindObjectsOfType<ArcherTower>())
        {
            if (string.IsNullOrEmpty(t.SiteId)) continue;
            var pos = t.transform.position;
            list.Add(new SaveSystem.TowerSave { siteId = t.SiteId, x = pos.x, y = pos.y, z = pos.z, storedMeat = t.StoredMeat });
        }
        return list;
    }

    void RestoreTowers()
    {
        var saved = SaveSystem.LoadTowers();
        if (saved == null || saved.Count == 0) return;
        if (archerTowerPrefab == null)
        {
            Debug.LogWarning("TowerPersistence: Archer Tower Prefab atanmadı, kaydedilmiş kuleler geri yüklenemedi.");
            return;
        }

        var sites = FindObjectsOfType<TowerBuildSite>()
            .Where(s => !string.IsNullOrEmpty(s.siteId))
            .ToDictionary(s => s.siteId, s => s);

        foreach (var ts in saved)
        {
            Vector3 pos = new Vector3(ts.x, ts.y, ts.z);
            Quaternion rot = Quaternion.identity;

            if (!string.IsNullOrEmpty(ts.siteId) && sites.TryGetValue(ts.siteId, out var site))
            {
                pos = site.transform.position;
                rot = site.transform.rotation;
                Destroy(site.gameObject);
            }

            var go = Instantiate(archerTowerPrefab, pos, rot);
            var tower = go.GetComponent<ArcherTower>();
            if (tower != null)
            {
                tower.SiteId = ts.siteId;
                tower.SetStoredMeat(ts.storedMeat);
            }
        }
    }
}
