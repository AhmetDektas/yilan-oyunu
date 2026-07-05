using UnityEngine;

/// <summary>
/// Auto-defense structure: every fireInterval seconds, damages the
/// nearest living Animal within range (no player input needed). Meat
/// from kills accumulates locally (StoredMeat) instead of going to the
/// player's carry — walk the character into this tower's trigger to
/// collect it, the same "go there and get it" rhythm as chopping/hunting.
///
/// Collider on this GameObject should be a trigger sized as the
/// collection radius. If you want the tower to also physically block
/// movement/NavMesh, add a separate child GameObject with its own
/// non-trigger Collider marked Navigation Static.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ArcherTower : MonoBehaviour
{
    [Header("Savunma")]
    public float range = 4f;
    public int damage = 20;
    public float fireInterval = 1.5f;

    [Header("Depolanan et")]
    public int storedMeatCap = 30;
    public int StoredMeat { get; private set; }

    float lastFireAt = -10f;

    void Update()
    {
        if (Time.time - lastFireAt < fireInterval) return;

        Animal target = FindNearestAnimal();
        if (target == null) return;

        lastFireAt = Time.time;
        bool died = target.TakeDamage(damage);
        if (died && StoredMeat < storedMeatCap)
        {
            int amount = Mathf.Min(Random.Range(4, 9), storedMeatCap - StoredMeat);
            StoredMeat += amount;
        }
    }

    Animal FindNearestAnimal()
    {
        var hits = Physics.OverlapSphere(transform.position, range);
        Animal nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var hit in hits)
        {
            var a = hit.GetComponent<Animal>();
            if (a == null || a.IsDead) continue;
            float d = Vector3.Distance(transform.position, a.transform.position);
            if (d < nearestDist) { nearestDist = d; nearest = a; }
        }
        return nearest;
    }

    void OnTriggerStay(Collider other)
    {
        if (StoredMeat <= 0) return;
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        int collected = player.AddCarryMeat(StoredMeat);
        StoredMeat -= collected;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
#endif
}
