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

        Transform rig = transform.Find("RIG");

        // Get all rigidbodies
        var rbs = rig.GetComponentsInChildren<Rigidbody>();

        // Calculate an offset that snaps the player's root to the target spawn
        // position instead of trying to align to a specific bone (which could
        // be offset or animated away from the root).
        Vector3 offset = targetPosition - rig.position;

        // Freeze all rigidbodies
        foreach (var rb in rbs)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Move immediately to avoid physics-driven drift between frames
        rig.position = targetPosition;
        transform.position = rig.position;

        // Ensure every body stays aligned with the new root position
        foreach (var rb in rbs)
        {
            rb.position += offset;
        }

        // Move transforms
        rig.position += offset;
        transform.position += offset;

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