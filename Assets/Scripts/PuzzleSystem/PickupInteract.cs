using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Inventory))]
public class PickupInteractor : NetworkBehaviour
{
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask itemLayer;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform raycastOrigin;
    [SerializeField] private GameObject pickupPrompt;
    [SerializeField] private GameObject heldByOtherPrompt;
    [SerializeField] private TMPro.TextMeshProUGUI interactPromptText;

    private Inventory inventory;
    private InputSystem_Actions inputActions;

    private SceneItem lookedAtItem;
    private IInteractable lookedAtInteractable;
    private NetworkIdentity lookedAtInteractableIdentity;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        if (raycastOrigin == null)
        {
            Camera childCam = GetComponentInChildren<Camera>();
            if (childCam != null)
                raycastOrigin = childCam.transform;
            else if (Camera.main != null)
                raycastOrigin = Camera.main.transform;
        }

        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();

        inputActions.Player.Interact.performed += OnInteractPerformed;
        inputActions.Player.Drop.performed += OnDropPerformed;

        inputActions.Player.ScrollHotbar.performed += OnScrollHotbar;

        inputActions.Player.HotbarSlot1.performed += _ => inventory.SetActiveSlot(0);
        inputActions.Player.HotbarSlot2.performed += _ => inventory.SetActiveSlot(1);
        inputActions.Player.HotbarSlot3.performed += _ => inventory.SetActiveSlot(2);
        inputActions.Player.HotbarSlot4.performed += _ => inventory.SetActiveSlot(3);

        Debug.Log($"Interact action bound: {inputActions.Player.Interact.name}");
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();

        if (inputActions != null)
        {
            inputActions.Player.Interact.performed -= OnInteractPerformed;
            inputActions.Player.Drop.performed -= OnDropPerformed;
            inputActions.Player.ScrollHotbar.performed -= OnScrollHotbar;
            inputActions.Player.Disable();
            inputActions.Dispose();
            inputActions = null;
        }
    }

    public void SetRaycastOrigin(Transform origin)
    {
        raycastOrigin = origin;
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log("Interact pressed");
        if (lookedAtInteractable != null)
            TryInteract();
        else
            TryPickUp();
    }

    private void OnDropPerformed(InputAction.CallbackContext ctx)
    {
        TryDrop();
    }

    private void OnScrollHotbar(InputAction.CallbackContext ctx)
    {
        float scrollValue = ctx.ReadValue<Vector2>().y;
        if (scrollValue == 0f) return;

        int currentSlot = inventory.GetActiveSlotIndex();
        int hotbarSize = 5; 

        int direction = scrollValue > 0 ? -1 : 1;
        int newSlot = (currentSlot + direction + hotbarSize) % hotbarSize;

        inventory.SetActiveSlot(newSlot);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        UpdateLookedAt();
    }

    public void TryPickUp()
    {
        Debug.Log($"TryPickUp called — item: {lookedAtItem}, isHeld: {lookedAtItem?.IsHeld}");
        if (lookedAtItem == null) return;
        if (lookedAtItem.IsHeld) return;
        inventory.CmdPickUp(lookedAtItem.netIdentity);
    }

    public void TryDrop()
    {
        Transform origin = GetRaycastOrigin();
        if (inventory.HasTwoHandedItem(out _))
            inventory.CmdDropTwoHandedItem(origin.position, origin.forward);
        else
            inventory.CmdDropActiveHotbarItem(origin.position, origin.forward);
    }

    public void TryInteract()
    {
        if (lookedAtInteractable == null || lookedAtInteractableIdentity == null) return;

        ItemDef heldItem = inventory.GetActiveItemDefinition();
        if (!lookedAtInteractable.CanInteract(heldItem)) return;

        CmdInteract(lookedAtInteractableIdentity);
    }

    [Command]
    private void CmdInteract(NetworkIdentity interactableIdentity)
    {
        if (interactableIdentity == null) return;

        IInteractable interactable = interactableIdentity.GetComponent<IInteractable>();
        if (interactable == null) return;

        ItemDef heldItem = inventory.GetActiveItemDefinition();
        if (!interactable.CanInteract(heldItem)) return;

        interactable.OnInteract(heldItem, netIdentity);
    }

    private void UpdateLookedAt()
    {
        Transform origin = GetRaycastOrigin();
        lookedAtItem = null;
        lookedAtInteractable = null;
        lookedAtInteractableIdentity = null;

        if (Physics.Raycast(origin.position, origin.forward, out RaycastHit interactHit, interactRange, interactableLayer))
        {
            IInteractable interactable = interactHit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                lookedAtInteractable = interactable;
                lookedAtInteractableIdentity = interactHit.collider.GetComponent<NetworkIdentity>();
            }
        }

        if (Physics.Raycast(origin.position, origin.forward, out RaycastHit itemHit, interactRange, itemLayer))
            lookedAtItem = itemHit.collider.GetComponent<SceneItem>();

        Debug.DrawRay(origin.position, origin.forward * interactRange,
            lookedAtItem != null ? Color.green : Color.red);

        UpdatePrompts();
    }

    private void UpdatePrompts()
    {
        ItemDef heldItem = inventory.GetActiveItemDefinition();

        bool showInteractPrompt = lookedAtInteractable != null
                               && lookedAtInteractable.CanInteract(heldItem);

        if (interactPromptText != null)
        {
            interactPromptText.gameObject.SetActive(showInteractPrompt);
            if (showInteractPrompt)
                interactPromptText.text = lookedAtInteractable.InteractPrompt;
        }

        bool lookingAtFreeItem = lookedAtItem != null && !lookedAtItem.IsHeld && !showInteractPrompt;
        bool lookingAtHeldItem = lookedAtItem != null && lookedAtItem.IsHeld && !showInteractPrompt;

        if (pickupPrompt != null) pickupPrompt.SetActive(lookingAtFreeItem);
        if (heldByOtherPrompt != null) heldByOtherPrompt.SetActive(lookingAtHeldItem);
    }

    private Transform GetRaycastOrigin() => raycastOrigin != null ? raycastOrigin : transform;

    private void OnDrawGizmosSelected()
    {
        Transform origin = GetRaycastOrigin();
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin.position, origin.forward * interactRange);
    }
}