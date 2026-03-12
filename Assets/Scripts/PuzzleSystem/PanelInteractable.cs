using Mirror;
using UnityEngine;

public class PanelInteractable : InteractableBase
{
    [SerializeField] private Animator animator;
    [SerializeField] private string openAnimationTrigger = "Open";
    [SerializeField] private Collider panelCollider;
    [SerializeField] private UnityEngine.Events.UnityEvent onPanelOpened;

    public override string InteractPrompt => "Unscrew panel";

    [Server]
    protected override void OnInteractionSuccess(ItemDef heldItem, NetworkIdentity interactor)
    {
        if (animator != null)
            animator.SetTrigger(openAnimationTrigger);

        RpcDisableCollider();
    }

    [ClientRpc]
    private void RpcDisableCollider()
    {
        if (panelCollider != null)
            panelCollider.enabled = false;

        onPanelOpened?.Invoke();
    }
}