using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PoseHandler : MonoBehaviour
{
    public PosePreset RestPose;
    public PosePreset CurrentTargetPose;

    public Transform RotationObject;
    public Transform CameraObject;

    public GameObject SpawnedObject;
    public bool WeaponSpawned = false;

    Rig data;

    public float drag = 0.05f;
    public float control = 1f;

    public bool DoingAnimation = false;
    public Action onArmHandle;

    public float rotP;
    private void Start()
    {
        data = transform.parent.parent.GetComponentInChildren<Rig>();
    }

    private void FixedUpdate()
    {
        /*if (Vector3.Distance(data.hip.transform.position, CurrentTargetPose.Hip.position) > 3f || data.control < 0.5f)
        {
            CurrentTargetPose.transform.position = data.Ground;
        }*/
        
        if (CurrentTargetPose != null)
        {
            CurrentTargetPose.gameObject.TryGetComponent(out WeaponPosePreset preset);
            if (preset != null)
            {
                if (!WeaponSpawned)
                {
                    SpawnedObject = Instantiate(preset.Weapon.gameObject, Vector3.zero, Quaternion.identity, data.allRigs[8].transform);
                    if (SpawnedObject)
                    {
                        SpawnedObject.transform.localPosition = preset.PositionOffset; // Use PositionOffset instead
                        SpawnedObject.transform.localRotation = Quaternion.Euler(preset.RotationOffset); // Use localRotation
                        WeaponSpawned = true;
                    }
                }
                else
                {
                    // Use the same approach as instantiation for consistency
                    SpawnedObject.transform.localPosition = preset.PositionOffset;
                    SpawnedObject.transform.localRotation = Quaternion.Euler(preset.RotationOffset);
                }
            }
            else if (preset == null)
            {
                if (SpawnedObject)
                {
                    WeaponSpawned = false;
                    Destroy(SpawnedObject);
                }
            }
            CurrentTargetPose.transform.rotation = RotationObject.rotation;
            if (CurrentTargetPose.LetHeadMovement)
            {
                CurrentTargetPose.transform.rotation = Quaternion.Euler(CameraObject.rotation.eulerAngles.x, RotationObject.rotation.eulerAngles.y, RotationObject.rotation.eulerAngles.z);
            }
            DoPose(CurrentTargetPose);
        }
    }
    void DoPose(PosePreset p)
    {
        int num = -1;
        foreach (Rigidbody i in data.allRigs)
        {
            try
            {
                num++;
                Transform pose = p.GetPose(num);
                Follow(i, pose, p, false, true, 1);
            }
            catch (System.Exception e)
            {
            }
        }
    }
    public void Animation(PosePreset[] poses, float TimeBetween, bool cancelled)
    {
        if (cancelled)
        {
            CurrentTargetPose = RestPose;
            DoingAnimation = false;
            StopAllCoroutines();
            Invoke("IMANGRY", 0.05f);
            return;
        }
        else
        {
            DoingAnimation = true;
            StartCoroutine(AnimationSequence(poses, TimeBetween));
        }
    }

    public void IMANGRY()
    {
        onArmHandle?.Invoke();
    }

    IEnumerator AnimationSequence(PosePreset[] poses, float timeBetween)
    {
        foreach (PosePreset pose in poses)
        {
            yield return StartCoroutine(ChangePose(timeBetween, pose));
        }
        CurrentTargetPose = RestPose;
        onArmHandle?.Invoke();
        DoingAnimation = false;
    }

    IEnumerator ChangePose(float Time, PosePreset NextPose)
    {
        CurrentTargetPose.transform.rotation = Quaternion.Euler(Vector3.zero);
        CurrentTargetPose = NextPose;
        yield return new WaitForSeconds(Time);
    }

    private void Follow(Rigidbody rig, Transform target, PosePreset pose, bool localAnim = false, bool disableStanding = true, float multiplier = 1f)
    {
        Vector3 forward = target.forward;
        Vector3 up = target.up;
        Vector3 vector = (0f - Vector3.Angle(forward, rig.transform.forward)) * Vector3.Cross(forward, rig.transform.forward).normalized;
        Vector3 vector2 = (0f - Vector3.Angle(up, rig.transform.up)) * Vector3.Cross(up, rig.transform.up).normalized;
        rig.AddTorque(Time.fixedDeltaTime * data.control * 200f * multiplier * rotP * (vector + vector2), ForceMode.Acceleration);
        rig.angularVelocity -= rig.angularVelocity * drag * multiplier * control;
        Vector3 position = target.position;
        Vector3 vector3 = position - rig.transform.position;
        //rig.AddForce(vector3 * 200f * multiplier * control * P * Time.fixedDeltaTime, ForceMode.Acceleration);
        rig.linearVelocity -= rig.linearVelocity * drag * multiplier * control;
    }

}
