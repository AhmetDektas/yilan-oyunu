using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Wanders a rectangular area (via NavMeshAgent, same pathfinding
/// approach as PlayerController) and periodically decides to approach
/// the hotel instead. While approaching, it targets the nearest *live*
/// Wall (if any exist) and attacks that instead of heading straight for
/// the hotel — only once no unbroken wall remains does it proceed to the
/// hotel front and raid (damages a random room + steals stockpiled meat,
/// mitigated by hired Güvenlik) before respawning elsewhere after a
/// delay. The player can kill it first via PlayerController's
/// auto-attack for a guaranteed meat drop. approachChance itself grows
/// over the run via GameManager.AnimalAggressionBonus() (day-based
/// difficulty ramp), so animals get bolder the longer the hotel survives
/// instead of staying at a flat difficulty forever.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Animal : MonoBehaviour
{
    [Header("Sağlık")]
    public int maxHp = 60;

    [Header("Hareket")]
    public float wanderSpeed = 1.2f;
    public float approachSpeed = 2.1f;
    public Vector3 wanderAreaMin;
    public Vector3 wanderAreaMax;
    [Tooltip("Otelin önündeki, duvar yoksa/yıkılmışsa hayvanın saldırı için hedeflediği nokta.")]
    public Transform hotelFrontMarker;

    [Header("Duvara saldırı")]
    public float wallAttackRange = 1.3f;
    public float wallAttackInterval = 1f;
    public int wallDamage = 15;

    [Header("Davranış")]
    public float decisionIntervalMin = 2.5f;
    public float decisionIntervalMax = 4.5f;
    [Range(0f, 1f)] public float approachChance = 0.35f;
    public float respawnDelayMin = 6f;
    public float respawnDelayMax = 12f;

    public bool IsDead { get; private set; }

    int hp;
    enum AnimalState { Wander, Approach }
    AnimalState state = AnimalState.Wander;
    float nextDecisionAt;
    float lastWallAttackAt = -10f;
    NavMeshAgent agent;
    Renderer rend;
    Collider col;

    void Awake()
    {
        hp = maxHp;
        agent = GetComponent<NavMeshAgent>();
        rend = GetComponentInChildren<Renderer>();
        col = GetComponent<Collider>();
    }

    void Update()
    {
        if (IsDead) return;

        double gFactor = GameManager.Instance.GuvenlikFactor();
        double wFactor = GameManager.Instance.WallFactor();
        agent.speed = state == AnimalState.Approach ? approachSpeed : wanderSpeed;

        if (Time.time >= nextDecisionAt)
        {
            nextDecisionAt = Time.time + Random.Range(decisionIntervalMin, decisionIntervalMax);
            double aggression = Mathf.Clamp01((float)(approachChance + GameManager.Instance.AnimalAggressionBonus()));
            if (state == AnimalState.Wander && Random.value < aggression * gFactor * wFactor)
            {
                state = AnimalState.Approach;
            }
            else
            {
                state = AnimalState.Wander;
                Vector3 t = new Vector3(
                    Random.Range(wanderAreaMin.x, wanderAreaMax.x),
                    transform.position.y,
                    Random.Range(wanderAreaMin.z, wanderAreaMax.z));
                agent.SetDestination(t);
            }
        }

        if (state == AnimalState.Approach) UpdateApproach();
    }

    void UpdateApproach()
    {
        var wall = FindNearestLiveWall();
        if (wall != null)
        {
            agent.SetDestination(wall.transform.position);
            if (Vector3.Distance(transform.position, wall.transform.position) <= wallAttackRange &&
                Time.time - lastWallAttackAt >= wallAttackInterval)
            {
                lastWallAttackAt = Time.time;
                wall.TakeDamage(wallDamage);
            }
            return;
        }

        // No unbroken wall stands between here and the hotel — proceed to raid.
        if (hotelFrontMarker == null) return;
        agent.SetDestination(hotelFrontMarker.position);
        if (Vector3.Distance(transform.position, hotelFrontMarker.position) < 0.6f)
        {
            GameManager.Instance.AnimalRaid();
            Die();
        }
    }

    Wall FindNearestLiveWall()
    {
        Wall nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var w in FindObjectsOfType<Wall>())
        {
            if (w.IsBroken) continue;
            float d = Vector3.Distance(transform.position, w.transform.position);
            if (d < nearestDist) { nearestDist = d; nearest = w; }
        }
        return nearest;
    }

    /// <returns>true if this hit killed the animal.</returns>
    public bool TakeDamage(int amount)
    {
        if (IsDead) return false;
        hp -= amount;
        if (hp <= 0) { Die(); return true; }
        return false;
    }

    void Die()
    {
        IsDead = true;
        if (rend != null) rend.enabled = false;
        if (col != null) col.enabled = false;
        agent.enabled = false;
        Invoke(nameof(Respawn), Random.Range(respawnDelayMin, respawnDelayMax));
    }

    void Respawn()
    {
        hp = maxHp;
        IsDead = false;
        state = AnimalState.Wander;
        Vector3 pos = new Vector3(
            Random.Range(wanderAreaMin.x, wanderAreaMax.x),
            transform.position.y,
            Random.Range(wanderAreaMin.z, wanderAreaMax.z));
        agent.enabled = true;
        agent.Warp(pos);
        if (rend != null) rend.enabled = true;
        if (col != null) col.enabled = true;
    }
}
