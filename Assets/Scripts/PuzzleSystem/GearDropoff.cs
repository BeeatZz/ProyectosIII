using Mirror;
using UnityEngine;

public class GearDropoff : InteractableBase
{
    [SerializeField] private GameObject emptyVisual;   
    [SerializeField] private GameObject filledVisual; 
    [SerializeField] private UnityEngine.Events.UnityEvent onGearPlaced;

    public override string InteractPrompt => "Place gear";

    [Server]
    protected override void OnInteractionSuccess(ItemDef heldItem, NetworkIdentity interactor)
    {
        Inventory inventory = interactor.GetComponent<Inventory>();
        if (inventory == null) return;

        inventory.CmdConsumeActiveItem();

        RpcOnGearPlaced();
    }

    [ClientRpc]
    private void RpcOnGearPlaced()
    {
        if (emptyVisual != null)  emptyVisual.SetActive(false);
        if (filledVisual != null) filledVisual.SetActive(true);

        onGearPlaced?.Invoke();
    }
}
