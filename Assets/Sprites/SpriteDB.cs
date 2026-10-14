using UnityEngine;

[CreateAssetMenu(fileName = "SpriteDB", menuName = "浮岛开拓者/精灵数据库")]
public class SpriteDB : ScriptableObject
{
    // 地形
    public Sprite[] grasslandTiles;
    public Sprite[] forestTiles;
    public Sprite[] desertTiles;
    public Sprite[] snowfieldTiles;
    public Sprite[] volcanoTiles;

    // 资源
    public Sprite tree;
    public Sprite stone;
    public Sprite fiber;
    public Sprite copperOre;
    public Sprite ironOre;
    public Sprite goldOre;
    public Sprite coal;

    // 建筑
    public Sprite furnace;
    public Sprite storageBox;
    public Sprite bridge;

    // 怪物
    public Sprite slime;
    public Sprite treant;
    public Sprite scorpion;
    public Sprite iceElemental;
    public Sprite lavaGolem;

    // 玩家
    public Sprite player;

    // 其他
    public Sprite oTile;

    // 根据名字查精灵
    public Sprite GetResourceSprite(string name)
    {
        switch (name)
        {
            case "树木": return tree;
            case "石头": return stone;
            case "纤维": return fiber;
            case "铜矿石": return copperOre;
            case "铁矿石": return ironOre;
            case "金矿石": return goldOre;
            case "煤炭": return coal;
            default: return oTile;
        }
    }



    public Sprite GetEnemySprite(string name)
    {
        switch (name)
        {
            case "史莱姆": return slime;
            case "树精": return treant;
            case "沙漠蝎": return scorpion;
            case "冰元素": return iceElemental;
            case "熔岩怪": return lavaGolem;
            default: return oTile;
        }
    }
        public Sprite GetBuildingSprite(string name)
    {
        switch (name)
        {
            case "熔炉":   return furnace;
            case "仓库":   return storageBox;
            case "桥":     return bridge;
            default: return oTile;
        }
    }
}