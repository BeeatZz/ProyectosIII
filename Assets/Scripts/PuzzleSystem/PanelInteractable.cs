using Mirror;
using UnityEngine;

public class PanelInteractable : InteractableBase
{
    [SerializeField] private Collider panelCollider;
    [SerializeField] private Vector3 openLocalPosition;
    [SerializeField] private Quaternion openLocalRotation;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private UnityEngine.Events.UnityEvent onPanelOpened;

    [SyncVar(hook = nameof(OnOpenStateChanged))]
    private bool isOpen = false;

    public override string InteractPrompt => "Unscrew panel";

    [Server]
    protected override void OnInteractionSuccess(ItemDef heldItem, NetworkIdentity interactor)
    {
        isOpen = true;
    }

    private void OnOpenStateChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            StartCoroutine(AnimateToOpen());
            if (panelCollider != null) panelCollider.enabled = false;
            onPanelOpened?.Invoke();
        }
    }

    private System.Collections.IEnumerator AnimateToOpen()
    {
        float elapsed = 0f;
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
            transform.localPosition = Vector3.Lerp(startPos, openLocalPosition, t);
            transform.localRotation = Quaternion.Lerp(startRot, openLocalRotation, t);
            yield return null;
        }

        transform.localPosition = openLocalPosition;
        transform.localRotation = openLocalRotation;
    }
}