using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Wanders a rectangular area (via NavMeshAgent, same pathfinding
/// approach as PlayerController) and periodically decides to approach
/// the hotel instead. If it reaches the hotel unopposed, it raids
/// (damages a random room + steals stockpiled meat, mitigated by hired
/// Güvenlik) then respawns elsewhere after a delay. The player can kill
/// it first via PlayerController's tap-to-attack for a guaranteed meat
/// drop.
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
    [Tooltip("Otelin önündeki, hayvanın saldırı için hedeflediği nokta.")]
    public Transform hotelFrontMarker;

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
        agent.speed = state == AnimalState.Approach ? approachSpeed : wanderSpeed;

        if (Time.time >= nextDecisionAt)
        {
            nextDecisionAt = Time.time + Random.Range(decisionIntervalMin, decisionIntervalMax);
            if (state == AnimalState.Wander && Random.value < approachChance * gFactor)
            {
                state = AnimalState.Approach;
                if (hotelFrontMarker != null) agent.SetDestination(hotelFrontMarker.position);
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

        if (state == AnimalState.Approach && hotelFrontMarker != null &&
            Vector3.Distance(transform.position, hotelFrontMarker.position) < 0.6f)
        {
            GameManager.Instance.AnimalRaid();
            Die();
        }
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
