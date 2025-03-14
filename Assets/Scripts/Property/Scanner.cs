using UnityEngine;
using UnityEngine.UI;

public class Scanner : MonoBehaviour
{
    [Header("Scan Settings")]
    [SerializeField] private float scanDistance = 5f;
    [SerializeField] private LayerMask scanLayer;
    [SerializeField] private Camera playerCamera;

    [Header("UI Elements")]
    [SerializeField] private GameObject scanPanel;
    [SerializeField] private Text itemNameText;
    [SerializeField] private Text itemDescriptionText;
    [SerializeField] private Image progressBar;
    [SerializeField] private InputField renameInputField; 

    [Header("Timing")]
    [SerializeField] private float displayDuration = 5f;
    [SerializeField] private float scanCooldown = 0.5f;

    public bool isScannerActive = false;

    private float displayTimer;
    private float lastScanTime;
    private bool isScanning;
    private Item currentItem;

    private bool isRenaming = false;

    private void Start()
    {

        if (renameInputField != null)
        {
            renameInputField.gameObject.SetActive(false);
            renameInputField.onEndEdit.AddListener(OnRenameFinished);
        }
    }

    private void Update()
    {
        if (!isScannerActive)
        {
            ForceHideUI();
            return;
        }


        if (!isRenaming)
        {
            HandleScanning();


            if (currentItem != null && Input.GetKeyDown(KeyCode.C))
            {
                StartRename();
            }
        }

        UpdateProgressBar();
    }

    private void HandleScanning()
    {
        if (Time.time - lastScanTime >= scanCooldown)
        {
            ScanForItems();
            lastScanTime = Time.time;
        }

        if (isScanning)
        {
            displayTimer -= Time.deltaTime;
            if (displayTimer <= 0)
            {
                ForceHideUI();
            }
        }
    }

    private void ScanForItems()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, scanDistance, scanLayer))
        {
            Item newItem = hit.collider.GetComponent<Item>();
            if (newItem != null)
            {
                if (newItem != currentItem)
                {
                    HandleNewItem(newItem);
                }
                return;
            }
        }
        currentItem = null;
    }

    private void HandleNewItem(Item item)
    {
        currentItem = item;
        displayTimer = displayDuration;
        isScanning = true;
        UpdateUI(item);
    }

    private void UpdateUI(Item item)
    {
        scanPanel.SetActive(true);
        itemNameText.text = item.itemName;
        itemDescriptionText.text = item.itemDescription;
    }

    private void UpdateProgressBar()
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = displayTimer / displayDuration;
        }
    }

    private void ForceHideUI()
    {
        scanPanel.SetActive(false);
        currentItem = null;
    }

    public void ToggleScanner(bool active)
    {
        isScannerActive = active;
        if (!active)
        {
            ForceHideUI();
        }
    }

    public bool IsScannerActive()
    {
        return isScannerActive;
    }


    private void StartRename()
    {
        if (renameInputField == null || currentItem == null) return;

        isRenaming = true;
        renameInputField.gameObject.SetActive(true);
        renameInputField.text = currentItem.itemName;
        renameInputField.Select();
        renameInputField.ActivateInputField();
    }


    private void OnRenameFinished(string newName)
    {
        if (currentItem != null && !string.IsNullOrEmpty(newName))
        {
            currentItem.itemName = newName;
            itemNameText.text = newName;
        }
        EndRename();
    }

    private void EndRename()
    {
        isRenaming = false;
        if (renameInputField != null)
        {
            renameInputField.gameObject.SetActive(false);
        }
    }
}
