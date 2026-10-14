using UnityEngine;

[CreateAssetMenu(fileName = "NewResource", menuName = "浮岛开拓者/资源数据")]
public class ResourceData : ScriptableObject
{
    public string resourceName = "新资源";
    public string dropName;           // 掉落物名称（默认和资源名相同）
    public Sprite worldSprite;        // 世界中的精灵
    public Sprite dropSprite;         // 掉落物精灵
    public Color fallbackColor = Color.gray;
    public float maxHealth = 30f;
    public int goldReward = 1;
    public string[] spawnOnIslands; // 在哪些岛上生成
    public float spawnWeight = 1f;    // 权重（越大越常见）
}