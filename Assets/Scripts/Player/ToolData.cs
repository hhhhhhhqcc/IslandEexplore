using UnityEngine;

[CreateAssetMenu(fileName = "NewTool", menuName = "浮岛开拓者/工具数据")]
public class ToolData : ScriptableObject
{
    public string toolName = "空手";
    public Sprite icon;
    public float damage = 0f;
    public float cooldown = 0.5f;
    public bool canAttack = false;

    [Header("目标限制")]
    public bool canHitEnemies = false;       // 能否打怪物
    public string[] targetResources;         // 能打的资源名（填"树木""石头""纤维"等）
    [Tooltip("留空 = 可以打所有资源")]
    public bool allowAnyResource = false;
}