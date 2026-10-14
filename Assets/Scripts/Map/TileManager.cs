using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public SpriteDB spriteDB;
    public ResourceDatabase resourceDB;
    public static TileManager Instance { get; private set; }

    [Header("尺寸")]
    public int islandSize = 25;
    public int islandGap = 3;
    public int worldRadius = 3;
    public float cellSize = 1f;

    [Header("经济")]
    public int baseTileCost = 0;
    public float costMultiplier = 1.5f;

    private Dictionary<Vector2Int, IslandData> allIslands = new Dictionary<Vector2Int, IslandData>();
    private HashSet<Vector2Int> unlockedIslands = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> revealedIslands = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> bridgeCells = new HashSet<Vector2Int>();

    public int unlockedCount => unlockedIslands.Count;
    public int UnitSpan => islandSize + islandGap;

    private void Awake() => Instance = this;

    private void Start()
    {
        GenerateAllIslands();
        UnlockIsland(Vector2Int.zero);
    }

    private void GenerateAllIslands()
    {
        for (int x = -worldRadius; x <= worldRadius; x++)
            for (int y = -worldRadius; y <= worldRadius; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                float noise = Mathf.PerlinNoise(x * 0.15f + 100, y * 0.15f + 100);
                int d = Mathf.Abs(x) + Mathf.Abs(y);

                TileType type;
                if (d == 0) type = TileType.Grassland;
                else if (d <= 2) type = noise > 0.5f ? TileType.Forest : TileType.Grassland;
                else if (d <= 4) type = noise > 0.6f ? TileType.Desert : TileType.Forest;
                 else if (noise > 0.75f) type = TileType.Volcano;
                else type = noise > 0.5f ? TileType.Snowfield : TileType.Desert;

                allIslands[coord] = new IslandData(coord, type, islandSize);
            }
    }

    // ===== 岛屿 ↔ 全球单元格 转换 =====

    public Vector2Int IslandToGlobal(Vector2Int islandCoord)
    {
        return islandCoord * UnitSpan;
    }

    public Vector2 GlobalCellToWorld(Vector2Int gCell)
    {
        return new Vector2(gCell.x + 0.5f, gCell.y + 0.5f);
    }

    public Vector2 IslandToWorld(Vector2Int islandCoord)
    {
        return new Vector2(islandCoord.x * UnitSpan, islandCoord.y * UnitSpan);
    }

    public Vector2 CellToWorld(Vector2Int islandCoord, int cx, int cy)
    {
        return IslandToWorld(islandCoord) + new Vector2(cx + 0.5f, cy + 0.5f);
    }

    public Vector2Int? WorldToGlobalCell(Vector3 worldPos)
    {
        int gx = Mathf.FloorToInt(worldPos.x);
        int gy = Mathf.FloorToInt(worldPos.y);
        return new Vector2Int(gx, gy);
    }

    public (Vector2Int island, int cx, int cy)? GlobalCellToIsland(Vector2Int gCell)
    {
        int ix = Mathf.FloorToInt((float)gCell.x / UnitSpan);
        int iy = Mathf.FloorToInt((float)gCell.y / UnitSpan);

        int localX = gCell.x - ix * UnitSpan;
        int localY = gCell.y - iy * UnitSpan;

        if (localX < 0 || localX >= islandSize || localY < 0 || localY >= islandSize)
            return null;

        Vector2Int iCoord = new Vector2Int(ix, iy);
        if (!allIslands.ContainsKey(iCoord)) return null;
        return (iCoord, localX, localY);
    }

    public (Vector2Int island, int cx, int cy)? WorldToCell(Vector3 worldPos)
    {
        var gCell = WorldToGlobalCell(worldPos);
        if (gCell == null) return null;
        return GlobalCellToIsland(gCell.Value);
    }

    // ===== 岛屿解锁 =====

    public bool CanUnlock(Vector2Int coord)
    {
        if (unlockedIslands.Contains(coord)) return false;
        if (!allIslands.ContainsKey(coord)) return false;
        Vector2Int[] n = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var d in n)
            if (unlockedIslands.Contains(coord + d)) return true;
        return false;
    }

    public int GetCost()
    {
        return Mathf.FloorToInt(baseTileCost * Mathf.Pow(costMultiplier, unlockedIslands.Count));
    }

    public bool UnlockIsland(Vector2Int coord)
    {
        if (!CanUnlock(coord) && coord != Vector2Int.zero) return false;

        unlockedIslands.Add(coord);
        RevealIsland(coord);

        IslandData island = allIslands[coord];
        for (int x = 0; x < islandSize; x++)
            for (int y = 0; y < islandSize; y++)
                island.cells[x, y].isWalkable = true;

        island.SpawnResources(this, coord);
        
        EnemySpawner es = FindObjectOfType<EnemySpawner>();
        if (es != null) es.SpawnOnIsland(coord);

        return true;
    }

    private void RevealIsland(Vector2Int coord)
    {
        revealedIslands.Add(coord);
        Vector2Int[] n = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var d in n)
            if (allIslands.ContainsKey(coord + d))
                revealedIslands.Add(coord + d);
    }

    // ===== 行走判断 =====

    public bool IsWalkable(Vector2Int gCell)
    {
        var island = GlobalCellToIsland(gCell);
        if (island != null && IsUnlocked(island.Value.island))
            return true;
        return bridgeCells.Contains(gCell);
    }

    public void AddBridge(Vector2Int gCell)
    {
        bridgeCells.Add(gCell);
    }

    // ===== 查询 =====

    public IslandData GetIsland(Vector2Int coord)
    {
        allIslands.TryGetValue(coord, out IslandData d);
        return d;
    }

    public bool IsUnlocked(Vector2Int coord) => unlockedIslands.Contains(coord);
    public bool IsRevealed(Vector2Int coord) => revealedIslands.Contains(coord);
    public bool IsIsland(Vector2Int coord) => allIslands.ContainsKey(coord);

    public CellData GetCellData(Vector2Int islandCoord, int cx, int cy)
    {
        var island = GetIsland(islandCoord);
        if (island == null) return null;
        if (cx < 0 || cx >= islandSize || cy < 0 || cy >= islandSize) return null;
        return island.cells[cx, cy];
    }
}

[System.Serializable]
public class IslandData
{
    public Vector2Int coord;
    public TileType type;
    public CellData[,] cells;

    public IslandData(Vector2Int coord, TileType type, int size)
    {
        this.coord = coord;
        this.type = type;
        cells = new CellData[size, size];
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                cells[x, y] = new CellData();
    }

    public void SpawnResources(TileManager tm, Vector2Int islandCoord)
    {
        int size = cells.GetLength(0);
        int count = Random.Range(size * size / 10, size * size / 7);

        for (int i = 0; i < count; i++)
        {
            int cx = Random.Range(0, size);
            int cy = Random.Range(0, size);
            if (cells[cx, cy].hasResource) continue;

            ResourceData res = tm.resourceDB?.GetRandomForIsland(type.ToString());
            if (res == null) continue;

            cells[cx, cy].hasResource = true;
            cells[cx, cy].resourceName = res.resourceName;

            Vector2 pos = tm.CellToWorld(islandCoord, cx, cy);
            CreateResourceObj(tm.spriteDB, res, pos);
        }
    }

    private void CreateResourceObj(SpriteDB spriteDB, ResourceData data, Vector2 pos)
    {
        GameObject obj = new GameObject(data.resourceName);
        obj.transform.position = pos;
        obj.layer = LayerMask.NameToLayer("Resource");

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = data.worldSprite ?? MakeTex(data.fallbackColor);
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100)+1;

        float spriteHeight = sr.sprite != null ? sr.sprite.bounds.size.y : 1f;
        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        if (spriteHeight > 1.5f)
        {
            col.offset = new Vector2(0, 0.25f);
            col.size = new Vector2(0.8f, 0.5f);
        }
        else
        {
            col.size = Vector2.one;
        }

        ResourceNode node = obj.AddComponent<ResourceNode>();
        node.resourceData = data;
    }

    private Sprite MakeTex(Color c)
    {
        int s = 16;
        Texture2D t = new Texture2D(s, s);
        for (int i = 0; i < s * s; i++)
        {
            int x = i % s, y = i / s;
            t.SetPixel(x, y, (x < 1 || x >= s - 1 || y < 1 || y >= s - 1) ? c * 0.7f : c);
        }
        t.filterMode = FilterMode.Point;
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16);
    }
}

[System.Serializable]
public class CellData
{
    public bool hasResource;
    public string resourceName;
    public bool hasBuilding;
    public bool isWalkable;
}

public enum TileType
{
    Grassland,
    Forest,
    Desert,
    Snowfield,
    Volcano
}