using UnityEngine;
using UnityEngine.UI;

public class ToolHotbarUI : MonoBehaviour
{
    public GameObject slotPrefab;       // 单个栏位预制体
    public Transform hotbarParent;      // 栏位容器
    public Color selectedColor = Color.yellow;

    private PlayerController player;
    private Image[] slots;
    private int lastIndex = -1;

    private void Start()
    {
        player = FindObjectOfType<PlayerController>();
        slots = new Image[4];

        for (int i = 0; i < 4; i++)
        {
            GameObject obj = Instantiate(slotPrefab, hotbarParent);
            obj.name = $"Slot_{i}";
            slots[i] = obj.GetComponent<Image>();

            Text label = obj.GetComponentInChildren<Text>();
            if (label != null) label.text = (i + 1).ToString();
        }
    }

    private void Update()
    {
        if (player == null) return;

        ToolData[] tools = player.tools;

        for (int i = 0; i < 4; i++)
        {
            if (i < tools.Length && tools[i] != null && tools[i].canAttack)
            {
                slots[i].color = Color.white;
                // 显示工具图标
                Image icon = slots[i].transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null && tools[i].icon != null)
                {
                    icon.sprite = tools[i].icon;
                    icon.color = Color.white;
                }
            }
            else
            {
                slots[i].color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }

        int currentIndex = GetToolIndex(player);
        if (currentIndex != lastIndex)
        {
            if (lastIndex >= 0 && lastIndex < 4) slots[lastIndex].color = Color.white;
            if (currentIndex >= 0 && currentIndex < 4) slots[currentIndex].color = selectedColor;
            lastIndex = currentIndex;
        }
    }

    private int GetToolIndex(PlayerController p)
    {
        string name = p.GetToolName();
        if (name == "空手") return -1;
        for (int i = 0; i < p.tools.Length; i++)
            if (p.tools[i] != null && p.tools[i].toolName == name) return i;
        return -1;
    }
}