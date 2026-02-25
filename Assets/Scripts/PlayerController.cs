using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerController : NetworkBehaviour
{
    public Transform cameraMount;

    public float moveSpeed = 5f;
    public float crouchSpeed = 2.5f;
    public float lookSensitivity = 2f;
    private bool isGrounded = false;
    public float jumpHeight = 2f;
    private float jumpCooldown = 0f;
    private const float jumpCooldownTime = 0.2f;
    public float gravity = -20f;

    public float standHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 10f;

    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    private Camera playerCamera;
    private float verticalLook = 0f;
    private float verticalVelocity = 0f;
    private bool isCrouching = false;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private CapsuleCollider capsuleCollider;

    private void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
    }
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        SetupCamera();
        SetupInput();
    }

    private void SetupCamera()
    {
        GameObject camObj = new GameObject("PlayerCamera");
        playerCamera = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();

        camObj.transform.SetParent(cameraMount);
        camObj.transform.localPosition = Vector3.zero;
        camObj.transform.localRotation = Quaternion.identity;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetupInput()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            return;
        }

        InputActionAsset inputActions = playerInput.actions;

        moveAction = inputActions.FindAction("Player/Move");
        lookAction = inputActions.FindAction("Player/Look");
        jumpAction = inputActions.FindAction("Player/Jump");
        crouchAction = inputActions.FindAction("Player/Crouch");

        if (moveAction == null || lookAction == null || jumpAction == null || crouchAction == null)
        {
            return;
        }

        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => moveInput = Vector2.zero;

        lookAction.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        lookAction.canceled += ctx => lookInput = Vector2.zero;

        jumpAction.performed += ctx => jumpPressed = true;

        crouchAction.performed += ctx => isCrouching = true;
        crouchAction.canceled += ctx => isCrouching = false;

        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        crouchAction.Enable();
    }

    private void OnDestroy()
    {
        if (moveAction != null)
        {
            moveAction.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
            moveAction.canceled -= ctx => moveInput = Vector2.zero;
        }
        if (lookAction != null)
        {
            lookAction.performed -= ctx => lookInput = ctx.ReadValue<Vector2>();
            lookAction.canceled -= ctx => lookInput = Vector2.zero;
        }
        if (jumpAction != null)
            jumpAction.performed -= ctx => jumpPressed = true;
        if (crouchAction != null)
        {
            crouchAction.performed -= ctx => isCrouching = true;
            crouchAction.canceled -= ctx => isCrouching = false;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleLook();
        HandleMovement();
        HandleCrouch();
    }

    private void HandleMovement()
    {
        // Count down the cooldown
        if (jumpCooldown > 0f)
            jumpCooldown -= Time.deltaTime;

        // Only check ground when not in jump cooldown
        if (jumpCooldown <= 0f)
        {
            if (groundCheck != null)
                isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            else
                isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        }

        if (isGrounded)
        {
            verticalVelocity = 0f;

            if (jumpPressed)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpCooldown = jumpCooldownTime; 
                isGrounded = false;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        jumpPressed = false;

        float speed = isCrouching ? crouchSpeed : moveSpeed;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move *= speed;
        move.y = verticalVelocity;

        transform.position += move * Time.deltaTime;
    }
    private void HandleLook()
    {
        if (playerCamera == null) return;

        Vector2 scaledLook = lookInput * lookSensitivity * 0.1f;

        transform.Rotate(Vector3.up * scaledLook.x);

        verticalLook -= scaledLook.y;
        verticalLook = Mathf.Clamp(verticalLook, -80f, 80f);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalLook, 0f, 0f);
    }

    private void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchHeight : standHeight;

        if (capsuleCollider != null)
        {
            capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            capsuleCollider.center = new Vector3(0, capsuleCollider.height / 2f, 0);
        }

        Vector3 mountPos = cameraMount.localPosition;
        mountPos.y = Mathf.Lerp(mountPos.y, targetHeight - 0.2f, Time.deltaTime * crouchTransitionSpeed);
        cameraMount.localPosition = mountPos;
    }
}