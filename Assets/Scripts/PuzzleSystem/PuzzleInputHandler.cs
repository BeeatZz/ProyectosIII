using UnityEngine;
using UnityEngine.Events;
using System;

public class PuzzleInputHandler : MonoBehaviour
{
    public static PuzzleInputHandler Local { get; private set; }

    [SerializeField] private float raycastDistance = 50f;
    [SerializeField] private LayerMask tileLayer;
    [SerializeField] private KeyCode exitKey = KeyCode.E;

    public UnityEvent onEnterPuzzle;
    public UnityEvent onExitPuzzle;

    private Camera activeCamera;
    private bool isActive = false;
    private Action onExitCallback;

    private void Awake() => Local = this;

    public void EnterPuzzle(Camera cam, Action exitCallback)
    {
        activeCamera = cam;
        isActive = true;
        onExitCallback = exitCallback;
        onEnterPuzzle.Invoke();
    }

    public void ExitPuzzle()
    {
        isActive = false;
        activeCamera = null;
        onExitPuzzle.Invoke();
        onExitCallback?.Invoke();
        onExitCallback = null;
    }

    private void Update()
    {
        if (!isActive) return;

        if (Input.GetKeyDown(exitKey))
        {
            ExitPuzzle();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, tileLayer))
                if (hit.collider.TryGetComponent(out WireTile tile))
                    tile.OnClicked();
        }
    }
}