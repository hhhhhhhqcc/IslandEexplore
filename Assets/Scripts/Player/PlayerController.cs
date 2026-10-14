using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("动画")]
    public Sprite idleSprite;
    public Sprite[] walkSprites;
    public float animSpeed = 0.15f;

    [Header("工具")]
    public ToolData[] tools;

    [Header("采集设置")]
    public float hitRange = 2.5f;
    public LayerMask resourceLayer;

    [Header("移动速度")]
    public float moveSpeed = 5f;

    [Header("战斗")]
    public float maxHP = 100f;
    public LayerMask enemyLayer;

    private float currentHP;
    private float hitTimer;
    private int currentToolIndex = -1;
    private ToolData currentTool;

    private Rigidbody2D rb;
    private Vector3 lastValidPos;
    private Vector2 movement;

    private SpriteRenderer sr;
    private int animFrame;
    private float animTimer;

    public Transform handPivot;     // 武器旋转锚点（Player 子物体）
    private SpriteRenderer toolHandSR;
    private float swingAngle;
    private bool isSwinging;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        lastValidPos = transform.position;
        currentHP = maxHP;
        sr = GetComponent<SpriteRenderer>();

        if (handPivot == null)
        {
            GameObject pivot = new GameObject("HandPivot");
            pivot.transform.SetParent(transform);
            pivot.transform.localPosition = new Vector3(0.5f, -0.2f, 0);
            handPivot = pivot.transform;
        }

        GameObject toolObj = new GameObject("ToolSprite");
        toolObj.transform.SetParent(handPivot);
        toolObj.transform.localPosition = Vector3.zero;
        toolObj.transform.localScale = Vector3.one * 0.6f;
        toolHandSR = toolObj.AddComponent<SpriteRenderer>();
        toolHandSR.sortingOrder = 30;
        toolHandSR.sprite = null;
    }

    void Update()
    {
        // Menus freeze gameplay even though Unity continues calling Update at timeScale 0.
        if (GameMenuController.BlocksGameplayInput)
        {
            movement = Vector2.zero;
            rb.velocity = Vector2.zero;
            return;
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        movement = new Vector2(moveX, moveY).normalized;

        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) + 20;

        // ===== 工具切换 =====
        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (i < tools.Length && tools[i] != null && tools[i].canAttack)
                {
                    currentToolIndex = i;
                    currentTool = tools[i];
                }
                else
                {
                    currentToolIndex = -1;
                    currentTool = null;
                }
                Debug.Log(currentTool != null ? $"装备: {currentTool.toolName}" : "空手");
            }
        }

        // ===== 手上武器朝向鼠标 =====
        Vector3 mouseW = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseW.z = 0;
        Vector2 dir = (mouseW - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (isSwinging)
        {
            swingAngle += Time.deltaTime * 720f;
            if (swingAngle > 90f)
            {
                swingAngle = 0f;
                isSwinging = false;
            }
            handPivot.rotation = Quaternion.Euler(0, 0, angle - 45f + swingAngle);
        }
        else
        {
            handPivot.rotation = Quaternion.Euler(0, 0, angle - 45f);
        }

        // 更新手上武器精灵
        if (currentTool != null && currentTool.icon != null)
            toolHandSR.sprite = currentTool.icon;
        else
            toolHandSR.sprite = null;        

        // ===== 攻击 =====
        hitTimer -= Time.deltaTime;
        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetKeyDown(KeyCode.Space) || (Input.GetMouseButtonDown(0) && !pointerOverUI))
        {
            TryHit();
        }

        // ===== 动画 =====
        if (walkSprites != null && walkSprites.Length > 0)
        {
            if (movement.magnitude > 0.1f)
            {
                animTimer += Time.deltaTime;
                if (animTimer >= animSpeed)
                {
                    animTimer = 0f;
                    animFrame = (animFrame + 1) % walkSprites.Length;
                    sr.sprite = walkSprites[animFrame];
                }
            }
            else
            {
                animTimer = 0f;
                animFrame = 0;
                sr.sprite = idleSprite;
            }
        }
    }

    private void FixedUpdate()
    {
        rb.velocity = GameMenuController.BlocksGameplayInput ? Vector2.zero : movement * moveSpeed;
    }

    private void TryHit()
    {
        if (currentTool == null)
        {
            Debug.Log("空手无法攻击，按 1-4 装备工具");
            return;
        }
        if (hitTimer > 0) return;
        hitTimer = currentTool.cooldown;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        Vector2 direction = (mouseWorld - transform.position).normalized;
        Vector2 origin = transform.position;

        int mask = resourceLayer | enemyLayer;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, hitRange, mask);

        if (hit.collider == null) return;

        ResourceNode node = hit.collider.GetComponent<ResourceNode>();
        EnemyAI enemy = hit.collider.GetComponent<EnemyAI>();

        if (node != null)
        {
            if (!currentTool.allowAnyResource)
            {
                string resName = node.resourceData?.resourceName;
                bool allowed = false;
                if (currentTool.targetResources != null)
                {
                    foreach (string t in currentTool.targetResources)
                        if (t == resName) { allowed = true; break; }
                }
                if (!allowed)
                {
                    Debug.Log($"{currentTool.toolName}无法采集 {resName}");
                    return;
                }
            }

            node.TakeHit(currentTool.damage);
            isSwinging = true;
            swingAngle = 0f;
        }

        if (enemy != null)
        {
            if (!currentTool.canHitEnemies)
            {
                Debug.Log($"{currentTool.toolName}无法攻击怪物");
                return;
            }
            enemy.TakeHit(currentTool.damage);
            isSwinging = true;
            swingAngle = 0f;
        }

        Debug.DrawRay(origin, direction * hitRange, Color.red, 0.2f);
    }
    
    private void LateUpdate()
    {
        var gCell = TileManager.Instance?.WorldToGlobalCell(transform.position);
        bool valid = gCell != null && TileManager.Instance.IsWalkable(gCell.Value);

        if (valid)
        {
            lastValidPos = transform.position;
        }
        else
        {
            transform.position = lastValidPos;
            rb.velocity = Vector2.zero;
        }
    }

    public float GetHPPercent() => currentHP / maxHP;

    public void TakeDamage(float dmg)
    {
        currentHP -= dmg;
        if (currentHP <= 0)
        {
            Debug.Log("玩家死亡！");
            currentHP = maxHP;
            transform.position = Vector3.zero;
        }
    }

    public ToolData GetCurrentTool() => currentTool;
    public string GetToolName() => currentTool != null ? currentTool.toolName : "空手";
}