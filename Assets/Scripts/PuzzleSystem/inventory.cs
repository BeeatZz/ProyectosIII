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

    public Transform GetTransformByName(string name)
    {
        return name switch
        {
            "RightHand" => rightHandTransform,
            "TwoHanded" => twoHandedTransform,
            _ => null
        };
    }

    public int GetActiveSlotIndex() => activeSlotIndex;

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
                TryPickUpHotbar(item, itemIdentity);
                break;

            case ItemCarryType.TwoHanded:
                TryPickUpTwoHanded(item, itemIdentity);
                break;
        }
    }

    [Server]
    private void TryPickUpHotbar(SceneItem item, NetworkIdentity itemIdentity)
    {
        // Two-handed active → drop it first, then pick up hotbar (if space exists)
        if (twoHandedNetId != 0)
        {
            // Check there's a free hotbar slot before committing to the drop
            int freeSlot = FindFreeHotbarSlot();
            if (freeSlot == -1) return; // Hotbar full, block pickup

            // Drop the two-handed item
            if (NetworkServer.spawned.TryGetValue(twoHandedNetId, out NetworkIdentity twoHandedNI))
            {
                SceneItem twoHandedItem = twoHandedNI.GetComponent<SceneItem>();
                twoHandedItem?.Drop(twoHandedNI.transform.position, Vector3.zero);
            }
            twoHandedNetId = 0;

            // Now pick up the hotbar item
            item.PickUp(netIdentity, "RightHand");
            hotbarNetIds[freeSlot] = itemIdentity.netId;
            return;
        }

        // No two-handed active → just find a free slot
        int slot = FindFreeHotbarSlot();
        if (slot == -1) return; // Hotbar full, block pickup

        item.PickUp(netIdentity, "RightHand");
        hotbarNetIds[slot] = itemIdentity.netId;
    }

    [Server]
    private void TryPickUpTwoHanded(SceneItem item, NetworkIdentity itemIdentity)
    {
        // Two-handed already held → drop it first
        if (twoHandedNetId != 0)
        {
            if (NetworkServer.spawned.TryGetValue(twoHandedNetId, out NetworkIdentity twoHandedNI))
            {
                SceneItem twoHandedItem = twoHandedNI.GetComponent<SceneItem>();
                twoHandedItem?.Drop(twoHandedNI.transform.position, Vector3.zero);
            }
            twoHandedNetId = 0;
        }

        // Hotbar item active → it just stays silently in its slot, two-handed becomes active on top
        item.PickUp(netIdentity, "TwoHanded");
        twoHandedNetId = itemIdentity.netId;
    }

    [Server]
    private int FindFreeHotbarSlot()
    {
        for (int i = 0; i < hotbarNetIds.Count; i++)
            if (hotbarNetIds[i] == 0) return i;
        return -1;
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

    public ItemDef GetActiveItemDefinition()
    {
        if (HasTwoHandedItem(out SceneItem twoH)) return twoH.definition;
        if (HasItemInHand(out SceneItem hotbarItem)) return hotbarItem.definition;
        return null;
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