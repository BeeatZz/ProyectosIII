using Mirror;
using UnityEngine;

public enum WireType
{
    Straight,
    Corner,
    TCross,
    Cross
}

public class WireTile : NetworkBehaviour
{
    private static readonly int[] BaseConnections =
    {
        0b0101,
        0b1100,
        0b1101,
        0b1111,
    };

    [SerializeField] private WireType wireType;
    [SerializeField] private float lerpSpeed = 3f;

    [SyncVar(hook = nameof(OnRotationIndexChanged))]
    private int rotationIndex = 0;

    private Quaternion targetRotation = Quaternion.identity;
    private WirePuzzleController controller;
    private bool initialized = false;

    public void Init(WirePuzzleController owner, int startingRotationIndex = -1)
    {
        if (initialized) return;
        initialized = true;

        controller = owner;

        if (isServer && startingRotationIndex >= 0)
        {
            rotationIndex = startingRotationIndex;
        }

        targetRotation = IndexToRotation(rotationIndex);
        transform.localRotation = targetRotation;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (isServer) return; 

        targetRotation = IndexToRotation(rotationIndex);
        transform.localRotation = targetRotation;
    }

    private void Update()
    {
        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * lerpSpeed
        );
    }

    public void OnClicked()
    {
        if (!controller.IsPuzzleActive) return;
        CmdRotate();
    }

    [Command(requiresAuthority = false)]
    private void CmdRotate()
    {
        rotationIndex = (rotationIndex + 1) % 4;
        targetRotation = IndexToRotation(rotationIndex);
        controller.CheckSolution();
    }

    private void OnRotationIndexChanged(int oldVal, int newVal)
    {
        targetRotation = IndexToRotation(newVal);
    }

    public int GetConnections()
    {
        return RotateMask(BaseConnections[(int)wireType], rotationIndex);
    }

    public bool ConnectsIn(int direction)
    {
        return (GetConnections() & (1 << direction)) != 0;
    }

    private int RotateMask(int mask, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            int newMask = 0;
            if ((mask & 1) != 0) newMask |= 2;
            if ((mask & 2) != 0) newMask |= 4;
            if ((mask & 4) != 0) newMask |= 8;
            if ((mask & 8) != 0) newMask |= 1;
            mask = newMask;
        }
        return mask;
    }

    private Quaternion IndexToRotation(int index)
    {
        return Quaternion.Euler(index * 90f, 0f, 0f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        int steps;
        if (Application.isPlaying)
        {
            steps = rotationIndex;
        }
        else
        {
            float angle = transform.localEulerAngles.x;
            steps = Mathf.RoundToInt(angle / 90f) % 4;
            if (steps < 0) steps += 4;
        }

        int mask = RotateMask(BaseConnections[(int)wireType], steps);
        float arrowLength = 0.4f;
        Vector3 pos = transform.position;

        DrawGizmoArrow(pos, transform.TransformDirection(Vector3.forward), (mask & 1) != 0, arrowLength);
        DrawGizmoArrow(pos, transform.TransformDirection(Vector3.up), (mask & 2) != 0, arrowLength);
        DrawGizmoArrow(pos, transform.TransformDirection(Vector3.back), (mask & 4) != 0, arrowLength);
        DrawGizmoArrow(pos, transform.TransformDirection(Vector3.down), (mask & 8) != 0, arrowLength);
    }

    private void DrawGizmoArrow(Vector3 origin, Vector3 direction, bool open, float length)
    {
        Gizmos.color = open ? Color.green : Color.red;
        Vector3 end = origin + direction * length;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawSphere(end, 0.05f);
    }
#endif
}