using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    [SerializeField] private SlotUI[] slots;
    [SerializeField] private TMP_Text itemNameLabel;
    [SerializeField] private float displayDuration = 1.5f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color normalSlotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color activeSlotColor = new Color(1f, 0.85f, 0f, 0.95f);

    private Inventory inventory;
    private Coroutine fadeCoroutine;
    private Color labelColor;

    /// <summary>
    /// Call this only for the local player's Inventory.
    /// Pass isLocalPlayer = false to silently ignore remote players.
    /// </summary>
    public void Init(Inventory inv, bool isLocalPlayer)
    {
        if (!isLocalPlayer) return;

        if (inventory != null)
        {
            inventory.OnHotbarUpdated -= Redraw;
            inventory.OnActiveSlotChanged -= OnActiveSlotChanged;
        }

        inventory = inv;
        inventory.OnHotbarUpdated += Redraw;
        inventory.OnActiveSlotChanged += OnActiveSlotChanged;

        if (itemNameLabel != null)
        {
            labelColor = itemNameLabel.color;
            labelColor.a = 0f;
            itemNameLabel.color = labelColor;
        }

        Redraw();
    }

    private void OnDestroy()
    {
        if (inventory == null) return;
        inventory.OnHotbarUpdated -= Redraw;
        inventory.OnActiveSlotChanged -= OnActiveSlotChanged;
    }

    private void Redraw()
    {
        if (inventory == null || slots == null) return;

        int active = inventory.GetActiveSlotIndex();

        for (int i = 0; i < slots.Length; i++)
        {
            ItemDef def = inventory.GetItemDefAtSlot(i);
            bool hasItem = def != null;

            if (slots[i].icon != null)
            {
                slots[i].icon.sprite = hasItem ? def.icon : null;
                slots[i].icon.enabled = hasItem;
            }

            if (slots[i].background != null)
                slots[i].background.color = (i == active) ? activeSlotColor : normalSlotColor;
        }
    }

    private void OnActiveSlotChanged(int newIndex)
    {
        if (slots == null || inventory == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].background != null)
                slots[i].background.color = (i == newIndex) ? activeSlotColor : normalSlotColor;
        }

        ItemDef def = inventory.GetItemDefAtSlot(newIndex);
        ShowItemName(def != null ? def.displayName : string.Empty);
    }

    private void ShowItemName(string itemName)
    {
        if (itemNameLabel == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (string.IsNullOrEmpty(itemName))
        {
            SetLabelAlpha(0f);
            return;
        }

        itemNameLabel.text = itemName;
        fadeCoroutine = StartCoroutine(FadeLabel());
    }

    private IEnumerator FadeLabel()
    {
        SetLabelAlpha(1f);
        yield return new WaitForSeconds(displayDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetLabelAlpha(1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        SetLabelAlpha(0f);
        fadeCoroutine = null;
    }

    private void SetLabelAlpha(float alpha)
    {
        labelColor.a = alpha;
        itemNameLabel.color = labelColor;
    }

    [System.Serializable]
    public struct SlotUI
    {
        public Image background;
        public Image icon;
    }
}