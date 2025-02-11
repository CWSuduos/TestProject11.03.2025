using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Приседание")]
    private float normalHeight = 2f;
    private float crouchHeight = 1f;
    private Vector3 normalCameraPos;
    private Vector3 crouchCameraPos;
    private bool isCrouching;

    [Header("Камера")]
    [SerializeField] private float mouseSensitivity = 2f;
    public Camera playerCamera;
    private float xRotation = 0f;

    [Header("Звук шагов")]
    [SerializeField] private AudioClip footstepSound; 
    [SerializeField] private float footstepInterval = 0.5f; 
    private AudioSource audioSource; 
    private float footstepTimer = 0f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        normalCameraPos = playerCamera.transform.localPosition;
        crouchCameraPos = new Vector3(normalCameraPos.x, normalCameraPos.y / 2f, normalCameraPos.z);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCrouch();
        HandleMovement();
        HandleMouseLook();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGame();
        }
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            isCrouching = !isCrouching;

            if (isCrouching)
            {
                controller.height = crouchHeight;
                playerCamera.transform.localPosition = crouchCameraPos;
            }
            else
            {
                controller.height = normalHeight;
                playerCamera.transform.localPosition = normalCameraPos;
            }
        }
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;
        currentSpeed = isCrouching ? currentSpeed * 0.5f : currentSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        HandleFootsteps(move);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleFootsteps(Vector3 move)
    {
        if (move.magnitude > 0 && isGrounded)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f) 
            {
                PlayFootstepSound();
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = footstepInterval;
        }
    }

    private void PlayFootstepSound()
    {
        if (audioSource != null && footstepSound != null)
        {
            audioSource.PlayOneShot(footstepSound); 
        }
    }

    private void ExitGame()
    {
        Debug.Log("Выход из игры..."); 
        Application.Quit();
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}