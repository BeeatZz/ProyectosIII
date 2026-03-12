using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class SceneItem : NetworkBehaviour
{
    public ItemDef definition;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider itemCollider;
    [SerializeField] private float dropForwardForce = 2f;
    [SerializeField] private float dropUpwardForce  = 1f;
    [SyncVar(hook = nameof(OnHolderChanged))]
    private uint holderNetId = 0;

    public bool IsHeld => holderNetId != 0;

    [Server]
    public void PickUp(NetworkIdentity holder)
    {
        holderNetId = holder.netId;
        SetPhysicsEnabled(false);
        netIdentity.RemoveClientAuthority();
        netIdentity.AssignClientAuthority(holder.connectionToClient);
    }

    [Server]
    public void Drop(Vector3 dropPosition, Vector3 forwardDirection)
    {
        holderNetId = 0;
        netIdentity.RemoveClientAuthority();

        Vector3 dropVelocity = (forwardDirection.normalized * dropForwardForce)
                             + (Vector3.up * dropUpwardForce);

        RpcOnDropped(dropPosition, dropVelocity);
    }
    [ClientRpc]
    private void RpcOnDropped(Vector3 position, Vector3 velocity)
    {
        transform.SetParent(null);

        transform.position = position;
        SetPhysicsEnabled(true);

        if (rb != null)
            rb.linearVelocity = velocity;
    }

 
    private void OnHolderChanged(uint oldHolder, uint newHolder)
    {
        bool beingHeld = newHolder != 0;

        if (beingHeld)
        {
            SetPhysicsEnabled(false);
        }
        else if (isServer)
        {
            transform.SetParent(null);
            SetPhysicsEnabled(true);
        }
    }

    private void SetPhysicsEnabled(bool enabled)
    {
        if (rb != null)
        {
            rb.isKinematic = !enabled;
            rb.useGravity  = enabled;
        }
        if (itemCollider != null)
            itemCollider.enabled = enabled;
    }

    private void Reset()
    {
        rb           = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();
    }
}
