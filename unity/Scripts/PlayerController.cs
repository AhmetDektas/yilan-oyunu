using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tap-to-move player character. Movement/pathfinding pattern adapted
/// from KaganAyten's Click-MoveSource repo (raycast to ground ->
/// NavMeshAgent.SetDestination -> animator "IsWalking" sync) so the
/// character actually paths around trees/the hotel instead of walking
/// through them. Chop/attack/carry logic layers on top for our
/// gathering loop.
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
    [Header("Toplama")]
    public float chopRange = 1.2f;
    public float attackRange = 1.2f;
    public float chopCooldown = 1.1f;
    public float attackCooldown = 0.45f;
    public int attackDamage = 30;
    public int carryCap = 20;

    [Header("Referanslar")]
    public Camera cam;
    public Animator animator;
    public string isWalkingParam = "IsWalking";
    public LayerMask tapMask = ~0;

    public int CarryWood { get; private set; }
    public int CarryMeat { get; private set; }

    NavMeshAgent agent;
    ResourceTree pendingChop;
    Animal pendingAttack;
    float lastChopAt = -10f;
    float lastAttackAt = -10f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        HandleInput();
        ChaseAttackTarget();
        SyncAnimator();
        TryChop();
        TryAttack();
    }

    void HandleInput()
    {
        bool tapped = Input.GetMouseButtonDown(0);
        if (!tapped && Input.touchCount > 0) tapped = Input.GetTouch(0).phase == TouchPhase.Began;
        if (!tapped) return;

        Vector3 screenPos = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, tapMask)) return;

        var tree = hit.collider.GetComponent<ResourceTree>();
        if (tree != null) { pendingChop = tree; pendingAttack = null; agent.SetDestination(tree.transform.position); return; }

        var animal = hit.collider.GetComponent<Animal>();
        if (animal != null && !animal.IsDead) { pendingAttack = animal; pendingChop = null; agent.SetDestination(animal.transform.position); return; }

        pendingChop = null;
        pendingAttack = null;
        if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            agent.SetDestination(navHit.position);
    }

    void ChaseAttackTarget()
    {
        if (pendingAttack != null && !pendingAttack.IsDead)
            agent.SetDestination(pendingAttack.transform.position);
    }

    void SyncAnimator()
    {
        if (animator != null) animator.SetBool(isWalkingParam, agent.velocity.sqrMagnitude > 0.01f);
    }

    void TryChop()
    {
        if (pendingChop == null) return;
        if (Vector3.Distance(transform.position, pendingChop.transform.position) > chopRange) return;
        if (Time.time - lastChopAt < chopCooldown) return;
        if (CarryWood >= carryCap) return;

        lastChopAt = Time.time;
        int amount = Mathf.Min(Random.Range(3, 7), carryCap - CarryWood);
        CarryWood += amount;
        GameManager.Instance.State.totalWoodChopped += amount;
        GameManager.Instance.CheckAchievements();
    }

    void TryAttack()
    {
        if (pendingAttack == null || pendingAttack.IsDead) { pendingAttack = null; return; }
        if (Vector3.Distance(transform.position, pendingAttack.transform.position) > attackRange) return;
        if (Time.time - lastAttackAt < attackCooldown) return;

        lastAttackAt = Time.time;
        bool died = pendingAttack.TakeDamage(attackDamage);
        if (died)
        {
            int amount = Mathf.Min(Random.Range(4, 9), carryCap - CarryMeat);
            CarryMeat += amount;
            GameManager.Instance.State.totalMeatCollected += amount;
            GameManager.Instance.CheckAchievements();
            pendingAttack = null;
        }
    }

    public void DepositWood(int amount) => CarryWood -= amount;
    public void DepositMeat(int amount) => CarryMeat -= amount;
}
