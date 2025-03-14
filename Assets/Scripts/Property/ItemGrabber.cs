using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;


public class ItemGrabber : MonoBehaviour
{
    [System.Serializable]
    public class InventorySlot
    {
        public Transform slotTransform;
        public GameObject displayObject;
        public GameObject originalPrefab;
    }

    [Header("Положение в руке")]
    [SerializeField] private RectTransform handPosition;
    [SerializeField] private Vector3 handRotation = new Vector3(30f, 45f, 0f);
    [SerializeField] private Vector3 handOffset = new Vector3(0.5f, -0.3f, 0.3f);

    [Header("Стандартное положение")]
    [SerializeField] private Vector3 normalHoldPosition = new Vector3(0f, 0f, 2f);
    private bool isFromInventory = false;

    [Header("Инвентарь")]
    [SerializeField] private InventorySlot[] slots = new InventorySlot[3];
    [SerializeField] private float inventoryItemScale = 50f;
    private bool isInHand = false;

    [Header("Настройки камеры")]
    [SerializeField] private float zoomFOV = 30f;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomSpeed = 5f;

    [Header("Настройки")]
    [SerializeField] private float grabDistance = 3f;
    [SerializeField] private float grabSpeed = 10f;
    [SerializeField] private LayerMask grabLayer;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float normalRotationAngle = 45f;
    [SerializeField] private float shiftRotationAngle = 90f;
    [SerializeField] private float spawnDistance = 2f;
    
 

    private Rigidbody heldItem;
    private Vector3 holdPosition;
    private Quaternion targetRotation;
    private bool isZoomed = false;
    private float currentFOV;

    // Ссылка на компонент Scanner
    private Scanner scanner;

    private void Start()
    {
        holdPosition = new Vector3(0f, 0f, 2f);
        LockCursor();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                slots[i] = new InventorySlot();
        }

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = normalFOV;
            currentFOV = normalFOV;
        }

        // Получаем компонент Scanner
        scanner = GetComponent<Scanner>();
        if (scanner == null)
        {
            Debug.LogError("Сканер не найден!");
        }
    }

    private void Update()
    {
        // Переключение сканера по нажатию C независимо от захваченного объекта
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (scanner != null)
            {
                bool newState = !scanner.IsScannerActive();
                scanner.ToggleScanner(newState);
                Debug.Log("Сканер переключен: " + newState);
            }
        }

        // Остальной функционал ItemGrabber
        if (Input.GetKeyDown(KeyCode.Q) && heldItem != null)
        {
            ThrowItemFromHand();
        }

        if (!isInHand)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (heldItem == null)
                    TryGrabItem();
                else
                    DropItem();
            }

            if (Input.GetKeyDown(KeyCode.E) && heldItem != null && !isZoomed)
            {
                TryAddToInventory();
            }

            if (heldItem != null)
            {
                HandleItemHold();
                HandleItemRotation();
                HandleZoom();
            }
        }
        else
        {
            HandleItemInHand();
        }

        for (int i = 0; i < 3; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (!isInHand)
                {
                    SpawnItemFromSlot(i);
                }
                else
                {
                    TryReturnToInventory(i);
                }
            }
        }

        UpdateCameraZoom();
    }

    public bool IsScannerActive()
    {
        return scanner != null && scanner.isScannerActive;
    }

    private void ThrowItemFromHand()
    {
        if (heldItem != null)
        {
            heldItem.transform.SetParent(null);

            Rigidbody rb = heldItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.freezeRotation = false;
                rb.drag = 0;

                Vector3 throwDirection = playerCamera.transform.forward;
                rb.AddForce(throwDirection * 5f, ForceMode.Impulse);
            }

            Collider col = heldItem.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = false;
            }

            heldItem = null;
            isFromInventory = false;
            isInHand = false;

            if (playerMovement != null)
            {
                playerMovement.enabled = true;
                LockCursor();
            }
        }
    }

    private void TryReturnToInventory(int slotIndex)
    {
        if (heldItem != null && isInHand && slotIndex >= 0 && slotIndex < slots.Length)
        {
            if (slots[slotIndex].displayObject == null)
            {
                slots[slotIndex].originalPrefab = Instantiate(heldItem.gameObject);
                slots[slotIndex].originalPrefab.SetActive(false);
                DontDestroyOnLoad(slots[slotIndex].originalPrefab);

                slots[slotIndex].displayObject = Instantiate(heldItem.gameObject, slots[slotIndex].slotTransform);
                Rigidbody rb = slots[slotIndex].displayObject.GetComponent<Rigidbody>();
                if (rb != null) Destroy(rb);

                Collider col = slots[slotIndex].displayObject.GetComponent<Collider>();
                if (col != null) Destroy(col);

                slots[slotIndex].displayObject.transform.localPosition = Vector3.zero;
                slots[slotIndex].displayObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
                slots[slotIndex].displayObject.transform.localScale = Vector3.one * inventoryItemScale;

                Destroy(heldItem.gameObject);
                heldItem = null;
                isInHand = false;

                if (playerMovement != null)
                {
                    playerMovement.enabled = true;
                    LockCursor();
                }
            }
        }
    }

    private void SpawnItemFromSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slots.Length &&
            slots[slotIndex].displayObject != null &&
            slots[slotIndex].originalPrefab != null)
        {
            if (heldItem != null) return;

            Vector3 spawnPosition = playerCamera.transform.position +
                                  playerCamera.transform.right * handOffset.x +
                                  playerCamera.transform.up * handOffset.y +
                                  playerCamera.transform.forward * handOffset.z;
            GameObject spawnedItem = Instantiate(slots[slotIndex].originalPrefab);
            spawnedItem.SetActive(true);
            Destroy(slots[slotIndex].displayObject);
            Destroy(slots[slotIndex].originalPrefab);
            slots[slotIndex].displayObject = null;
            slots[slotIndex].originalPrefab = null;
            spawnedItem.transform.position = spawnPosition;
            spawnedItem.transform.rotation = playerCamera.transform.rotation * Quaternion.Euler(handRotation);
            spawnedItem.transform.SetParent(playerCamera.transform);

            Rigidbody rb = spawnedItem.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = spawnedItem.AddComponent<Rigidbody>();
            }

            rb.useGravity = false;
            rb.isKinematic = true;
            rb.freezeRotation = true;
            rb.drag = 10;

            Collider col = spawnedItem.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true;
            }
            else
            {
                BoxCollider newCol = spawnedItem.AddComponent<BoxCollider>();
                newCol.isTrigger = true;
            }
            heldItem = rb;
            targetRotation = rb.rotation;
            isFromInventory = true;
            isInHand = true;

            if (playerMovement != null)
            {
                playerMovement.enabled = true;
            }
        }
    }

    private void HandleItemInHand()
    {
        if (heldItem != null)
        {
            heldItem.transform.localPosition = new Vector3(
                handOffset.x,
                handOffset.y,
                handOffset.z
            );
            heldItem.transform.localRotation = Quaternion.Euler(handRotation);
        }
    }

    private void TryGrabItem()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, grabDistance, grabLayer))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                heldItem = rb;
                heldItem.useGravity = false;
                heldItem.freezeRotation = true;
                heldItem.drag = 10;
                targetRotation = heldItem.rotation;
                isFromInventory = false;
                if (playerMovement != null)
                    playerMovement.enabled = false;
            }
        }
    }

    private void TryAddToInventory()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].displayObject == null)
            {
                slots[i].originalPrefab = Instantiate(heldItem.gameObject);
                slots[i].originalPrefab.SetActive(false);
                DontDestroyOnLoad(slots[i].originalPrefab);
                slots[i].displayObject = Instantiate(heldItem.gameObject, slots[i].slotTransform);
                Rigidbody rb = slots[i].displayObject.GetComponent<Rigidbody>();
                if (rb != null) Destroy(rb);
                Collider col = slots[i].displayObject.GetComponent<Collider>();
                if (col != null) Destroy(col);
                slots[i].displayObject.transform.localPosition = Vector3.zero;
                slots[i].displayObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
                slots[i].displayObject.transform.localScale = Vector3.one * inventoryItemScale;
                Destroy(heldItem.gameObject);
                heldItem = null;
                if (playerMovement != null)
                {
                    playerMovement.enabled = true;
                    LockCursor();
                }
                break;
            }
        }
    }

    private void FitItemToSlot(Transform item, Transform slot)
    {
        RectTransform slotRect = slot as RectTransform;
        if (slotRect == null) return;
        Renderer itemRenderer = item.GetComponent<Renderer>();
        if (itemRenderer == null) return;
        Bounds itemBounds = itemRenderer.bounds;
        float slotSize = Mathf.Min(slotRect.rect.width, slotRect.rect.height) * 0.8f;
        float itemSize = Mathf.Max(itemBounds.size.x, itemBounds.size.y, itemBounds.size.z);
        float scale = slotSize / itemSize;
        item.localScale = Vector3.one * scale;
    }

    private void HandleItemRotation()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            float rotationAmount = Input.GetKey(KeyCode.LeftShift) ? shiftRotationAngle : normalRotationAngle;
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            if (Mathf.Abs(mouseX) > Mathf.Abs(mouseY))
            {
                if (mouseX > 0)
                    targetRotation *= Quaternion.Euler(0, rotationAmount, 0);
                else
                    targetRotation *= Quaternion.Euler(0, -rotationAmount, 0);
            }
            else
            {
                if (mouseY > 0)
                    targetRotation *= Quaternion.Euler(rotationAmount, 0, 0);
                else
                    targetRotation *= Quaternion.Euler(-rotationAmount, 0, 0);
            }
        }
    }

    private void HandleItemHold()
    {
        if (heldItem != null)
        {
            Vector3 targetPos;
            Quaternion targetRot;
            if (isFromInventory)
            {
                targetPos = playerCamera.transform.position +
                           playerCamera.transform.right * handOffset.x +
                           playerCamera.transform.up * handOffset.y +
                           playerCamera.transform.forward * handOffset.z;
                targetRot = playerCamera.transform.rotation * Quaternion.Euler(handRotation);
            }
            else
            {
                targetPos = playerCamera.transform.position +
                           playerCamera.transform.TransformDirection(normalHoldPosition);
                targetRot = targetRotation;
            }
            Vector3 lerpPos = Vector3.Lerp(heldItem.position, targetPos, Time.deltaTime * grabSpeed);
            heldItem.MovePosition(lerpPos);
            heldItem.MoveRotation(Quaternion.Lerp(heldItem.rotation, targetRot, Time.deltaTime * grabSpeed));
        }
    }

    private void UpdateCameraZoom()
    {
        if (playerCamera != null)
        {
            float targetFOV = isZoomed ? zoomFOV : normalFOV;
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * zoomSpeed);
            playerCamera.fieldOfView = currentFOV;
        }
    }

    private void HandleZoom()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            isZoomed = !isZoomed;
        }
    }

    private void DropItem()
    {
        if (heldItem != null)
        {
            heldItem.useGravity = true;
            heldItem.freezeRotation = false;
            heldItem.drag = 0;
            heldItem = null;
            isFromInventory = false;
            if (playerMovement != null)
            {
                playerMovement.enabled = true;
                LockCursor();
            }
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        if (heldItem != null)
        {
            DropItem();
        }
    }
}
