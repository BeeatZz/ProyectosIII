using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteInEditMode]
public class Zoom : MonoBehaviour
{
    Camera cam;

    public float defaultFOV = 60f;
    public float maxZoomFOV = 15f;

    [Range(0, 1)]
    public float currentZoom;

    public float sensitivity = 1f;

    void Awake()
    {
        cam = GetComponent<Camera>();

        if (cam != null)
        {
            defaultFOV = cam.fieldOfView;
        }
    }

    void Update()
    {
        if (cam == null)
            return;

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;

            // Ajustado porque el nuevo sistema devuelve valores más grandes
            currentZoom += scroll * sensitivity * 0.001f;
            currentZoom = Mathf.Clamp01(currentZoom);

            cam.fieldOfView = Mathf.Lerp(defaultFOV, maxZoomFOV, currentZoom);
        }
    }
}