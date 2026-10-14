using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public string itemName;                     // 物品名称
    public float pickupRange = 1.5f;            // 直接拾取距离
    public float magnetRange = 4f;              // 磁吸触发距离
    public float magnetSpeed = 8f;              // 磁吸飞行速度
    public float lifetime = 30f;                // 存活时间（超时消失）

    private Rigidbody2D rb;
    private float age;
    private Transform player;
    private bool isPickable;                    // 飞出后冷却一小段时间才能拾取

    /// <summary>
    /// 静态工厂方法：在指定位置生成一个掉落物
    /// </summary>
        public static DroppedItem Spawn(ResourceData data, Vector3 position)
    {
        GameObject obj = new GameObject($"Dropped_{data.resourceName}");
        obj.transform.position = position;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = data.dropSprite ?? CreateSmallSquare(data.fallbackColor);
        obj.transform.localScale = Vector3.one * 0.4f;
        sr.sortingOrder = Mathf.RoundToInt(position.y * 100) + 10;

        CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1f;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        DroppedItem item = obj.AddComponent<DroppedItem>();
        item.itemName = data.resourceName;
        item.rb = rb;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        rb.AddForce(randomDir * Random.Range(2f, 5f), ForceMode2D.Impulse);

        return item;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        // 0.3 秒后才能被拾取（防止刚飞出就被吸入）
        Invoke(nameof(MakePickable), 0.3f);
    }

    private void MakePickable()
    {
        isPickable = true;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age > lifetime)
        {
            Destroy(gameObject);
        }

        // 快消失时闪烁
        if (age > lifetime * 0.7f)
        {
            float alpha = Mathf.PingPong(Time.time * 5f, 1f);
            GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, alpha);
        }
    }

    private void FixedUpdate()
    {
        if (player == null || !isPickable) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // 背包满了：不吸
        if (!WouldFitInventory())
        {
            rb.velocity *= 0.95f;
            return;
        }

        if (dist <= magnetRange)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = direction * magnetSpeed;
        }
        else if (rb.velocity.magnitude > 0.1f)
        {
            rb.velocity *= 0.95f;
        }

        if (dist <= pickupRange)
        {
            PickUp();
        }
    }

    private bool WouldFitInventory()
    {
        InventoryManager inv = FindObjectOfType<InventoryManager>();
        return inv != null && inv.CanAddItem(itemName);
    }

   private void PickUp()
    {
        InventoryManager inv = FindObjectOfType<InventoryManager>();
        if (inv != null)
        {
            if (inv.CanAddItem(itemName))
            {
                inv.AddItem(itemName, 1);
                Destroy(gameObject);
            }
        }
    }

    // ===== 工具方法 =====

    private static Sprite CreateSmallSquare(Color color)
    {
        int size = 8;
        Texture2D tex = new Texture2D(size, size);
        for (int i = 0; i < size * size; i++)
            tex.SetPixel(i % size, i / size, color);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 8);
    }

    private static Color GetItemColor(string name)
    {
        switch (name)
        {
            case "木材":   return new Color(0.55f, 0.35f, 0.15f);
            case "石头":   return new Color(0.5f, 0.5f, 0.55f);
            case "铜矿石": return new Color(0.85f, 0.5f, 0.25f);
            case "铁矿石": return new Color(0.6f, 0.6f, 0.65f);
            case "金矿石": return new Color(1f, 0.85f, 0.2f);
            case "纤维":   return new Color(0.3f, 0.7f, 0.2f);
            case "煤炭":   return new Color(0.2f, 0.18f, 0.18f);
            default:       return Color.magenta;
        }
    }
}