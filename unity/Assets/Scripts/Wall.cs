using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A physical, damageable wall segment. Animal in "Approach" state target
/// the nearest live Wall instead of heading straight for the hotel (see
/// Animal.cs) — they have to break through it first. Once a wall's HP
/// hits 0 it breaks: stops blocking movement and can no longer be
/// targeted, so animals path past it toward the hotel from then on.
/// Walk a carrying PlayerController near a broken wall to auto-repair it
/// with wood (same proximity-check idea as ArcherTower/TryAutoChop — no
/// tap needed).
///
/// Setup: add a NavMeshObstacle component (Carve on) so the wall blocks
/// pathing while intact and stops blocking automatically when broken
/// (no re-bake needed), plus a non-trigger Collider so animals/the
/// repair check can detect it via Physics.OverlapSphere.
/// </summary>
public class Wall : MonoBehaviour
{
    [Header("Dayanıklılık")]
    public int maxHp = 200;
    public int hp;

    [Header("Onarım")]
    public float repairRange = 1.5f;
    public double repairWoodCost = 40;

    public bool IsBroken => hp <= 0;

    NavMeshObstacle obstacle;
    Renderer rend;
    Color intactColor;
    float lastRepairCheckAt = -10f;
    const float RepairCheckInterval = 0.5f;

    void Awake()
    {
        hp = maxHp;
        obstacle = GetComponent<NavMeshObstacle>();
        rend = GetComponentInChildren<Renderer>();
        if (rend != null) intactColor = rend.material.color;
    }

    void Update()
    {
        if (!IsBroken) return;
        if (Time.time - lastRepairCheckAt < RepairCheckInterval) return;
        lastRepairCheckAt = Time.time;

        foreach (var hit in Physics.OverlapSphere(transform.position, repairRange))
        {
            if (hit.GetComponent<PlayerController>() == null) continue;
            if (GameManager.Instance.State.wood < repairWoodCost) return;
            GameManager.Instance.State.wood -= repairWoodCost;
            Repair();
            return;
        }
    }

    /// <returns>true if this hit broke the wall.</returns>
    public bool TakeDamage(int amount)
    {
        if (IsBroken) return false;
        hp = Mathf.Max(0, hp - amount);
        if (hp <= 0) { Break(); return true; }
        return false;
    }

    void Break()
    {
        if (obstacle != null) obstacle.enabled = false;
        if (rend != null) rend.material.color = intactColor * 0.4f; // simple charred/broken visual cue
    }

    void Repair()
    {
        hp = maxHp;
        if (obstacle != null) obstacle.enabled = true;
        if (rend != null) rend.material.color = intactColor;
    }
}
