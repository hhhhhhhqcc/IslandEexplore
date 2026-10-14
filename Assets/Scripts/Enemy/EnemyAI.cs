using System.Collections;
using UnityEngine;

public enum EnemyState { Patrol, Chase, Attack, Dead }

public class EnemyAI : MonoBehaviour
{
    [Header("属性")]
    public string enemyName = "史莱姆";
    public Color enemyColor = Color.green;
    public float maxHP = 40f;
    public float damage = 5f;

    [Header("行为")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3f;
    public float detectRange = 5f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1f;
    public float patrolWaitTime = 2f;

    [Header("掉落")]
    public string[] drops = { "石头" };
    public int dropGold = 3;

    private float currentHP;
    private float attackTimer;
    private EnemyState state;
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 patrolTarget;
    private float waitTimer;
    private bool isWaiting;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * 0.8f;
        }
        col.isTrigger = false;

        gameObject.layer = LayerMask.NameToLayer("Enemy");
    }

    private void Start()
    {
        currentHP = maxHP;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        state = EnemyState.Patrol;
        SetColor(enemyColor);
        PickNewPatrolTarget();
    }

    private void Update()
    {
        if (GameMenuController.BlocksGameplayInput)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (state == EnemyState.Dead) return;
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100)+20;

        float distToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : 999f;

        // 状态切换
        if (distToPlayer <= attackRange)
            state = EnemyState.Attack;
        else if (distToPlayer <= detectRange)
            state = EnemyState.Chase;
        else
            state = EnemyState.Patrol;

        // 执行状态行为
        switch (state)
        {
            case EnemyState.Patrol: PatrolUpdate(); break;
            case EnemyState.Chase: ChaseUpdate(distToPlayer); break;
            case EnemyState.Attack: AttackUpdate(distToPlayer); break;
        }
    }

    // ===== 巡逻 =====
    private void PatrolUpdate()
    {
        SetColor(enemyColor);

        if (isWaiting)
        {
            rb.velocity = Vector2.zero;
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                PickNewPatrolTarget();
            }
            return;
        }

        Vector2 dir = (patrolTarget - (Vector2)transform.position).normalized;
        rb.velocity = dir * patrolSpeed;

        if (Vector2.Distance(transform.position, patrolTarget) < 0.3f)
        {
            rb.velocity = Vector2.zero;
            isWaiting = true;
            waitTimer = patrolWaitTime;
        }
    }

    private void PickNewPatrolTarget()
    {
        Vector2 origin = transform.position;
        Vector2Int global = TileManager.Instance?.WorldToGlobalCell(origin) ?? Vector2Int.zero;

        for (int tries = 0; tries < 10; tries++)
        {
            Vector2 candidate = origin + Random.insideUnitCircle * 3f;
            Vector2Int gc = TileManager.Instance?.WorldToGlobalCell(candidate) ?? Vector2Int.zero;
            if (TileManager.Instance != null && TileManager.Instance.IsWalkable(gc))
            {
                patrolTarget = candidate;
                return;
            }
        }
        patrolTarget = origin;
    }

    // ===== 追击 =====
    private void ChaseUpdate(float dist)
    {
        SetColor(Color.Lerp(enemyColor, Color.red, 0.3f));

        if (player == null) return;
        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = dir * chaseSpeed;
    }

    // ===== 攻击 =====
    private void AttackUpdate(float dist)
    {
        SetColor(Color.red);
        rb.velocity = Vector2.zero;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0 && player != null)
        {
            attackTimer = attackCooldown;
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null && Vector2.Distance(transform.position, player.position) <= attackRange + 0.5f)
            {
                pc.TakeDamage(damage);
            }
        }
    }

    // ===== 受伤 =====
    public void TakeHit(float dmg)
    {
        if (state == EnemyState.Dead) return;

        currentHP -= dmg;
        StartCoroutine(FlashWhite());

        // 被攻击后进入追击状态
        state = EnemyState.Chase;

        if (currentHP <= 0) Die();
    }

    private IEnumerator FlashWhite()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        SetColor(enemyColor);
    }

    // ===== 死亡 =====
    private void Die()
    {
        state = EnemyState.Dead;
        rb.velocity = Vector2.zero;

        foreach (string drop in drops)
        {
            Vector3 pos = transform.position + (Vector3)(Random.insideUnitCircle * 0.5f);
            ResourceDatabase db = FindObjectOfType<ResourceDatabase>();
            ResourceData rd = db?.GetByName(drop);
            if (rd != null)
                DroppedItem.Spawn(rd, pos);
        }

        // 掉落金币
        InventoryManager inv = FindObjectOfType<InventoryManager>();
        if (inv != null) inv.AddGold(dropGold);

        Destroy(gameObject);
    }

    private void SetColor(Color c)
    {
        if (sr != null) sr.color = c;
    }

    // 初始化时画贴图
    public void InitVisual(Color color, string name)
    {
        enemyColor = color;
        enemyName = name;
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        int s = 16;
        Texture2D t = new Texture2D(s, s);
        for (int i = 0; i < s * s; i++)
        {
            int x = i % s, y = i / s;
            t.SetPixel(x, y, (x < 2 || x >= s - 2 || y < 2 || y >= s - 2) ? color * 0.6f : color);
        }
        t.filterMode = FilterMode.Point;
        t.Apply();
        sr.sprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16);
    }
}