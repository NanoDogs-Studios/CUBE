using System.Collections;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class PlayerTeleportHandler : MonoBehaviourPunCallbacks
{
    [Header("Setup")]
    [Tooltip("Root of the ragdoll rig. If left null, will try to Find(\"RIG\").")]
    [SerializeField] private Transform rigRoot;

    [SerializeField] private Rigidbody[] teleportBodies;

    [Tooltip("Layer mask used to find valid ground below a spawn point.")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Tooltip("How far above the ground the player should be placed.")]
    [SerializeField] private float groundOffset = 0.5f;

    private PhotonView pv;
    private Rigidbody rootRb;
    private bool isTeleporting;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        rootRb = GetComponent<Rigidbody>();

        if (rigRoot == null)
        {
            var found = transform.Find("RIG");
            if (found != null) rigRoot = found;
        }
    }

    /// <summary>
    /// Teleport this player to a WORLD position/rotation.
    /// IMPORTANT: targetPosition must be world-space.
    /// </summary>
    public void InitiateTeleport(Vector3 targetPosition, Quaternion? targetRotation = null, bool snapToGround = true)
    {
        if (!pv.IsMine)
        {
            Debug.LogWarning("[Teleport] Cannot teleport a player you don't own!");
            return;
        }

        bool hasRotation = targetRotation.HasValue;
        Quaternion rotation = targetRotation ?? Quaternion.identity;

        // Buffered so late joiners also get the last teleport
        pv.RPC(nameof(NetworkTeleportPlayer), RpcTarget.AllBuffered, targetPosition, hasRotation, rotation, snapToGround);
    }

    [PunRPC]
    public void NetworkTeleportPlayer(Vector3 targetPosition, bool hasRotation, Quaternion targetRotation, bool snapToGround)
    {
        StartCoroutine(PerformTeleport(targetPosition, hasRotation ? targetRotation : (Quaternion?)null, snapToGround));
    }

    private IEnumerator PerformTeleport(Vector3 targetPosition, Quaternion? targetRotation, bool snapToGround)
    {
        if (isTeleporting) yield break;
        isTeleporting = true;

        if (rigRoot == null)
        {
            Debug.LogError("[Teleport] rigRoot is null. Cannot teleport ragdoll safely.", this);
            isTeleporting = false;
            yield break;
        }

        // 1) Calculate safe target position
        Vector3 finalTargetPos = targetPosition;
        if (snapToGround)
        {
            Vector3 rayStart = targetPosition + Vector3.up * 5f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 50f, groundMask, QueryTriggerInteraction.Ignore))
                finalTargetPos = hit.point + Vector3.up * groundOffset;
            else
                Debug.LogWarning($"[Teleport] No ground found below teleport target at {targetPosition}. Using raw position.");
        }

        // 2) Gather and freeze rigidbodies (ragdoll)
        var rbs = rigRoot.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rbs)
        {
            rb.isKinematic = true;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        // Also freeze root rigidbody if you have one (prevents physics from fighting the teleport)
        bool rootWasKinematic = false;
        if (rootRb != null)
        {
            rootWasKinematic = rootRb.isKinematic;
            rootRb.isKinematic = true;
#if UNITY_6000_0_OR_NEWER
            rootRb.linearVelocity = Vector3.zero;
#else
            rootRb.velocity = Vector3.zero;
#endif
            rootRb.angularVelocity = Vector3.zero;
            rootRb.Sleep();
        }

        yield return new WaitForFixedUpdate();

        // =========================================================
        // IMPORTANT: ABSOLUTE SET (NOT +=)
        //
        // Compute the CURRENT world-space offset from rigRoot to the prefab root.
        // Then place the prefab root so rigRoot ends up exactly on finalTargetPos.
        // This avoids any “position added to” feel if something else changed the root
        // between frames (parent motion, physics, late network corrections, etc.).
        // =========================================================

        Vector3 rootMinusRig = transform.position - rigRoot.position; // world delta
        Quaternion newRootRot = transform.rotation;

        if (targetRotation.HasValue)
        {
            // Rotate the whole character so rigRoot matches the desired rotation.
            // We rotate the rootMinusRig offset too so the root stays consistent.
            Quaternion deltaRot = targetRotation.Value * Quaternion.Inverse(rigRoot.rotation);
            newRootRot = deltaRot * transform.rotation;
            rootMinusRig = deltaRot * rootMinusRig;
        }

        Vector3 newRootPos = finalTargetPos + rootMinusRig;

        Physics.SyncTransforms();

        // 4) Force RB poses to match transforms
        foreach (var rb in rbs)
        {
            rb.position = newRootPos;
            rb.rotation = newRootRot;
        }

        // Optional: snap network targets to avoid smoothing from old pose
        var net = GetComponent<RagdollNetwork>();
        if (net != null) net.ForceSnapToCurrentPose(0.15f);

        Physics.SyncTransforms();

        // 5) Unfreeze next fixed updates
        StartCoroutine(UnfreezeRigidbodiesNextFixedUpdate(rbs, rootWasKinematic));

        isTeleporting = false;
    }

    private IEnumerator UnfreezeRigidbodiesNextFixedUpdate(Rigidbody[] rbs, bool rootWasKinematic)
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        foreach (var rb in rbs)
        {
            rb.WakeUp();
            rb.isKinematic = false;
        }
    }
}
