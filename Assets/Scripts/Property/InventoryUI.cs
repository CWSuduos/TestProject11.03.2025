using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform inventoryPanel;
    [SerializeField] private Image[] slots = new Image[3]; 

    [Header("Настройки")]
    [SerializeField] private Vector2 panelOffset = new Vector2(20f, 20f); 

    private void Start()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("Inventory Panel не назначен!");
            return;
        }


        PositionInventoryPanel();
    }

    private void PositionInventoryPanel()
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        Vector2 panelSize = inventoryPanel.sizeDelta;

        float posX = screenSize.x - panelSize.x - panelOffset.x;
        float posY = screenSize.y - panelSize.y - panelOffset.y;
        inventoryPanel.anchoredPosition = new Vector2(posX, posY);
    }
}