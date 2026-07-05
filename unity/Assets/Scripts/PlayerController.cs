using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Joystick-driven player character — no tap/raycast input at all, so it
/// can never conflict with Canvas UI buttons (the earlier tap-to-move
/// version could accidentally also move/attack when the player was just
/// trying to press a UI button underneath the tap). Movement comes from
/// a Joystick (camera-relative), while chopping/attacking happen
/// automatically: walk within range of a tree/animal and it fires on its
/// own cooldown, same idea as ArcherTower's auto-attack. Purchases
/// (towers, etc.) happen the same way — walk into the marked zone and it
/// resolves automatically (see TowerBuildSite/DropZone), no tap needed.
///
/// Setup: Window > AI > Navigation, mark the ground plane Navigation
/// Static + Walkable, mark tree/hotel/animal colliders Navigation
/// Static + Not Walkable, then Bake. This GameObject needs a
/// NavMeshAgent + a (kinematic) Rigidbody + a non-trigger Collider.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Hareket")]
    public Joystick joystick;
    public float moveSpeed = 3.5f;
    public Camera cam;

    [Header("Toplama")]
    public float chopRange = 1.2f;
    public float attackRange = 1.2f;
    public float chopCooldown = 1.1f;
    public float attackCooldown = 0.45f;
    public int attackDamage = 30;
    public int carryCap = 20;

    [Header("Referanslar")]
    public Animator animator;
    public string isWalkingParam = "IsWalking";

    public int CarryWood { get; private set; }
    public int CarryMeat { get; private set; }

    /// <summary>Fires once when a carry slot hits capacity (not every frame) — e.g. "wood" or "meat". Hook a toast/UI message to this.</summary>
    public event Action<string> OnCarryFull;

    NavMeshAgent agent;
    float lastChopAt = -10f;
    float lastAttackAt = -10f;
    bool woodWasFull;
    bool meatWasFull;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.updateRotation = false; // we rotate manually to face the joystick direction
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        Move();
        SyncAnimator();
        TryAutoChop();
        TryAutoAttack();
    }

    void Move()
    {
        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;
        if (input.sqrMagnitude < 0.01f)
        {
            agent.velocity = Vector3.zero;
            return;
        }

        Vector3 rawDir = new Vector3(input.x, 0f, input.y);
        Vector3 moveDir = rawDir;
        if (cam != null)
        {
            Quaternion camYaw = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            moveDir = camYaw * rawDir;
        }

        agent.velocity = moveDir * moveSpeed;
        transform.rotation = Quaternion.LookRotation(moveDir);
    }

    void SyncAnimator()
    {
        if (animator != null) animator.SetBool(isWalkingParam, agent.velocity.sqrMagnitude > 0.01f);
    }

    void TryAutoChop()
    {
        if (Time.time - lastChopAt < chopCooldown) return;
        if (CarryWood >= carryCap)
        {
            if (!woodWasFull) { woodWasFull = true; OnCarryFull?.Invoke("wood"); }
            return;
        }

        var tree = FindNearest<ResourceTree>(chopRange);
        if (tree == null) return;

        lastChopAt = Time.time;
        int amount = Mathf.Min(Random.Range(3, 7), carryCap - CarryWood);
        CarryWood += amount;
        GameManager.Instance.State.totalWoodChopped += amount;
        GameManager.Instance.CheckAchievements();
    }

    void TryAutoAttack()
    {
        if (Time.time - lastAttackAt < attackCooldown) return;

        var animal = FindNearest<Animal>(attackRange, a => !a.IsDead);
        if (animal == null) return;

        lastAttackAt = Time.time;
        bool died = animal.TakeDamage(attackDamage);
        if (died)
        {
            if (CarryMeat >= carryCap)
            {
                if (!meatWasFull) { meatWasFull = true; OnCarryFull?.Invoke("meat"); }
            }
            else
            {
                int amount = Mathf.Min(Random.Range(4, 9), carryCap - CarryMeat);
                CarryMeat += amount;
                GameManager.Instance.State.totalMeatCollected += amount;
                GameManager.Instance.CheckAchievements();
            }
        }
    }

    T FindNearest<T>(float range, System.Func<T, bool> filter = null) where T : Component
    {
        var hits = Physics.OverlapSphere(transform.position, range);
        T nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var hit in hits)
        {
            var c = hit.GetComponent<T>();
            if (c == null) continue;
            if (filter != null && !filter(c)) continue;
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < nearestDist) { nearestDist = d; nearest = c; }
        }
        return nearest;
    }

    public void DepositWood(int amount) { CarryWood -= amount; woodWasFull = false; }
    public void DepositMeat(int amount) { CarryMeat -= amount; meatWasFull = false; }

    /// <summary>Collect meat into carry (e.g. from an ArcherTower's stash). Returns how much actually fit.</summary>
    public int AddCarryMeat(int amount)
    {
        int space = carryCap - CarryMeat;
        int added = Mathf.Min(amount, space);
        CarryMeat += added;
        return added;
    }
}
