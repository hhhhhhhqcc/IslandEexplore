using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // 用字典存物品：物品名 → 数量
    private Dictionary<string, int> items = new Dictionary<string, int>();

    private const int MAX_UNIQUE = 8;

    // 金币
    public int gold { get; private set; }

    private void Start()
    {
        gold = 100000;
        AddItem("树木", 5);
        AddItem("石头", 5);
        AddItem("纤维", 5);
        AddItem("铜矿石", 5);
        AddItem("铁矿石", 5);
        AddItem("金矿石", 5);
        AddItem("煤炭", 5);

        Debug.Log($"初始金币: {gold}，初始物品已发放");
    }


    /// <summary>
    /// 添加物品
    /// </summary>
        public void AddItem(string itemName, int amount)
    {
        if (items.ContainsKey(itemName))
        {
            items[itemName] = Mathf.Min(items[itemName] + amount, 999);
        }
        else
        {
            if (items.Count >= MAX_UNIQUE) return;
            items[itemName] = Mathf.Min(amount, 999);
        }
    }
    public bool CanAddItem(string itemName)
    {
        return items.ContainsKey(itemName) || items.Count < MAX_UNIQUE;
    }



    /// <summary>
    /// 查询物品数量
    /// </summary>
    public int GetItemCount(string itemName)
    {
        return items.TryGetValue(itemName, out int count) ? count : 0;
    }

    /// <summary>
    /// 消耗物品（返回是否成功）
    /// </summary>
    public bool RemoveItem(string itemName, int amount)
    {
        if (!items.ContainsKey(itemName) || items[itemName] < amount)
            return false;

        items[itemName] -= amount;
        if (items[itemName] <= 0)
            items.Remove(itemName);
        return true;
    }

    /// <summary>
    /// 添加金币
    /// </summary>
    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"金币 +{amount}（共 {gold}）");
    }

    /// <summary>
    /// 花费金币（返回是否成功）
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (gold < amount) return false;
        gold -= amount;
        return true;
    }

    /// <summary>
    /// 获取所有物品（给 HUD 用）
    /// </summary>
    public Dictionary<string, int> GetAllItems()
    {
        return new Dictionary<string, int>(items);
    }
}