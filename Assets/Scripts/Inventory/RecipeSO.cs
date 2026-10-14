using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "浮岛开拓者/合成配方")]
public class RecipeSO : ScriptableObject
{
    public string resultItem = "1";
    public int resultAmount = 1;

    [Header("材料1")]
    public string ingredient1 = "1";
    public int amount1 = 3;

    [Header("材料2（可留空）")]
    public string ingredient2 = "";
    public int amount2 = 0;

    [Header("材料3（可留空）")]
    public string ingredient3 = "";
    public int amount3 = 0;
}