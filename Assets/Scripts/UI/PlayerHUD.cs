using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    public Text hpText;
    public Text goldText;

    private PlayerController player;
    private InventoryManager inventory;

    private void Start()
    {
        player = FindObjectOfType<PlayerController>();
        inventory = FindObjectOfType<InventoryManager>();
    }

    private void Update()
    {
        if (hpText != null && player != null)
            hpText.text = $"HP: {player.GetHPPercent() * 100:F0}/100";

        if (goldText != null && inventory != null)
            goldText.text = $"金币: {inventory.gold}";
    }
}