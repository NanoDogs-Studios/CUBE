using System.Collections;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class PlayerTeleportHandler : MonoBehaviourPunCallbacks
{
    private PhotonView pv;
    private bool isTeleporting = false;
    private Coroutine teleportRoutine;

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
        if (teleportRoutine != null)
        {
            StopCoroutine(teleportRoutine);
        }

        teleportRoutine = StartCoroutine(PerformTeleport(targetPosition));
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

        if (rig == null)
        {
            transform.position = targetPosition;
            yield break;
        }

        // Get all rigidbodies
        var rbs = rig.GetComponentsInChildren<Rigidbody>();

        // Find hip for offset calculation
        Rigidbody hip = null;
        foreach (var rb in rbs)
        {
            if (rb.name == "Hip")
            {
                hip = rb;
                break;
            }
        }

        Vector3 offset;
        if (hip != null)
        {
            offset = targetPosition - hip.position;
        }
        else
        {
            // No hip found, use the RIG position
            offset = targetPosition - rig.position;
        }

        // Freeze all rigidbodies
        foreach (var rb in rbs)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Wait for physics to settle
        yield return new WaitForFixedUpdate();

        // Move all rigidbodies
        foreach (var rb in rbs)
        {
            rb.position += offset;
        }

        // Move transforms
        rig.position += offset;
        transform.position = targetPosition;

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