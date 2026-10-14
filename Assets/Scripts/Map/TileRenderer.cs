using System.Collections.Generic;
using UnityEngine;

public class TileRenderer : MonoBehaviour
{
    public SpriteDB spriteDB;
    private TileManager tm;
    private Transform parent;

    private void Start()
    {
       tm = TileManager.Instance;
        parent = new GameObject("TilesContainer").transform;
        //maxG = (tm.worldRadius * 2 + 1) * tm.UnitSpan + tm.islandGap;

        // 相机背景改为蓝色 = 海洋
        Camera.main.backgroundColor = new Color(0.1f, 0.15f, 0.45f);
    }

    private void Update()
    {
        DrawOrphans();
    }

   

    private void DrawOrphans()
    {
        for (int ix = -tm.worldRadius; ix <= tm.worldRadius; ix++)
        {
            for (int iy = -tm.worldRadius; iy <= tm.worldRadius; iy++)
            {
                Vector2Int iCoord = new Vector2Int(ix, iy);
                if (!tm.IsRevealed(iCoord)) continue;

                string iName = $"Island_{ix}_{iy}";
                Transform iT = parent.Find(iName);
                if (iT == null)
                {
                    GameObject go = new GameObject(iName);
                    go.transform.SetParent(parent);
                    go.transform.position = tm.IslandToWorld(iCoord);
                    iT = go.transform;
                }

                if (iT.childCount >= tm.islandSize * tm.islandSize) continue;
                DrawIslandCells(iCoord, iT);
            }
        }
    }

    private void DrawIslandCells(Vector2Int iCoord, Transform iT)
    {
        IslandData island = tm.GetIsland(iCoord);
        if (island == null) return;

        int n = tm.islandSize;
        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                string cn = $"C_{x}_{y}";
                if (iT.Find(cn) != null) continue;

                Vector2 pos = tm.CellToWorld(iCoord, x, y);
                GameObject cell = new GameObject(cn);
                cell.transform.SetParent(iT);
                cell.transform.position = pos;

                SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100) - 1000;

                if (!tm.IsUnlocked(iCoord))
                {
                    sr.sprite = MakeTex(new Color(0.08f, 0.08f, 0.12f, 0.6f));
                    sr.color = Color.white;
                }
                else
                {
                    sr.sprite = GetTerrainSprite(island.type, x, y);
                }
            }
        }
    }
    Sprite GetTerrainSprite(TileType type, int x, int y)
    {
        if (spriteDB == null) return null;
        Sprite[] pool = null;
        switch (type)
        {
            case TileType.Forest:    pool = spriteDB.forestTiles; break;
            case TileType.Grassland: pool = spriteDB.grasslandTiles; break;
            case TileType.Desert:    pool = spriteDB.desertTiles; break;
            case TileType.Snowfield: pool = spriteDB.snowfieldTiles; break;
            case TileType.Volcano:   pool = spriteDB.volcanoTiles; break;
        }
        if (pool == null || pool.Length == 0) return null;
        int i = Mathf.Abs(x * 73856093 ^ y * 19349663) % pool.Length;
        return pool[i];
    }
    private Color GetTerrainColor(TileType type)//原设定
    {
        switch (type)
        {
            case TileType.Forest: return new Color(0.22f, 0.55f, 0.15f);
            case TileType.Grassland: return new Color(0.4f, 0.75f, 0.3f);
            case TileType.Desert: return new Color(0.9f, 0.82f, 0.5f);
            case TileType.Snowfield: return new Color(0.85f, 0.92f, 0.98f);
            case TileType.Volcano: return new Color(0.55f, 0.2f, 0.15f);
            default: return Color.gray;
        }
    }

    private Sprite MakeTex(Color c)
    {
        int s = 8;
        Texture2D t = new Texture2D(s, s);
        for (int i = 0; i < s * s; i++)
        {
            int x = i % s, y = i / s;
            t.SetPixel(x, y, (x < 1 || x >= s - 1 || y < 1 || y >= s - 1) ? c * 0.75f : c);
        }
        t.filterMode = FilterMode.Point;
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    }
}