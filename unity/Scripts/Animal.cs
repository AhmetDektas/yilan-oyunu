using UnityEngine;

/// <summary>
/// Wanders a rectangular area; periodically decides to approach the
/// hotel instead. If it reaches the hotel unopposed, it raids (damages a
/// random room + steals stockpiled meat, mitigated by hired Güvenlik)
/// then respawns elsewhere after a delay. The player can kill it first
/// via PlayerController's tap-to-attack for a guaranteed meat drop.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Animal : MonoBehaviour
{
    [Header("Sağlık")]
    public int maxHp = 60;

    [Header("Hareket")]
    public float wanderSpeed = 1.2f;
    public float approachSpeed = 2.1f;
    public Vector2 wanderAreaMin;
    public Vector2 wanderAreaMax;
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
    Vector2 wanderTarget;
    float nextDecisionAt;
    SpriteRenderer sr;
    Collider2D col;

    void Awake()
    {
        hp = maxHp;
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (IsDead) return;

        double gFactor = GameManager.Instance.GuvenlikFactor();

        if (Time.time >= nextDecisionAt)
        {
            nextDecisionAt = Time.time + Random.Range(decisionIntervalMin, decisionIntervalMax);
            if (state == AnimalState.Wander && Random.value < approachChance * gFactor)
            {
                state = AnimalState.Approach;
            }
            else
            {
                state = AnimalState.Wander;
                wanderTarget = new Vector2(Random.Range(wanderAreaMin.x, wanderAreaMax.x), Random.Range(wanderAreaMin.y, wanderAreaMax.y));
            }
        }

        Vector2 targetPos = (state == AnimalState.Approach && hotelFrontMarker != null)
            ? (Vector2)hotelFrontMarker.position
            : wanderTarget;
        float speed = state == AnimalState.Approach ? approachSpeed : wanderSpeed;
        transform.position = Vector2.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        if (state == AnimalState.Approach && hotelFrontMarker != null &&
            Vector2.Distance(transform.position, hotelFrontMarker.position) < 0.3f)
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
        if (sr != null) sr.enabled = false;
        col.enabled = false;
        Invoke(nameof(Respawn), Random.Range(respawnDelayMin, respawnDelayMax));
    }

    void Respawn()
    {
        hp = maxHp;
        IsDead = false;
        state = AnimalState.Wander;
        transform.position = new Vector2(Random.Range(wanderAreaMin.x, wanderAreaMax.x), Random.Range(wanderAreaMin.y, wanderAreaMax.y));
        if (sr != null) sr.enabled = true;
        col.enabled = true;
    }
}
