using System.Collections;
using UnityEngine;
using Mirror;

public class PuzzleManager : NetworkBehaviour
{
    public GameObject[] puzzleObjects;
    public SkinnedMeshRenderer[] blendMeshes;


    public void ActivateObject(int objPos)
    {
        CmdSetObjectActive(objPos, true);
    }

    public void DeactivateObject(int objPos)
    {
        CmdSetObjectActive(objPos, false);
    }

    [Command(requiresAuthority = false)]
    private void CmdSetObjectActive(int objPos, bool active)
    {
        RpcSetObjectActive(objPos, active);
    }

    [ClientRpc]
    private void RpcSetObjectActive(int objPos, bool active)
    {
        puzzleObjects[objPos].SetActive(active);
    }


    // GENERICO, HAY QUE CREAR UNO PARA CADA BLENDSHAPE / DURACION, EJEMPLO A CONTINUACION
    public void AnimateBlendShape(int meshIndex, int shapeIndex, float from, float to, float duration)
    {
        CmdAnimateBlendShape(meshIndex, shapeIndex, from, to, duration);
    }

    public void AnimateElevatorDoor()
    {
        AnimateBlendShape(0, 0, 0f, 100f, 2f);
    }

    [Command(requiresAuthority = false)]
    private void CmdAnimateBlendShape(int meshIndex, int shapeIndex, float from, float to, float duration)
    {
        RpcAnimateBlendShape(meshIndex, shapeIndex, from, to, duration);
    }

    [ClientRpc]
    private void RpcAnimateBlendShape(int meshIndex, int shapeIndex, float from, float to, float duration)
    {
        StartCoroutine(AnimateBlendShapeCoroutine(blendMeshes[meshIndex], shapeIndex, from, to, duration));
    }

    private IEnumerator AnimateBlendShapeCoroutine(SkinnedMeshRenderer smr, int shapeIndex, float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float value = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            smr.SetBlendShapeWeight(shapeIndex, value);
            yield return null;
        }

        smr.SetBlendShapeWeight(shapeIndex, to);
    }
}