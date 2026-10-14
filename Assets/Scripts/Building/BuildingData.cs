using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "浮岛开拓者/建筑数据")]
public class BuildingData : ScriptableObject
{
    public string buildingName = "新建筑";
    public Color buildingColor = Color.gray;
    public Vector2Int gridSize = new Vector2Int(2, 2);

    [Header("建造消耗")]
    public string requiredItem = "石头";
    public int requiredAmount = 5;

    [Header("替代材料（有其一即可建造）")]
    public string altRequiredItem = "";
    public int altRequiredAmount = 0;

    [Header("特殊")]
    public bool isBridge = false;
    
    [Header("功能")]
    public string description = "";
}