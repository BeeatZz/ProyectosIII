using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class MultiplayerController : NetworkBehaviour
{

    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private GameObject playerVisuals;

    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravityMultiplier = 2.5f;
    [SerializeField] private float groundAcceleration = 20f;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float maxPitchAngle = 85f;

    [SerializeField][Range(0f, 20f)] private float lookSmoothing = 5f;
    [SerializeField] private PlayerNameplate namePlate;

    private InputSystem_Actions inputActions;
    private Vector2 rawLookDelta;
    private Vector2 smoothedLookDelta;
    private Vector2 moveInput;
    private Vector3 currentVelocityXZ;   
    private float verticalVelocity;    
    private bool jumpQueued;
    private bool isSprinting;
    private float cameraPitch;           
    private CharacterController cc;


    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        namePlate = GetComponentInChildren<PlayerNameplate>(true); 

       
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

 
        SetCameraActive(false);
    }


    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        SetCameraActive(true);

        if (playerVisuals != null)
            playerVisuals.SetActive(true);

        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();


        inputActions.Player.Jump.performed += OnJump;
        inputActions.Player.Sprint.performed += _ => isSprinting = true;
        inputActions.Player.Sprint.canceled += _ => isSprinting = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        namePlate.RefreshName();
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();

        if (inputActions != null)
        {
            inputActions.Player.Jump.performed -= OnJump;
            inputActions.Player.Sprint.performed -= _ => isSprinting = true;
            inputActions.Player.Sprint.canceled -= _ => isSprinting = false;
            inputActions.Player.Disable();
            inputActions.Dispose();
            inputActions = null;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    private void Update()
    {
        if (!isLocalPlayer) return;

        ReadInput();
        HandleLook();
        HandleMovement();
    }

    private void ReadInput()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        rawLookDelta = inputActions.Player.Look.ReadValue<Vector2>();
    }

    private void HandleLook()
    {
        smoothedLookDelta = lookSmoothing > 0f
            ? Vector2.Lerp(smoothedLookDelta, rawLookDelta, Time.deltaTime * lookSmoothing * 10f)
            : rawLookDelta;

        float yaw = smoothedLookDelta.x * mouseSensitivity;  
        float pitch = -smoothedLookDelta.y * mouseSensitivity;   

        transform.Rotate(Vector3.up, yaw, Space.World);

        cameraPitch = Mathf.Clamp(cameraPitch + pitch, -maxPitchAngle, maxPitchAngle);
        cameraHolder.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }


    private void HandleMovement()
    {
        float gravity = Physics.gravity.y * gravityMultiplier;

        bool isGrounded = cc.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;   

        if (jumpQueued && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
            jumpQueued = false;
        }
        else if (jumpQueued && !isGrounded)
        {
            jumpQueued = false;
        }

        verticalVelocity += gravity * Time.deltaTime;

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 targetVelocity = transform.right * moveInput.x * speed
                               + transform.forward * moveInput.y * speed;

        currentVelocityXZ = Vector3.MoveTowards(
            currentVelocityXZ,
            targetVelocity,
            groundAcceleration * Time.deltaTime);

        Vector3 motion = currentVelocityXZ + Vector3.up * verticalVelocity;
        cc.Move(motion * Time.deltaTime);
    }


    private void OnJump(InputAction.CallbackContext ctx)
    {
        jumpQueued = true;
    }

    private void SetCameraActive(bool active)
    {
        if (playerCamera == null) return;

        playerCamera.enabled = active;

        if (playerCamera.TryGetComponent<AudioListener>(out var listener))
            listener.enabled = active;
    }
}