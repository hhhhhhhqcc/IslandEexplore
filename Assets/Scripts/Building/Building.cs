using UnityEngine;
using UnityEngine.EventSystems;

public class Building : MonoBehaviour
{
    public string buildingName;
    public float interactRange = 2f;

    private void Update()
    {
        if (GameMenuController.BlocksGameplayInput) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (!Input.GetMouseButtonDown(1)) return;

        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) return;
        if (Vector2.Distance(transform.position, player.position) > interactRange) return;

        Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        wp.z = 0;

        Collider2D hit = Physics2D.OverlapPoint(wp);
        if (hit == null) return;
        if (hit.gameObject != gameObject && !hit.transform.IsChildOf(transform) &&
            !transform.IsChildOf(hit.transform)) return;

        if (buildingName.Contains("熔炉"))
        {
            CraftingUI ui = FindObjectOfType<CraftingUI>();
            if (ui != null) ui.OpenFurnace(this);
        }
    }
}