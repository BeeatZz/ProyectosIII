using System.Collections.Generic;
using Mirror;
using UnityEngine;
public class Inventory : NetworkBehaviour
{
    [SerializeField] private int hotbarSize = 5;
    [SerializeField] private Transform rightHandTransform;  
    [SerializeField] private Transform twoHandedTransform;  

    private readonly SyncList<uint> hotbarNetIds = new SyncList<uint>();

    [SyncVar(hook = nameof(OnTwoHandedItemChanged))]
    private uint twoHandedNetId = 0;

    private int activeSlotIndex = 0;

    public override void OnStartServer()
    {
        for (int i = 0; i < hotbarSize; i++)
            hotbarNetIds.Add(0);
    }

    public override void OnStartLocalPlayer()
    {
        hotbarNetIds.Callback += OnHotbarChanged;
    }

    [Command]
    public void CmdPickUp(NetworkIdentity itemIdentity)
    {
        if (itemIdentity == null) return;
        SceneItem item = itemIdentity.GetComponent<SceneItem>();
        if (item == null || item.IsHeld) return;

        ItemDef def = item.definition;
        if (def == null) return;

        switch (def.carryType)
        {
            case ItemCarryType.Hotbar:
                TryAddToHotbar(item, itemIdentity);
                break;

            case ItemCarryType.TwoHanded:
                TryPickUpTwoHanded(item, itemIdentity);
                break;
        }
    }

    [Command]
    public void CmdDropActiveHotbarItem(Vector3 dropPosition, Vector3 forwardDirection)
    {
        uint netId = hotbarNetIds[activeSlotIndex];
        if (netId == 0) return;

        if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity ni))
        {
            SceneItem item = ni.GetComponent<SceneItem>();
            item?.Drop(dropPosition, forwardDirection);
        }

        hotbarNetIds[activeSlotIndex] = 0;
    }

    [Command]
    public void CmdDropTwoHandedItem(Vector3 dropPosition, Vector3 forwardDirection)
    {
        if (twoHandedNetId == 0) return;

        if (NetworkServer.spawned.TryGetValue(twoHandedNetId, out NetworkIdentity ni))
        {
            SceneItem item = ni.GetComponent<SceneItem>();
            item?.Drop(dropPosition, forwardDirection);
        }

        twoHandedNetId = 0;
    }

    [Command]
    public void CmdConsumeActiveItem()
    {
        if (twoHandedNetId != 0)
        {
            if (NetworkServer.spawned.TryGetValue(twoHandedNetId, out NetworkIdentity twoHandedNI))
                NetworkServer.Destroy(twoHandedNI.gameObject);
            twoHandedNetId = 0;
            return;
        }

        uint netId = hotbarNetIds[activeSlotIndex];
        if (netId == 0) return;

        if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity ni))
            NetworkServer.Destroy(ni.gameObject);

        hotbarNetIds[activeSlotIndex] = 0;
    }

    public void SetActiveSlot(int index)
    {
        if (index < 0 || index >= hotbarSize) return;
        activeSlotIndex = index;
        UpdateHeldItemVisual();
    }


    public bool HasItemInHand(out SceneItem item)
    {
        item = null;
        uint netId = hotbarNetIds[activeSlotIndex];
        if (netId == 0) return false;
        if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity ni))
            item = ni.GetComponent<SceneItem>();
        return item != null;
    }

    public bool HasTwoHandedItem(out SceneItem item)
    {
        item = null;
        if (twoHandedNetId == 0) return false;
        if (NetworkServer.spawned.TryGetValue(twoHandedNetId, out NetworkIdentity ni))
            item = ni.GetComponent<SceneItem>();
        return item != null;
    }

    /// <summary>
    /// Returns the ItemDefinition of the item currently "in hand"
    /// (active hotbar slot, or two-handed item — two-handed takes priority).
    /// </summary>
    public ItemDef GetActiveItemDefinition()
    {
        if (HasTwoHandedItem(out SceneItem twoH)) return twoH.definition;
        if (HasItemInHand(out SceneItem hotbarItem)) return hotbarItem.definition;
        return null;
    }

    [Server]
    private void TryAddToHotbar(SceneItem item, NetworkIdentity itemIdentity)
    {
        for (int i = 0; i < hotbarNetIds.Count; i++)
        {
            if (hotbarNetIds[i] == 0)
            {
                item.PickUp(netIdentity);
                hotbarNetIds[i] = itemIdentity.netId;
                return;
            }
        }
    }

    [Server]
    private void TryPickUpTwoHanded(SceneItem item, NetworkIdentity itemIdentity)
    {
        if (twoHandedNetId != 0)
        {
            return;
        }
        item.PickUp(netIdentity);
        twoHandedNetId = itemIdentity.netId;
    }

    private void OnTwoHandedItemChanged(uint oldId, uint newId)
    {
        AttachItemToTransform(newId, twoHandedTransform);
    }

    private void OnHotbarChanged(SyncList<uint>.Operation op, int index, uint oldId, uint newId)
    {
        if (index == activeSlotIndex)
            UpdateHeldItemVisual();
    }

    private void UpdateHeldItemVisual()
    {
        uint netId = hotbarNetIds[activeSlotIndex];
        AttachItemToTransform(netId, rightHandTransform);
    }

    private void AttachItemToTransform(uint netId, Transform parent)
    {
        if (netId == 0 || parent == null) return;

        if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity ni))
        {
            ni.transform.SetParent(parent, false);
            ni.transform.localPosition = Vector3.zero;
            ni.transform.localRotation = Quaternion.identity;
        }
    }
}
