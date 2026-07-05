using UnityEngine;

/// <summary>
/// Tap-to-move player character for the live gathering map. Tapping empty
/// ground walks there; tapping a ResourceTree walks over and chops it
/// (wood accumulates as carry); tapping an Animal chases and attacks it
/// with the axe (meat accumulates as carry on kill). Walk into a DropZone
/// to empty the matching carry into GameManager's stockpile.
///
/// Requires: a Collider2D on this GameObject (for DropZone trigger
/// detection) and a Rigidbody2D set to Kinematic (2D physics needs at
/// least one Rigidbody2D in a trigger pair).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Hareket")]
    public float moveSpeed = 3f;
    public Camera cam;

    [Header("Toplama")]
    public float chopRange = 0.9f;
    public float attackRange = 0.9f;
    public float chopCooldown = 1.1f;
    public float attackCooldown = 0.45f;
    public int attackDamage = 30;
    public int carryCap = 20;

    public int CarryWood { get; private set; }
    public int CarryMeat { get; private set; }

    Vector2 target;
    ResourceTree pendingChop;
    Animal pendingAttack;
    float lastChopAt = -10f;
    float lastAttackAt = -10f;

    void Start()
    {
        target = transform.position;
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        HandleInput();
        MoveTowardTarget();
        TryChop();
        TryAttack();
    }

    void HandleInput()
    {
        bool tapped = Input.GetMouseButtonDown(0);
        if (!tapped && Input.touchCount > 0) tapped = Input.GetTouch(0).phase == TouchPhase.Began;
        if (!tapped) return;

        Vector3 screenPos = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
        Vector2 world = cam.ScreenToWorldPoint(screenPos);
        var hit = Physics2D.OverlapPoint(world);

        if (hit != null)
        {
            var tree = hit.GetComponent<ResourceTree>();
            if (tree != null) { pendingChop = tree; pendingAttack = null; target = tree.transform.position; return; }

            var animal = hit.GetComponent<Animal>();
            if (animal != null && !animal.IsDead) { pendingAttack = animal; pendingChop = null; target = animal.transform.position; return; }
        }

        pendingChop = null;
        pendingAttack = null;
        target = world;
    }

    void MoveTowardTarget()
    {
        if (pendingAttack != null && !pendingAttack.IsDead) target = pendingAttack.transform.position;

        Vector2 pos = transform.position;
        if (Vector2.Distance(pos, target) > 0.05f)
            transform.position = Vector2.MoveTowards(pos, target, moveSpeed * Time.deltaTime);
    }

    void TryChop()
    {
        if (pendingChop == null) return;
        if (Vector2.Distance(transform.position, pendingChop.transform.position) > chopRange) return;
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
        if (Vector2.Distance(transform.position, pendingAttack.transform.position) > attackRange) return;
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
