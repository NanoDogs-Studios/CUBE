using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

[RequireComponent(typeof(PhotonView))]
public class RagdollNetwork : MonoBehaviour, IPunObservable
{
    public Transform hip;                      // the main bone to send
    public Rigidbody hipRb;                    // hip's Rigidbody on owner
    public List<Transform> keyBones;           // chest, head, hands, feet etc. (configure in inspector)

    PhotonView pv;

    // smoothing
    Vector3 hipPosTarget;
    Quaternion hipRotTarget;
    Vector3 hipVelTarget;
    Vector3 hipAngVelTarget;

    Vector3 hipPosBuffer;
    Quaternion hipRotBuffer;

    // buffers for key bones
    Vector3[] keyBonePosTarget;
    Quaternion[] keyBoneRotTarget;

    // interpolation speeds
    public float positionLerp = 20f;
    public float rotationLerp = 20f;

    private float freezeUntilTime = 0f;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        if (keyBones == null) keyBones = new List<Transform>();

        keyBonePosTarget = new Vector3[keyBones.Count];
        keyBoneRotTarget = new Quaternion[keyBones.Count];
    }

    void Start()
    {
        // initial targets
        if (hip) { hipPosTarget = hip.position; hipRotTarget = hip.rotation; }
        for (int i = 0; i < keyBones.Count; i++)
        {
            if (keyBones[i] != null)
            {
                keyBonePosTarget[i] = keyBones[i].localPosition;
                keyBoneRotTarget[i] = keyBones[i].localRotation;
            }
        }

        // Owner: physics active. Remotes: make physics kinematic and we will drive transforms
        if (pv.IsMine)
            SetRagdollKinematic(false);
        else
            SetRagdollKinematic(true);
    }

    void Update()
    {
        if (!pv.IsMine)
        {
            // During teleports, we want to hold the snap pose (no smoothing from old targets)
            if (Time.time < freezeUntilTime) return;

            if (hip != null)
            {
                hip.position = Vector3.Lerp(hip.position, hipPosTarget, Time.deltaTime * positionLerp);
                hip.rotation = Quaternion.Slerp(hip.rotation, hipRotTarget, Time.deltaTime * rotationLerp);
            }

            for (int i = 0; i < keyBones.Count; i++)
            {
                var t = keyBones[i];
                if (t == null) continue;
                t.localPosition = Vector3.Lerp(t.localPosition, keyBonePosTarget[i], Time.deltaTime * positionLerp);
                t.localRotation = Quaternion.Slerp(t.localRotation, keyBoneRotTarget[i], Time.deltaTime * rotationLerp);
            }
        }
    }

    // This serializes only a few transforms/velocities.
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting && pv.IsMine)
        {
            // Write hip state
            stream.SendNext(hip.position);
            stream.SendNext(hip.rotation);
            // write velocities so remote can extrapolate if desired
            stream.SendNext(hipRb != null ? hipRb.linearVelocity : Vector3.zero);
            stream.SendNext(hipRb != null ? hipRb.angularVelocity : Vector3.zero);

            // Write key bones local transforms
            for (int i = 0; i < keyBones.Count; i++)
            {
                Transform t = keyBones[i];
                if (t != null)
                {
                    stream.SendNext(t.localPosition);
                    stream.SendNext(t.localRotation);
                }
                else
                {
                    stream.SendNext(Vector3.zero);
                    stream.SendNext(Quaternion.identity);
                }
            }
        }
        else if (stream.IsReading)
        {
            // Read hip
            hipPosTarget = (Vector3)stream.ReceiveNext();
            hipRotTarget = (Quaternion)stream.ReceiveNext();
            hipVelTarget = (Vector3)stream.ReceiveNext();
            hipAngVelTarget = (Vector3)stream.ReceiveNext();

            // Read key bones
            for (int i = 0; i < keyBones.Count; i++)
            {
                keyBonePosTarget[i] = (Vector3)stream.ReceiveNext();
                keyBoneRotTarget[i] = (Quaternion)stream.ReceiveNext();
            }
        }
    }

    // toggle all child rigidbodies kinematic (called on start depending on owner)
    void SetRagdollKinematic(bool kinematic)
    {
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        foreach (var r in rbs)
        {
            // leave hip's kinematic state set depending on 'kinematic' param too.
            r.isKinematic = kinematic;
            // optionally disable collision / interpolation on remotes
            if (!pv.IsMine)
            {
                r.interpolation = RigidbodyInterpolation.None;
            }
            else
            {
                r.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
    }

    // Example RPC for impulses -> call on owner to notify remotes of major hits
    [PunRPC]
    public void RPC_ApplyImpulse(int boneIndex, Vector3 worldForce, Vector3 worldPoint)
    {
        // on remotes, we can either apply as velocity (if not kinematic) or store for visual effect
        if (!pv.IsMine)
        {
            var bone = GetBoneByIndex(boneIndex);
            if (bone != null)
            {
                var rb = bone.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.AddForceAtPosition(worldForce, worldPoint, ForceMode.Impulse);
                }
                else
                {
                    // optionally nudge transforms on remotes
                    // e.g. temporarily increase interpolation speed or spawn particle effect
                }
            }
        }
    }

    Transform GetBoneByIndex(int idx)
    {
        if (idx < 0 || idx >= keyBones.Count) return null;
        return keyBones[idx];
    }


    public void ForceSnapToCurrentPose(float freezeSeconds = 0.10f)
    {
        if (hip != null)
        {
            hipPosTarget = hip.position;
            hipRotTarget = hip.rotation;
        }

        for (int i = 0; i < keyBones.Count; i++)
        {
            var t = keyBones[i];
            if (t == null) continue;

            keyBonePosTarget[i] = t.localPosition;
            keyBoneRotTarget[i] = t.localRotation;
        }

        // Remotes: also hard-apply immediately so there's no lerp from old pose
        if (!pv.IsMine)
        {
            if (hip != null)
            {
                hip.position = hipPosTarget;
                hip.rotation = hipRotTarget;
            }

            for (int i = 0; i < keyBones.Count; i++)
            {
                var t = keyBones[i];
                if (t == null) continue;

                t.localPosition = keyBonePosTarget[i];
                t.localRotation = keyBoneRotTarget[i];
            }
        }

        freezeUntilTime = Time.time + freezeSeconds;
    }
}
