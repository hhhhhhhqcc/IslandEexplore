using UnityEngine;
using UnityEngine.UI;

public class CraftingUI : MonoBehaviour
{
    public GameObject panel;
    public Transform recipeListParent;
    public GameObject recipeButtonPrefab;
    public Text stationTitle;

    private CraftingManager crafting;
    private bool isOpen;
    private Building currentStation;

    private void Start()
    {
        crafting = FindObjectOfType<CraftingManager>();
        panel.SetActive(false);
        BuildRecipeButtons();
    }

    private void Update()
    {
        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (currentStation != null)
            {
                Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
                if (player != null &&
                    Vector2.Distance(currentStation.transform.position, player.position) >
                    currentStation.interactRange + 1f)
                {
                    Close();
                }
            }
        }
    }

    public void OpenFurnace(Building station)
    {
        currentStation = station;
        if (stationTitle != null)
            stationTitle.text = "熔炉 - 冶炼";
        isOpen = true;
        panel.SetActive(true);
    }

    public void Close()
    {
        isOpen = false;
        currentStation = null;
        panel.SetActive(false);
    }

    private void BuildRecipeButtons()
    {
        if (crafting == null || crafting.recipes == null) return;

        foreach (var recipe in crafting.recipes)
        {
            GameObject btnObj = Instantiate(recipeButtonPrefab, recipeListParent);
            btnObj.name = $"Btn_{recipe.resultItem}";

            Text label = btnObj.GetComponentInChildren<Text>();
            if (label != null)
            {
                string mats = $"{recipe.amount1} {recipe.ingredient1}";
                if (!string.IsNullOrEmpty(recipe.ingredient2))
                    mats += $" + {recipe.amount2} {recipe.ingredient2}";
                label.text = $"{recipe.resultItem}  ← {mats}";
            }

            Button btn = btnObj.GetComponent<Button>();
            RecipeSO captured = recipe;
            btn.onClick.AddListener(() => OnCraftClicked(captured));
        }
    }

    private void OnCraftClicked(RecipeSO recipe)
    {
        if (crafting.Craft(recipe))
            StartCoroutine(FlashButton(recipe));
        else
            Debug.Log("材料不足！");
    }

    private System.Collections.IEnumerator FlashButton(RecipeSO recipe)
    {
        Transform btn = recipeListParent.Find($"Btn_{recipe.resultItem}");
        if (btn == null) yield break;
        Image img = btn.GetComponent<Image>();
        if (img == null) yield break;
        Color orig = img.color;
        img.color = Color.green;
        yield return new WaitForSeconds(0.15f);
        img.color = orig;
    }
}