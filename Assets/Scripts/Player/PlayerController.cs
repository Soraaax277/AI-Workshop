using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sprintSpeed = 8f;
    [SerializeField] float jumpHeight = 1.5f;
    [SerializeField] float gravity = -20f;

    [Header("Look")]
    [SerializeField] Transform cameraPivot;
    [SerializeField] float lookSensitivity = 0.15f;
    [SerializeField] float minPitch = -80f;
    [SerializeField] float maxPitch = 80f;

    [Header("Input")]
    [SerializeField] InputActionAsset inputActions;

    CharacterController controller;
    InputAction moveAction;
    InputAction lookAction;
    InputAction jumpAction;
    InputAction sprintAction;

    Vector3 velocity;
    float pitch;
    bool inputEnabled = true;

    public bool InputEnabled
    {
        get => inputEnabled;
        set => inputEnabled = value;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            gameObject.layer = playerLayer;

        if (inputActions == null)
        {
            Debug.LogError("PlayerController: Assign InputSystem_Actions to the Input Actions field.", this);
            return;
        }

        var playerMap = inputActions.FindActionMap("Player", true);
        moveAction = playerMap.FindAction("Move", true);
        lookAction = playerMap.FindAction("Look", true);
        jumpAction = playerMap.FindAction("Jump", true);
        sprintAction = playerMap.FindAction("Sprint", true);
    }

    void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
    }

    void Update()
    {
        if (!inputEnabled)
            return;

        HandleLook();
        HandleMovement();
    }

    void HandleLook()
    {
        if (cameraPivot == null || lookAction == null)
            return;

        Vector2 look = lookAction.ReadValue<Vector2>() * lookSensitivity;
        transform.Rotate(Vector3.up, look.x);
        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleMovement()
    {
        if (moveAction == null)
            return;

        bool grounded = controller.isGrounded;
        if (grounded && velocity.y < 0f)
            velocity.y = -2f;

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        float speed = sprintAction != null && sprintAction.IsPressed() ? sprintSpeed : moveSpeed;
        controller.Move(move * speed * Time.deltaTime);

        if (grounded && jumpAction != null && jumpAction.WasPressedThisFrame())
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
