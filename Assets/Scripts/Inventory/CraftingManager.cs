using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public RecipeSO[] recipes;
    private InventoryManager inventory;

    private void Start()
    {
        inventory = GetComponent<InventoryManager>();
    }

    /// <summary>
    /// 尝试合成（返回是否成功）
    /// </summary>
    public bool Craft(RecipeSO recipe)
    {
        if (recipe == null) return false;

        // 检查材料1
        if (inventory.GetItemCount(recipe.ingredient1) < recipe.amount1)
            return false;

        // 检查材料2
        if (!string.IsNullOrEmpty(recipe.ingredient2) &&
            inventory.GetItemCount(recipe.ingredient2) < recipe.amount2)
            return false;

        // 检查材料3
        if (!string.IsNullOrEmpty(recipe.ingredient3) &&
            inventory.GetItemCount(recipe.ingredient3) < recipe.amount3)
            return false;

        // 扣材料
        inventory.RemoveItem(recipe.ingredient1, recipe.amount1);
        if (!string.IsNullOrEmpty(recipe.ingredient2))
            inventory.RemoveItem(recipe.ingredient2, recipe.amount2);
        if (!string.IsNullOrEmpty(recipe.ingredient3))
            inventory.RemoveItem(recipe.ingredient3, recipe.amount3);

        // 给成品
        inventory.AddItem(recipe.resultItem, recipe.resultAmount);
        Debug.Log($"合成: {recipe.resultItem} x{recipe.resultAmount}");

        return true;
    }

    /// <summary>
    /// 检查某个配方的材料是否足够
    /// </summary>
    public bool CanCraft(RecipeSO recipe)
    {
        if (recipe == null) return false;
        if (inventory.GetItemCount(recipe.ingredient1) < recipe.amount1) return false;
        if (!string.IsNullOrEmpty(recipe.ingredient2) &&
            inventory.GetItemCount(recipe.ingredient2) < recipe.amount2) return false;
        if (!string.IsNullOrEmpty(recipe.ingredient3) &&
            inventory.GetItemCount(recipe.ingredient3) < recipe.amount3) return false;
        return true;
    }
}