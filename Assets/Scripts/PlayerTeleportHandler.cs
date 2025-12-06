using System.Collections;
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
    /// Teleport this player to a world-space position/rotation.
    /// For map / intermission spawns, call with useOffset = false (default).
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

        // Buffered so late joiners also get the last teleport
        pv.RPC("NetworkTeleportPlayer", RpcTarget.AllBuffered, targetPosition, hasRotation, rotation, useOffset, snapToGround);
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

        // 1) Calculate safe target position
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
                Debug.LogWarning($"[Teleport] No ground found below teleport target at {targetPosition}. Using raw position.");
            }
        }

        // 2) Gather and freeze rigidbodies
        var rbs = rig.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        yield return new WaitForFixedUpdate();

        // 3) Hard snap root + rig to the final target (ignore offset for now to avoid drift)
        transform.position = finalTargetPos;
        rig.position = finalTargetPos;

        if (targetRotation.HasValue)
        {
            transform.rotation = targetRotation.Value;
            rig.rotation = targetRotation.Value;
        }

        // 4) Sync transforms -> rigidbodies
        foreach (var rb in rbs)
        {
            rb.position = rb.transform.position;
            rb.rotation = rb.transform.rotation;
        }

        Physics.SyncTransforms();

        // 5) Unfreeze next fixed update
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
