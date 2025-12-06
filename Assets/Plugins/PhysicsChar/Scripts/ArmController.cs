using System;
using UnityEngine;

public class ArmController : MonoBehaviour
{
    public float timeBetweenPoses = 0.5f;
    public PoseHandler PoseHandler;

    public AnimationHandler Active;

    public Action onArmHandle;

    private void Awake()
    {
        if (PoseHandler == null)
            PoseHandler = transform.parent.GetComponentInChildren<PoseHandler>();

        if (Active == null)
            Active = GetComponentInChildren<AnimationHandler>();
    }

    private void Start()
    {
        if (PoseHandler != null)
        {
            PoseHandler.CurrentTargetPose = Active.GetComponent<AnimationPistolHandler>().RestPose;
        }
        else
        {
            Debug.LogError("PoseHandler not found!");
        }
    }

    public void ArmHandle(Hands hand, bool Cancelled) 
    {
        if (!Cancelled)
        {
            if (Active != null)
            {
                Active.RunAnimation(hand, Cancelled, timeBetweenPoses);
            }
        }
        else
        {
            if (Active != null)
            {
                Active.RunAnimation(hand, Cancelled, timeBetweenPoses);
            }
        }

    }
}
