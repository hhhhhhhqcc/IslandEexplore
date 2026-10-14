using System.Collections;
using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    public ResourceData resourceData;

    [Header("资源属性")]
    public string resourceName = "树木";     // 显示名字
    public string dropItemName = "木材";     // 掉落物名称
    public int dropAmount = 2;              // 掉落数量
    public float maxHealth = 30f;           // 需要敲多少下（每下10伤害=敲3下）
    public float respawnTime = 15f;         // 敲碎后多少秒重生

    [Header("视觉效果")]
    public Color hitColor = Color.white;    // 被敲时的闪白颜色
    public float hitDuration = 0.1f;        // 闪白持续时间
    public float shakeAmount = 0.1f;        // 抖动幅度

    private float currentHealth;
    private SpriteRenderer sr;
    private Color originalColor;
    private Vector3 originalPosition;
    private BoxCollider2D col;
    private bool isDead;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
        originalColor = sr.color;
        originalPosition = transform.position;
    }

    private void Start()
    {
        if (resourceData != null)
            currentHealth = resourceData.maxHealth;
        else
            currentHealth = maxHealth;  // 兜底
    }
    
    /// <summary>
    /// 受到一次攻击（由 PlayerController 调用）
    /// </summary>
    public void TakeHit(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // 播放反馈
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
        StartCoroutine(ShakeRoutine());

        if (currentHealth <= 0)
        {
            DestroyNode();
        }
    }

    /// <summary>
    /// 闪白效果
    /// </summary>
    private IEnumerator FlashRoutine()
    {
        sr.color = hitColor;
        yield return new WaitForSeconds(hitDuration);
        sr.color = originalColor;
    }

    /// <summary>
    /// 抖动效果
    /// </summary>
    private IEnumerator ShakeRoutine()
    {
        float timer = 0f;
        while (timer < hitDuration)
        {
            float offsetX = Random.Range(-shakeAmount, shakeAmount);
            float offsetY = Random.Range(-shakeAmount, shakeAmount);
            transform.position = originalPosition + new Vector3(offsetX, offsetY, 0);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
    }

    /// <summary>
    /// 被敲碎
    /// </summary>
    private void DestroyNode()
    {
        isDead = true;

        if (resourceData != null)
        {
            FindObjectOfType<InventoryManager>()?.AddGold(resourceData.goldReward);

            for (int i = 0; i < dropAmount; i++)
            {
                Vector3 dropPos = transform.position + (Vector3)(Random.insideUnitCircle * 1f);
                DroppedItem.Spawn(resourceData, dropPos);
            }
        }

        sr.enabled = false;
        col.enabled = false;
        StartCoroutine(RespawnRoutine());
    }

    /// <summary>
    /// 等待重生
    /// </summary>
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
        isDead = false;
        currentHealth = maxHealth;
        sr.enabled = true;
        col.enabled = true;
        sr.color = originalColor;
        transform.position = originalPosition;
    }
}