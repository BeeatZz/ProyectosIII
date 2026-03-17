using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class SceneItem : NetworkBehaviour
{
    public ItemDef definition;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider itemCollider;
    [SerializeField] private Renderer[] itemRenderers;

    [SyncVar(hook = nameof(OnHolderChanged))]
    private uint holderNetId = 0;

    public bool IsHeld => holderNetId != 0;

    [Server]
    public void PickUp(NetworkIdentity holder, string targetParentName)
    {
        holderNetId = holder.netId;
        SetPhysicsEnabled(false);
        netIdentity.RemoveClientAuthority();
        netIdentity.AssignClientAuthority(holder.connectionToClient);
        RpcOnPickedUp(holder.netId, targetParentName);
    }

    [ClientRpc]
    private void RpcOnPickedUp(uint holderId, string parentName)
    {
        if (NetworkClient.spawned.TryGetValue(holderId, out NetworkIdentity playerIdentity))
        {
            Inventory inv = playerIdentity.GetComponent<Inventory>();
            Transform targetParent = inv.GetTransformByName(parentName);
            if (targetParent != null)
            {
                transform.SetParent(targetParent, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }
        SetPhysicsEnabled(false); 
        SetRendererEnabled(false);
        ToggleNetworkSync(false);
    }

    [Server]
    public void Drop(Vector3 dropPosition, Vector3 forwardDirection)
    {
        netIdentity.RemoveClientAuthority();
        holderNetId = 0;
        transform.SetParent(null);
        transform.position = dropPosition;
        SetPhysicsEnabled(true);
        ToggleNetworkSync(true);
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.WakeUp();
            rb.linearVelocity = (forwardDirection.normalized * 2f) + (Vector3.up * 2f);
        }
        RpcOnDropped(dropPosition, forwardDirection);
    }

    [ClientRpc]
    private void RpcOnDropped(Vector3 position, Vector3 forward)
    {
        transform.SetParent(null);
        transform.position = position;
        SetRendererEnabled(true);
        SetPhysicsEnabled(true);
        ToggleNetworkSync(true);
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.WakeUp();
            rb.linearVelocity = (forward.normalized * 2f) + (Vector3.up * 2f);
        }
    }

    public void SetPhysicsState(bool enabled) => SetPhysicsEnabled(enabled);

    private void ToggleNetworkSync(bool enabled)
    {
        var nt = GetComponent<NetworkTransformUnreliable>();
        if (nt != null) nt.enabled = enabled;
        var nrb = GetComponent<NetworkRigidbodyUnreliable>();
        if (nrb != null) nrb.enabled = enabled;
    }

    private void SetPhysicsEnabled(bool enabled)
    {
        if (rb != null) { rb.isKinematic = !enabled; rb.useGravity = enabled; }
        if (itemCollider != null) itemCollider.enabled = enabled;
    }

    private void SetRendererEnabled(bool enabled)
    {
        foreach (Renderer r in itemRenderers) if (r != null) r.enabled = enabled;
    }

    private void OnHolderChanged(uint old, uint newId) { }
}