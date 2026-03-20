using Mirror;
using UnityEngine;

public class FuseBox : InteractableBase
{
    [SerializeField] private GameObject[] fuseVisuals = new GameObject[3]; 

    [SerializeField] private UnityEngine.Events.UnityEvent onFusePlaced;
    [SerializeField] private UnityEngine.Events.UnityEvent onAllFusesPlaced;

    [SyncVar(hook = nameof(OnFuseCountChanged))]
    private int fusesPlaced = 0;

    public override string InteractPrompt => fusesPlaced >= 3 ? "" : $"Insert fuse ({fusesPlaced}/3)";

    [Server]
    protected override void OnInteractionSuccess(ItemDef heldItem, NetworkIdentity interactor)
    {
        if (fusesPlaced >= 3) return;

        Inventory inventory = interactor.GetComponent<Inventory>();
        if (inventory == null) return;

        inventory.CmdConsumeActiveItem();
        fusesPlaced++;

        RpcOnFusePlaced(fusesPlaced);

        if (fusesPlaced >= 3)
            RpcOnAllFusesPlaced();
    }

    private void OnFuseCountChanged(int oldCount, int newCount)
    {
        for (int i = 0; i < fuseVisuals.Length; i++)
        {
            if (fuseVisuals[i] != null)
                fuseVisuals[i].SetActive(i < newCount);
        }
    }

    [ClientRpc]
    private void RpcOnFusePlaced(int count)
    {
        int index = count - 1;
        if (index >= 0 && index < fuseVisuals.Length && fuseVisuals[index] != null)
            fuseVisuals[index].SetActive(true);

        onFusePlaced?.Invoke();
    }

    [ClientRpc]
    private void RpcOnAllFusesPlaced()
    {
        onAllFusesPlaced?.Invoke();
    }
}