using System.Collections;
using Mirror;
using UnityEngine;

public class WirePuzzleInteractable : InteractableBase
{
    [SerializeField] private WirePuzzleController puzzle;
    [SerializeField] private PuzzleCameraRig cameraRig;
    [SerializeField] private float cameraLerpSpeed = 3f;

    public override string InteractPrompt => "Inspect wiring";

    protected override void OnInteractionSuccess(ItemDef heldItem, NetworkIdentity interactor)
    {
        if (puzzle == null)
        {
            return;
        }

        if (!puzzle.isUnlocked) return;

        //puzzle.SetActive(true);
        RpcEnterPuzzle(interactor.connectionToClient);
    }

    [TargetRpc]
    private void RpcEnterPuzzle(NetworkConnectionToClient target)
    {
        MultiplayerController player = NetworkClient.localPlayer.GetComponent<MultiplayerController>();

        if (player == null)
        {
            return;
        }

        if (player.playerCamera == null)
        {
            return;
        }

        if (cameraRig == null)
        {
            return;
        }

        StartCoroutine(LerpToRig(player.playerCamera));
    }

    private IEnumerator LerpToRig(Camera cam)
    {
        Transform originalParent = cam.transform.parent;
        Vector3 originalLocalPos = cam.transform.localPosition;
        Quaternion originalLocalRot = cam.transform.localRotation;

        cam.transform.SetParent(null, worldPositionStays: true);

        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * cameraLerpSpeed;
            float st = Mathf.SmoothStep(0f, 1f, t);
            cam.transform.position = Vector3.Lerp(startPos, cameraRig.transform.position, st);
            cam.transform.rotation = Quaternion.Slerp(startRot, cameraRig.transform.rotation, st);
            yield return null;
        }

        cam.transform.position = cameraRig.transform.position;
        cam.transform.rotation = cameraRig.transform.rotation;

        PuzzleInputHandler.Local.EnterPuzzle(cam, () =>
            StartCoroutine(LerpBackToPlayer(cam, originalParent, originalLocalPos, originalLocalRot))
        );
    }

    private IEnumerator LerpBackToPlayer(Camera cam, Transform originalParent, Vector3 localPos, Quaternion localRot)
    {
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * cameraLerpSpeed;
            float st = Mathf.SmoothStep(0f, 1f, t);

            Vector3 targetWorldPos = originalParent.TransformPoint(localPos);
            Quaternion targetWorldRot = originalParent.rotation * localRot;

            cam.transform.position = Vector3.Lerp(startPos, targetWorldPos, st);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetWorldRot, st);

            startPos = cam.transform.position;
            startRot = cam.transform.rotation;

            yield return null;
        }

        cam.transform.SetParent(originalParent, worldPositionStays: false);
        cam.transform.localPosition = localPos;
        cam.transform.localRotation = localRot;
    }
}