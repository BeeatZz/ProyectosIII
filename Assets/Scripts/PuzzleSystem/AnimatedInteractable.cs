using Mirror;
using UnityEngine;

public class AnimatedInteractable : InteractableBase
{
    [SerializeField] private Animator[] targetAnimators;
    [SerializeField] private string animationTriggerName = "Open";
    [SerializeField] private bool allowInteraction = true;
    [SerializeField] private bool requireItem = true;
    public override bool CanInteract(ItemDef heldItem)
    {
        if (!allowInteraction) return false;

        if (!requireItem) return base.CanInteract(null) == false
            ? false  
            : true;

        return base.CanInteract(heldItem);
    }

    protected override void OnInteractionSuccess(ItemDef heldItem, NetworkIdentity interactor)
    {
        RpcPlayAllAnimations();
    }

    [ClientRpc]
    private void RpcPlayAllAnimations()
    {
        if (targetAnimators == null || targetAnimators.Length == 0) return;
        foreach (Animator anim in targetAnimators)
        {
            if (anim != null)
                anim.SetTrigger(animationTriggerName);
        }
    }

    public void SetAllowInteraction(bool value) => allowInteraction = value;
    public void SetRequireItem(bool value) => requireItem = value;
}