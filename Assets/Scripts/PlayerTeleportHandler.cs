using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class PlayerTeleportHandler : MonoBehaviourPunCallbacks
{
    [Header("Setup")]
    [Tooltip("Root of the ragdoll rig. If left null, will try to Find(\"RIG\").")]
    [SerializeField] private Transform rigRoot;

    [Tooltip("Layer mask used to find valid ground below a spawn point.")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Tooltip("How far above the ground the player should be placed.")]
    [SerializeField] private float groundOffset = 0.5f;

    private PhotonView pv;
    private bool isTeleporting;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        if (rigRoot == null)
        {
            var found = transform.Find("RIG");
            if (found != null) rigRoot = found;
        }
    }

    /// <summary>
    /// Call this to teleport this player to a map spawn or intermission spawn.
    /// For map teleports, prefer useOffset = false so you land exactly on the spawn.
    /// </summary>
    public void InitiateTeleport(Vector3 targetPosition, Quaternion? targetRotation = null, bool useOffset = false, bool snapToGround = true)
    {
        if (!pv.IsMine)
        {
            Debug.LogWarning("[Teleport] Cannot teleport a player you don't own!");
            return;
        }

        bool hasRotation = targetRotation.HasValue;
        Quaternion rotation = targetRotation ?? Quaternion.identity;

        pv.RPC("NetworkTeleportPlayer", RpcTarget.All, targetPosition, hasRotation, rotation, useOffset, snapToGround);
    }

    [PunRPC]
    private void NetworkTeleportPlayer(Vector3 targetPosition, bool hasRotation, Quaternion targetRotation, bool useOffset, bool snapToGround)
    {
        StartCoroutine(PerformTeleport(targetPosition, hasRotation ? targetRotation : (Quaternion?)null, useOffset, snapToGround));
    }

    private IEnumerator PerformTeleport(Vector3 targetPosition, Quaternion? targetRotation, bool useOffset, bool snapToGround)
    {
        if (isTeleporting)
            yield break;

        isTeleporting = true;

        Transform rig = rigRoot != null ? rigRoot : transform;

        // Calculate a safe target position (snap to ground if requested)
        Vector3 finalTargetPos = targetPosition;
        if (snapToGround)
        {
            Vector3 rayStart = targetPosition + Vector3.up * 5f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, groundMask, QueryTriggerInteraction.Ignore))
            {
                finalTargetPos = hit.point + Vector3.up * groundOffset;
            }
            else
            {
                // No ground found – log this so you can fix the spawn point in the scene.
                Debug.LogWarning($"[Teleport] No ground found below teleport target at {targetPosition}. Using raw position.");
            }
        }

        // Gather rig rigidbodies (ragdoll pieces)
        var rbs = rig.GetComponentsInChildren<Rigidbody>();

        // Freeze physics
        foreach (var rb in rbs)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        // Wait for one fixed-step to make sure all physics has settled
        yield return new WaitForFixedUpdate();

        if (useOffset)
        {
            // Move relative to current position (for short-range teleports / knockbacks)
            Vector3 offset = finalTargetPos - transform.position;
            Quaternion? rotationOffset = targetRotation.HasValue
                ? targetRotation.Value * Quaternion.Inverse(transform.rotation)
                : (Quaternion?)null;

            transform.position += offset;
            if (rotationOffset.HasValue)
                transform.rotation = rotationOffset.Value * transform.rotation;

            rig.position += offset;
            if (rotationOffset.HasValue)
                rig.rotation = rotationOffset.Value * rig.rotation;
        }
        else
        {
            // Hard snap to spawn point (recommended for map / intermission teleports)
            transform.position = finalTargetPos;
            rig.position = finalTargetPos;

            if (targetRotation.HasValue)
            {
                transform.rotation = targetRotation.Value;
                rig.rotation = targetRotation.Value;
            }
        }

        // Sync transforms -> rigidbodies
        foreach (var rb in rbs)
        {
            rb.position = rb.transform.position;
            rb.rotation = rb.transform.rotation;
        }

        Physics.SyncTransforms();

        // Unfreeze next fixed update
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
