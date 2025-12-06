using System.Collections;
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
    public void InitiateTeleport(Vector3 targetPosition)
    {
        if (pv.IsMine)
        {
            // Send RPC to all clients to teleport this player
            pv.RPC("NetworkTeleportPlayer", RpcTarget.All, targetPosition);
        }
        else
        {
            Debug.LogWarning("Cannot teleport a player you don't own!");
        }
    }

    [PunRPC]
    private void NetworkTeleportPlayer(Vector3 targetPosition)
    {
        // This executes on all clients for this specific player
        StartCoroutine(PerformTeleport(targetPosition));
    }

    private IEnumerator PerformTeleport(Vector3 targetPosition)
    {
        // Avoid overlapping teleports that can stack offsets
        if (isTeleporting)
        {
            yield break;
        }

        isTeleporting = true;

        Transform rig = GetComponentInChildren<Rig>()?.transform ?? transform.Find("RIG") ?? transform;

        // Get all rigidbodies
        var rbs = rig.GetComponentsInChildren<Rigidbody>();

        // Calculate an offset from the rig's root so the RIG object (and, by
        // extension, the entire player) lands exactly on the spawn position.
        Vector3 offset = targetPosition - rig.position;

        // Freeze all rigidbodies
        foreach (var rb in rbs)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Wait for physics to settle
        yield return new WaitForFixedUpdate();

        // Move transforms once so the rig, root, and all children align to the
        // target without compounding offsets.
        transform.position += offset;

        // Force Photon to snap instead of interpolating a huge offset
        var transformView = GetComponent<PhotonTransformViewClassic>();
        if (transformView != null)
        {
            transformView.TeleportTo(transform.position, transform.rotation);
        }

        // Force physics update
        Physics.SyncTransforms();

        // Wait before unfreezing
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Unfreeze rigidbodies
        foreach (var rb in rbs)
        {
            rb.isKinematic = false;
        }

        isTeleporting = false;
    }
}