using Mirror;
using UnityEngine;

public interface IInteractable
{
    bool CanInteract(ItemDef heldItem);
    void OnInteract(ItemDef heldItem, NetworkIdentity interactor);
    string InteractPrompt { get; }
}
