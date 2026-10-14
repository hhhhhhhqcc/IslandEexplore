using UnityEngine;

public class BuildingPlacer : MonoBehaviour
{   
    public SpriteDB spriteDB;

    [Header("可建造列表")]
    public BuildingData[] availableBuildings;

    [Header("预览颜色")]
    public Color validColor = new Color(0, 1, 0, 0.4f);
    public Color invalidColor = new Color(1, 0, 0, 0.4f);

    private bool isBuildingMode;
    private int selectedIndex;
    private GameObject previewObject;
    private SpriteRenderer previewRenderer;
    private InventoryManager inventory;
    private TileManager tm;
    private Transform buildingsParent;

    private void Start()
    {
        inventory = GetComponent<InventoryManager>();
        tm = TileManager.Instance;

        previewObject = new GameObject("BuildingPreview");
        previewRenderer = previewObject.AddComponent<SpriteRenderer>();

        previewRenderer.sortingOrder =30000;

        previewObject.SetActive(false);

        buildingsParent = new GameObject("Buildings").transform;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) ToggleBuildingMode();
        if (!isBuildingMode) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectBuilding(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectBuilding(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectBuilding(2);
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0) SelectBuilding(selectedIndex - 1);
        if (scroll < 0) SelectBuilding(selectedIndex + 1);

        UpdatePreview();

        if (Input.GetMouseButtonDown(0)) TryPlaceBuilding();
        if (Input.GetKeyDown(KeyCode.Escape)) ExitBuildingMode();
    }

    private void ToggleBuildingMode()
    {
        isBuildingMode = !isBuildingMode;
        previewObject.SetActive(isBuildingMode);
        if (isBuildingMode && availableBuildings.Length > 0) SelectBuilding(0);
    }

    private void SelectBuilding(int index)
    {
        if (availableBuildings.Length == 0) return;
        selectedIndex = (index + availableBuildings.Length) % availableBuildings.Length;
        Debug.Log($"选中: {availableBuildings[selectedIndex].buildingName}");
    }

    private void UpdatePreview()
    {
        if (availableBuildings.Length == 0 || selectedIndex >= availableBuildings.Length) return;
        BuildingData data = availableBuildings[selectedIndex];

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        var gCell = tm.WorldToGlobalCell(mouseWorld);
        Vector3 snapped = gCell != null
            ? (Vector3)tm.GlobalCellToWorld(gCell.Value)
            : new Vector3(Mathf.Round(mouseWorld.x), Mathf.Round(mouseWorld.y), 0);

        previewObject.transform.position = snapped;
        previewObject.transform.localScale = new Vector3(data.gridSize.x, data.gridSize.y, 1);
        previewRenderer.sprite = MakeTexSprite(data.gridSize, data.buildingColor);

        bool canPlace = gCell != null && CanPlaceBuilding(data, gCell.Value);
        previewRenderer.color = canPlace ? validColor : invalidColor;
    }

    private bool CanPlaceBuilding(BuildingData data, Vector2Int gCell)
    {
        if (data.isBridge)
            return CanPlaceBridge(gCell);

        // 非桥建筑：必须在岛屿内
        var island = tm.GlobalCellToIsland(gCell);
        if (island == null || !tm.IsUnlocked(island.Value.island)) return false;

        var (iCoord, cx, cy) = island.Value;
        if (cx + data.gridSize.x > tm.islandSize || cy + data.gridSize.y > tm.islandSize) return false;

        for (int dx = 0; dx < data.gridSize.x; dx++)
            for (int dy = 0; dy < data.gridSize.y; dy++)
            {
                CellData cd = tm.GetCellData(iCoord, cx + dx, cy + dy);
                if (cd == null || cd.hasResource || cd.hasBuilding) return false;
            }

        return HasMaterials(data);
    }

    private bool CanPlaceBridge(Vector2Int gCell)
    {
        if (tm.IsWalkable(gCell)) return false;

        bool adjacent = false;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var d in dirs)
            if (tm.IsWalkable(gCell + d)) { adjacent = true; break; }

        if (!adjacent) return false;
        return HasMaterials(availableBuildings[selectedIndex]);
    }

    private bool HasMaterials(BuildingData data)
    {
        bool m = inventory.GetItemCount(data.requiredItem) >= data.requiredAmount;
        bool a = !string.IsNullOrEmpty(data.altRequiredItem) &&
                 inventory.GetItemCount(data.altRequiredItem) >= data.altRequiredAmount;
        return m || a;
    }

    private void TryPlaceBuilding()
    {
        BuildingData data = availableBuildings[selectedIndex];
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        var gCell = tm.WorldToGlobalCell(mouseWorld);
        if (gCell == null || !CanPlaceBuilding(data, gCell.Value))
        {
            Debug.Log("此处无法建造");
            return;
        }

        if (data.isBridge)
            PlaceBridge(data, gCell.Value);
        else
            PlaceBuilding(data, gCell.Value);
    }

    private void PlaceBridge(BuildingData data, Vector2Int gCell)
    {
        bool hasMain = inventory.GetItemCount(data.requiredItem) >= data.requiredAmount;
        if (hasMain) inventory.RemoveItem(data.requiredItem, data.requiredAmount);
        else inventory.RemoveItem(data.altRequiredItem, data.altRequiredAmount);

        tm.AddBridge(gCell);

        Vector3 pos = (Vector3)tm.GlobalCellToWorld(gCell);
        GameObject obj = new GameObject("桥");
        obj.transform.position = pos;
        obj.transform.SetParent(buildingsParent);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();

        sr.sprite = spriteDB?.GetBuildingSprite("桥") 
                    ?? MakeTexSprite(Vector2Int.one, data.buildingColor);
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100)+10;

        Building b = obj.AddComponent<Building>();
        b.buildingName = "桥";

        Debug.Log($"架桥完成：({gCell.x}, {gCell.y})");
    }

    private void PlaceBuilding(BuildingData data, Vector2Int gCell)
    {
        if (!HasMaterials(data)) return;
        bool hasMain = inventory.GetItemCount(data.requiredItem) >= data.requiredAmount;
        if (hasMain) inventory.RemoveItem(data.requiredItem, data.requiredAmount);
        else inventory.RemoveItem(data.altRequiredItem, data.altRequiredAmount);

        var island = tm.GlobalCellToIsland(gCell);
        var (iCoord, cx, cy) = island.Value;

        for (int dx = 0; dx < data.gridSize.x; dx++)
            for (int dy = 0; dy < data.gridSize.y; dy++)
                tm.GetCellData(iCoord, cx + dx, cy + dy).hasBuilding = true;

        Vector2 origin = tm.CellToWorld(iCoord, cx, cy);
        Vector3 center = (Vector3)origin + new Vector3(
            (data.gridSize.x - 1) * 0.5f, (data.gridSize.y - 1) * 0.5f, 0);

        GameObject obj = new GameObject(data.buildingName);
        obj.transform.position = center;
        obj.transform.SetParent(buildingsParent);
        obj.transform.localScale = new Vector3(data.gridSize.x, data.gridSize.y, 1);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = spriteDB?.GetBuildingSprite(data.buildingName) 
                    ?? MakeTexSprite(data.gridSize, data.buildingColor);
        sr.sortingOrder = Mathf.RoundToInt(-center.y * 100)+10;

        BoxCollider2D bc = obj.AddComponent<BoxCollider2D>();
        bc.size = Vector2.one;

        Building b = obj.AddComponent<Building>();
        b.buildingName = data.buildingName;

        Debug.Log($"建造: {data.buildingName}（{data.gridSize.x}×{data.gridSize.y}）");
    }

    private void ExitBuildingMode()
    {
        isBuildingMode = false;
        previewObject.SetActive(false);
    }

    private Sprite MakeTexSprite(Vector2Int size, Color c)
    {
        int ppu = 16, w = size.x * ppu, h = size.y * ppu;
        Texture2D t = new Texture2D(w, h);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                t.SetPixel(x, y, (x < 2 || x >= w - 2 || y < 2 || y >= h - 2) ? c * 0.6f : c);
        t.filterMode = FilterMode.Point;
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
    }
}