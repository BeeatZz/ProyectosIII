using Mirror;
using UnityEngine;

public abstract class InteractableBase : NetworkBehaviour, IInteractable
{
    [SerializeField] private string[] requiredTags;
    [SerializeField] private bool oneTimeInteraction = false;
    [SyncVar]
    private bool hasBeenInteracted = false;

    public virtual string InteractPrompt => "Interact";


    public virtual bool CanInteract(ItemDef heldItem)
    {
        if (oneTimeInteraction && hasBeenInteracted) return false;

        if (requiredTags == null || requiredTags.Length == 0) return true;

        if (heldItem == null) return false;

        foreach (string required in requiredTags)
            foreach (string itemTag in heldItem.interactionTags)
                if (required == itemTag) return true;

        return false;
    }

    public void OnInteract(ItemDef heldItem, NetworkIdentity interactor)
    {
        if (!CanInteract(heldItem)) return;

        if (oneTimeInteraction)
            hasBeenInteracted = true;

        OnInteractionSuccess(heldItem, interactor);
    }



    protected abstract void OnInteractionSuccess(ItemDef heldItem, NetworkIdentity interactor);
}