using UnityEngine;
using UnityEngine.EventSystems;

public class TileClickHandler : MonoBehaviour
{
    private TileManager tm;

    private void Start()
    {
        tm = TileManager.Instance;
    }

    private void Update()
    {
        if (GameMenuController.BlocksGameplayInput) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (Input.GetMouseButtonDown(0))
        {
            TryUnlock();
        }
    }

    private void TryUnlock()
    {
        Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        wp.z = 0;

        // 用 WorldToCell 反查哪个岛屿哪一格
        var cell = tm.WorldToCell(wp);
        if (cell == null) return;

        var (iCoord, cx, cy) = cell.Value;

        if (tm.IsUnlocked(iCoord)) return;  // 已解锁，不处理

        if (!tm.CanUnlock(iCoord))
        {
            // 可能在岛屿格子但不在相邻岛屿：查是不是在某个已揭示岛屿范围内
            return;
        }

        int cost = tm.GetCost();
        InventoryManager inv = FindObjectOfType<InventoryManager>();
        if (inv != null && !inv.SpendGold(cost))
        {
            Debug.Log($"金币不足！需要 {cost}，当前 {inv.gold}");
            return;
        }

        tm.UnlockIsland(iCoord);

        // 刷新该岛屿的格子颜色（删了重画）
        Transform parent = GameObject.Find("TilesContainer")?.transform;
        if (parent != null)
        {
            string islandName = $"Island_{iCoord.x}_{iCoord.y}";
            Transform old = parent.Find(islandName);
            if (old != null) Destroy(old.gameObject);
        }
    }
}