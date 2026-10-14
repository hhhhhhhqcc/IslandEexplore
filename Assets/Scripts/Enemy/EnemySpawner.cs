using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public void SpawnOnIsland(Vector2Int islandCoord)
    {
        TileManager tm = TileManager.Instance;
        IslandData island = tm.GetIsland(islandCoord);
        if (island == null) return;

        int count = Random.Range(3, 7);
        int n = tm.islandSize;

        for (int i = 0; i < count; i++)
        {
            int cx, cy;
            int tries = 0;
            do
            {
                cx = Random.Range(1, n - 1);
                cy = Random.Range(1, n - 1);
                tries++;
            }
            while (tries < 50 &&
                   (tm.GetCellData(islandCoord, cx, cy).hasResource ||
                    tm.GetCellData(islandCoord, cx, cy).hasBuilding));

            if (tries >= 50) continue;

            Vector2 pos = tm.CellToWorld(islandCoord, cx, cy);
            SpawnEnemy(pos, island.type);
        }
    }

    private void SpawnEnemy(Vector2 pos, TileType islandType)
    {
        GameObject obj = new GameObject("Monster");
        obj.transform.position = pos;

        EnemyAI ai = obj.AddComponent<EnemyAI>();

        switch (islandType)
        {
            case TileType.Grassland:
                ai.InitVisual(new Color(0.3f, 0.8f, 0.3f), "史莱姆");
                ai.maxHP = 30f;
                ai.damage = 5f;
                ai.drops = new string[] { "石头", "纤维" };
                ai.dropGold = 2;
                break;
            case TileType.Forest:
                ai.InitVisual(new Color(0.8f, 0.5f, 0.2f), "树精");
                ai.maxHP = 50f;
                ai.damage = 10f;
                ai.drops = new string[] { "木材", "木材" };
                ai.dropGold = 4;
                break;
            case TileType.Desert:
                ai.InitVisual(new Color(0.95f, 0.8f, 0.3f), "沙漠蝎");
                ai.maxHP = 40f;
                ai.damage = 8f;
                ai.drops = new string[] { "铜矿石" };
                ai.dropGold = 5;
                break;
            case TileType.Snowfield:
                ai.InitVisual(new Color(0.7f, 0.8f, 0.9f), "冰元素");
                ai.maxHP = 60f;
                ai.damage = 12f;
                ai.drops = new string[] { "铁矿石" };
                ai.dropGold = 6;
                break;
            case TileType.Volcano:
                ai.InitVisual(new Color(0.9f, 0.3f, 0.1f), "熔岩怪");
                ai.maxHP = 80f;
                ai.damage = 15f;
                ai.drops = new string[] { "金矿石", "煤炭" };
                ai.dropGold = 8;
                break;
        }
    }
}