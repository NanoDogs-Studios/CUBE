using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class PlayerTeleportHandler : MonoBehaviourPunCallbacks
{
    private PhotonView pv;
    private bool isTeleporting;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    // Call this method from PhotonLauncher or any other script to teleport this player
    public void InitiateTeleport(Vector3 targetPosition, Quaternion? targetRotation = null)
    {
        if (pv.IsMine)
        {
            // Send RPC to all clients to teleport this player
            bool hasRotation = targetRotation.HasValue;
            Quaternion rotation = targetRotation ?? Quaternion.identity;
            pv.RPC("NetworkTeleportPlayer", RpcTarget.All, targetPosition, hasRotation, rotation);
        }
        else
        {
            Debug.LogWarning("Cannot teleport a player you don't own!");
        }
    }

    [PunRPC]
    private void NetworkTeleportPlayer(Vector3 targetPosition, bool hasRotation, Quaternion targetRotation)
    {
        // This executes on all clients for this specific player
        StartCoroutine(PerformTeleport(targetPosition, hasRotation ? targetRotation : (Quaternion?)null));
    }

    private IEnumerator PerformTeleport(Vector3 targetPosition, Quaternion? targetRotation)
    {
        // Avoid overlapping teleports that can stack offsets
        if (isTeleporting)
        {
            yield break;
        }

        isTeleporting = true;

        Transform rig = transform.Find("RIG");
        if (rig == null)
        {
            isTeleporting = false;
            yield break;
        }

        // Get all rigidbodies and transforms inside the rig (including root)
        var rbs = rig.GetComponentsInChildren<Rigidbody>();
        var rigTransforms = rig.GetComponentsInChildren<Transform>();

        // Calculate an offset from the player root to the target position so bones
        // and Photon transform sync stay aligned with spawns.
        Vector3 offset = targetPosition - transform.position;
        Quaternion? rotationOffset = targetRotation.HasValue
            ? targetRotation.Value * Quaternion.Inverse(transform.rotation)
            : (Quaternion?)null;

        // Cache the desired world positions so we can reapply them after moving the root
        var targetPositions = new Dictionary<Transform, Vector3>(rigTransforms.Length);
        var targetRotations = new Dictionary<Transform, Quaternion>(rigTransforms.Length);
        foreach (var t in rigTransforms)
        {
            targetPositions[t] = t.position + offset;
            targetRotations[t] = rotationOffset.HasValue
                ? rotationOffset.Value * t.rotation
                : t.rotation;
        }

        // Freeze all rigidbodies
        foreach (var rb in rbs)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        // Wait for physics to settle
        yield return new WaitForFixedUpdate();

        // Move the player root so Photon sync stays aligned
        transform.position += offset;
        if (rotationOffset.HasValue)
        {
            transform.rotation = rotationOffset.Value * transform.rotation;
        }

        // Shift the rig root, then reapply the cached world positions to children
        rig.position = targetPositions[rig];
        rig.rotation = targetRotations[rig];
        foreach (var t in rigTransforms)
        {
            if (t == rig) continue;
            t.position = targetPositions[t];
            t.rotation = targetRotations[t];
        }

        // Keep rigidbody positions in sync with updated transforms
        foreach (var rb in rbs)
        {
            rb.position = rb.transform.position;
            rb.rotation = rb.transform.rotation;
        }

        // Force physics update
        Physics.SyncTransforms();

        // Unfreeze rigidbodies on the next fixed update to avoid drift
        StartCoroutine(UnfreezeRigidbodiesNextFixedUpdate(rbs));

        isTeleporting = false;
    }

    private IEnumerator UnfreezeRigidbodiesNextFixedUpdate(Rigidbody[] rbs)
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        foreach (var rb in rbs)
        {
            rb.isKinematic = false;
        }
    }
}