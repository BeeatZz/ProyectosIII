using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

/// <summary>
/// Server-authoritative FPS controller for Mirror.
///
/// Design:
///   LOCAL PLAYER   — reads input, runs movement locally for immediate response
///                    (client prediction), then sends inputs to the server.
///   SERVER         — re-simulates movement from received inputs and writes the
///                    authoritative result into SyncVars.
///   REMOTE CLIENTS — interpolate toward SyncVar values; CharacterController
///                    is disabled because they are never simulated locally.
///
/// Setup checklist:
///   • Attach CharacterController, NetworkIdentity, PlayerInput to this GameObject.
///   • Create an Input Action Asset with actions: Move (Vector2), Look (Vector2),
///     Jump (Button), Crouch (Button), Sprint (Button). Reference it in PlayerInput.
///   • Create an empty child GameObject at eye level and assign it to cameraMount.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : NetworkBehaviour
{
    // ─── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Empty transform at eye level. The camera is spawned here for the local player.")]
    public Transform cameraMount;

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float crouchSpeed = 2.5f;
    public float sprintSpeed = 9f;
    public float jumpHeight = 2f;
    [Tooltip("Downward gravitational acceleration (m/s²).")]
    public float gravity = 20f;

    [Header("Look")]
    public float mouseSensitivity = 2f;
    public float pitchClamp = 80f;

    [Header("Crouch")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 10f;

    [Header("Networking")]
    [Tooltip("Lerp speed for smoothing remote players on non-owning clients.")]
    public float remoteInterpSpeed = 20f;

    // ─── Components ────────────────────────────────────────────────────────────

    private CharacterController _cc;
    private Camera _cam;

    // ─── Input (local player only) ─────────────────────────────────────────────

    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _crouchAction;
    private InputAction _sprintAction;

    // Stored delegates allow clean unsubscription in OnDestroy
    private System.Action<InputAction.CallbackContext>
        _cbMovePerf, _cbMoveCan,
        _cbLookPerf, _cbLookCan,
        _cbJumpPerf,
        _cbCrouchPerf, _cbCrouchCan,
        _cbSprintPerf, _cbSprintCan;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _jumpQueued;   // set on press, consumed after one Simulate call
    private bool _crouching;
    private bool _sprinting;

    // Input System fires performed callbacks immediately on action.Enable() if
    // the physical key is held. Block all input for the first few frames so a
    // held jump key or spurious event can't launch the player on spawn.
    private bool _inputReady;
    private int _spawnFrame;

    // ─── Simulation state ──────────────────────────────────────────────────────

    private float _yVel;           // vertical velocity (m/s), positive = downward
    private float _pitch;          // camera pitch in degrees, local only
    private float _capsuleHeight;  // current lerped capsule height, all clients

    // ─── SyncVars (server → all clients) ───────────────────────────────────────

    [SyncVar] private Vector3 _syncPos;
    [SyncVar] private float _syncYaw;
    [SyncVar] private bool _syncCrouching;

    // ══════════════════════════════════════════════════════════════════════════
    //  Unity / Mirror lifecycle
    // ══════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _capsuleHeight = standHeight;

        // Set correct height and center immediately so the CharacterController
        // never overlaps with the ground on the first frame. If center stays at
        // (0,0,0) — the Unity default — the lower half of the capsule is buried
        // and the physics solver ejects the player upward on the very first tick.
        _cc.height = _capsuleHeight;
        _cc.center = new Vector3(0f, _capsuleHeight * 0.5f, 0f);
    }

    public override void OnStartLocalPlayer()
    {
        _moveInput = Vector2.zero;
        _lookInput = Vector2.zero;
        _jumpQueued = false;
        _yVel = 0f;
        _inputReady = false;
        _spawnFrame = Time.frameCount;

        // Push the CC downward slightly on spawn so it registers as grounded
        // on the very first frame. Without this, isGrounded=false on frame 1
        // and the grounding clamp never fires.
        _cc.Move(Vector3.down * 0.1f);

        SpawnCamera();
        BindInput();
    }

    /// <summary>Called on every client (including non-owners).</summary>
    public override void OnStartClient()
    {
        if (!isLocalPlayer)
            _cc.enabled = false;   // remote players: driven by SyncVars only
    }

    void OnDestroy()
    {
        UnbindInput();

        if (isLocalPlayer)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Update()
    {
        if (isLocalPlayer)
            LocalUpdate();
        else
            RemoteUpdate();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Camera
    // ══════════════════════════════════════════════════════════════════════════

    void SpawnCamera()
    {
        if (cameraMount == null)
        {
            Debug.LogError("[PlayerController] cameraMount is not assigned!", this);
            return;
        }

        var go = new GameObject("PlayerCamera");
        _cam = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();

        go.transform.SetParent(cameraMount, worldPositionStays: false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Input binding / unbinding
    // ══════════════════════════════════════════════════════════════════════════

    void BindInput()
    {
        var pi = GetComponent<PlayerInput>();
        if (pi == null || pi.actions == null)
        {
            Debug.LogError("[PlayerController] PlayerInput or Action Asset is missing.", this);
            return;
        }

        var a = pi.actions;
        _moveAction = a.FindAction("Move", throwIfNotFound: true);
        _lookAction = a.FindAction("Look", throwIfNotFound: true);
        _jumpAction = a.FindAction("Jump", throwIfNotFound: true);
        _crouchAction = a.FindAction("Crouch", throwIfNotFound: true);
        _sprintAction = a.FindAction("Sprint", throwIfNotFound: true);

        _cbMovePerf = ctx => { if (_inputReady) _moveInput = ctx.ReadValue<Vector2>(); };
        _cbMoveCan = _ => { if (_inputReady) _moveInput = Vector2.zero; };
        _cbLookPerf = ctx => { if (_inputReady) _lookInput = ctx.ReadValue<Vector2>(); };
        _cbLookCan = _ => { if (_inputReady) _lookInput = Vector2.zero; };
        _cbJumpPerf = _ => { if (_inputReady) _jumpQueued = true; };
        _cbCrouchPerf = _ => { if (_inputReady) _crouching = true; };
        _cbCrouchCan = _ => { if (_inputReady) _crouching = false; };
        _cbSprintPerf = _ => { if (_inputReady) _sprinting = true; };
        _cbSprintCan = _ => { if (_inputReady) _sprinting = false; };

        _moveAction.performed += _cbMovePerf;
        _moveAction.canceled += _cbMoveCan;
        _lookAction.performed += _cbLookPerf;
        _lookAction.canceled += _cbLookCan;
        _jumpAction.performed += _cbJumpPerf;
        _crouchAction.performed += _cbCrouchPerf;
        _crouchAction.canceled += _cbCrouchCan;
        _sprintAction.performed += _cbSprintPerf;
        _sprintAction.canceled += _cbSprintCan;

        _moveAction.Enable();
        _lookAction.Enable();
        _jumpAction.Enable();
        _crouchAction.Enable();
        _sprintAction.Enable();
    }

    void UnbindInput()
    {
        if (_moveAction != null) { _moveAction.performed -= _cbMovePerf; _moveAction.canceled -= _cbMoveCan; }
        if (_lookAction != null) { _lookAction.performed -= _cbLookPerf; _lookAction.canceled -= _cbLookCan; }
        if (_jumpAction != null) _jumpAction.performed -= _cbJumpPerf;
        if (_crouchAction != null) { _crouchAction.performed -= _cbCrouchPerf; _crouchAction.canceled -= _cbCrouchCan; }
        if (_sprintAction != null) { _sprintAction.performed -= _cbSprintPerf; _sprintAction.canceled -= _cbSprintCan; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Local player tick
    // ══════════════════════════════════════════════════════════════════════════

    void LocalUpdate()
    {
        // Unlock input after 3 frames — suppresses spurious callbacks that fire
        // immediately on action.Enable() (e.g. a held jump key at spawn time).
        if (!_inputReady)
        {
            if (Time.frameCount >= _spawnFrame + 3)
                _inputReady = true;
            else
                return;
        }

        HandleLook();
        UpdateCapsuleHeight(_crouching);
        Simulate(_moveInput, _jumpQueued, _crouching, _sprinting, ref _yVel);

        // Capture jump flag BEFORE clearing it so the Command can carry it
        bool jumpThisTick = _jumpQueued;
        _jumpQueued = false;

        // On a listen-server (host) Simulate() already ran above with full
        // authority — write SyncVars directly to skip the Command round-trip
        // and guarantee Simulate is never called a second time this tick.
        if (isServer)
        {
            _syncPos = transform.position;
            _syncYaw = transform.eulerAngles.y;
            _syncCrouching = _crouching;
        }
        else
        {
            // Send inputs to server for authoritative re-simulation
            CmdSendInput(_moveInput, jumpThisTick, _crouching, _sprinting,
                         transform.eulerAngles.y);
        }
    }

    void HandleLook()
    {
        if (_cam == null) return;

        transform.Rotate(Vector3.up * (_lookInput.x * mouseSensitivity), Space.World);

        _pitch = Mathf.Clamp(_pitch - _lookInput.y * mouseSensitivity,
                              -pitchClamp, pitchClamp);
        _cam.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Movement simulation  (identical logic on local client AND server)
    // ══════════════════════════════════════════════════════════════════════════

    void Simulate(Vector2 move, bool jump, bool crouch, bool sprint, ref float yVel)
    {
        bool grounded = _cc.isGrounded;

        if (grounded && yVel < 0)
        {
            yVel = -2f; // Keep grounded
        }

        if (jump && grounded)
        {
            yVel = Mathf.Sqrt(jumpHeight * 2f * gravity);
        }

        // Only apply gravity when in the air
        if (!grounded)
        {
            yVel -= gravity * Time.deltaTime;
        }

        Vector3 dir = transform.right * move.x + transform.forward * move.y;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        float speed = crouch ? crouchSpeed : sprint ? sprintSpeed : walkSpeed;

        Vector3 motion = dir * speed;
        motion.y = yVel;
        _cc.Move(motion * Time.deltaTime);
    }
    // ══════════════════════════════════════════════════════════════════════════
    //  Crouch / capsule height  (runs on all clients for smooth visuals)
    // ══════════════════════════════════════════════════════════════════════════

    void UpdateCapsuleHeight(bool wantCrouch)
    {
        float target = wantCrouch ? crouchHeight : standHeight;

        // Before standing up, verify there is clearance above the player
        if (!wantCrouch && _capsuleHeight < standHeight - 0.05f)
        {
            float r = _cc.radius * 0.9f;
            float top = transform.position.y + standHeight - r;
            if (Physics.CheckSphere(new Vector3(transform.position.x, top, transform.position.z), r))
                target = crouchHeight;   // blocked — stay crouched
        }

        _capsuleHeight = Mathf.Lerp(_capsuleHeight, target,
                                     Time.deltaTime * crouchTransitionSpeed);

        // Only write to the CharacterController when it is actually enabled
        if (_cc.enabled)
        {
            _cc.height = _capsuleHeight;
            _cc.center = new Vector3(0f, _capsuleHeight * 0.5f, 0f);
        }

        if (cameraMount != null)
        {
            var lp = cameraMount.localPosition;
            lp.y = Mathf.Lerp(lp.y, _capsuleHeight - 0.15f,
                                  Time.deltaTime * crouchTransitionSpeed);
            cameraMount.localPosition = lp;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Network  (Command + remote update)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Owner → Server: deliver this frame's inputs.
    /// The server re-simulates movement and pushes authoritative state via SyncVars.
    /// Only called by non-host clients — hosts write SyncVars directly in LocalUpdate.
    /// </summary>
    [Command(requiresAuthority = true)]
    void CmdSendInput(Vector2 move, bool jump, bool crouch, bool sprint, float yaw)
    {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        UpdateCapsuleHeight(crouch);
        Simulate(move, jump, crouch, sprint, ref _yVel);

        _syncPos = transform.position;
        _syncYaw = yaw;
        _syncCrouching = crouch;
    }

    /// <summary>
    /// Non-owning clients smoothly interpolate the player toward the server-
    /// authoritative position received via SyncVars.
    /// </summary>
    void RemoteUpdate()
    {
        float t = Time.deltaTime * remoteInterpSpeed;
        transform.position = Vector3.Lerp(transform.position, _syncPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation,
                                               Quaternion.Euler(0f, _syncYaw, 0f), t);

        // Visual crouch height for hit-box accuracy on remote clients
        float target = _syncCrouching ? crouchHeight : standHeight;
        _capsuleHeight = Mathf.Lerp(_capsuleHeight, target,
                                     Time.deltaTime * crouchTransitionSpeed);

        if (cameraMount != null)
        {
            var lp = cameraMount.localPosition;
            lp.y = Mathf.Lerp(lp.y, _capsuleHeight - 0.15f,
                                  Time.deltaTime * crouchTransitionSpeed);
            cameraMount.localPosition = lp;
        }
    }
}