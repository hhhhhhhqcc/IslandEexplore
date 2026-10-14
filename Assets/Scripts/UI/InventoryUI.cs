using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject panel;
    public Transform slotParent;
    public GameObject slotPrefab;

    [Header("精灵")]
    public SpriteDB spriteDB;


    public ResourceDatabase resourceDB;
    private InventoryManager inventory;
    private bool isOpen;
    public bool IsOpen => isOpen;
    private List<GameObject> slots = new List<GameObject>();

    private void Start()
    {
        inventory = FindObjectOfType<InventoryManager>();
        panel.SetActive(false);
        CreateSlots();
    }

    private void Update()
    {
        if (GameMenuController.BlocksGameplayInput)
        {
            if (isOpen) Close();
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Toggle();
        }
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            GameMenuController.ConsumeEscapeThisFrame();
            Close();
        }

        if (isOpen)
        {
            RefreshSlots();
        }
    }

    private void Toggle()
    {
        isOpen = !isOpen;
        panel.SetActive(isOpen);
    }

    public void Close()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>
    /// 创建 8 个空栏位
    /// </summary>
    private void CreateSlots()
    {
        for (int i = 0; i < 8; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotParent);
            slot.name = $"Slot_{i}";
            slots.Add(slot);
        }
    }

    /// <summary>
    /// 每帧刷新栏位显示
    /// </summary>
    private void RefreshSlots()
    {
        if (inventory == null) return;

        var items = new List<KeyValuePair<string, int>>(inventory.GetAllItems());

        for (int i = 0; i < slots.Count; i++)
        {
            Transform slot = slots[i].transform;

            // 物品图标
            Image icon = slot.Find("Icon")?.GetComponent<Image>();
            // 数量文字
            Text countText = slot.Find("Count")?.GetComponent<Text>();
            // 名称文字
            Text nameText = slot.Find("Name")?.GetComponent<Text>();

            if (i < items.Count)
            {
                string itemName = items[i].Key;
                int count = items[i].Value;

            if (icon != null)
            {
                var db = resourceDB;
                var res = db?.GetByName(itemName);
                if (res != null && res.dropSprite != null)
                {
                    icon.sprite = res.dropSprite;
                    icon.color = Color.white;
                }
                else
                {
                    icon.sprite = null;
                    icon.color = GetFallbackColor(itemName);
                }
            }
            if (countText != null)
                countText.text = count >= 999 ? "999" : count.ToString();
            if (nameText != null)
                nameText.text = itemName;
            }
            else
            {
                // 空栏位
                if (icon != null) { icon.sprite = null; icon.color = new Color(0, 0, 0, 0); }
                if (countText != null) countText.text = "";
                if (nameText != null) nameText.text = "";
            }
        }
    }

        private Color GetFallbackColor(string name)
    {
        var db = FindObjectOfType<ResourceDatabase>();
        if (db != null)
        {
            var res = db.GetByName(name);
            if (res != null) return res.fallbackColor;
        }
        return Color.cyan;
    }
}