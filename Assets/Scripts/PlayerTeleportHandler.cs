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

        // Calculate an offset from the rig root to the target position so bones
        // are shifted consistently.
        Vector3 offset = targetPosition - rig.position;

        // Cache the desired world positions so we can reapply them after moving the root
        var targetPositions = new Dictionary<Transform, Vector3>(rigTransforms.Length);
        foreach (var t in rigTransforms)
        {
            targetPositions[t] = t.position + offset;
        }

        // Freeze all rigidbodies
        foreach (var rb in rbs)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Wait for physics to settle
        yield return new WaitForFixedUpdate();

        // Shift the rig root, then reapply the cached world positions to children
        rig.position = targetPositions[rig];
        foreach (var t in rigTransforms)
        {
            if (t == rig) continue;
            t.position = targetPositions[t];
        }

        // Keep rigidbody positions in sync with updated transforms
        foreach (var rb in rbs)
        {
            rb.position = rb.transform.position;
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