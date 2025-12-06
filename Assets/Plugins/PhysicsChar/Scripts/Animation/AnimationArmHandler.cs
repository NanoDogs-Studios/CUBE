using System;
using UnityEngine;

public class AnimationArmHandler : AnimationHandler
{
    public PosePreset[] LeftMoveRightPoses;
    public PosePreset[] RightMoveLeftPoses;
    public PosePreset[] LeftPoses;
    public PosePreset[] RightPoses;


    public override void HandleLeftHandAnimation(bool cancelled)
    {
        if (!cancelled)
        {
            if (!LeftUp && !RightUp)
            {
                InputArmController.armController.PoseHandler.Animation(LeftPoses, timeBetween, cancelled);
                LeftUp = true;
            }
        }
        else
        {
            InputArmController.armController.PoseHandler.CurrentTargetPose = InputArmController.armController.PoseHandler.RestPose;
            LeftUp = false;
        }
    }

    public override void HandleRightHandAnimation(bool cancelled)
    {
        if (!cancelled)
        {
            if (!RightUp && !LeftUp)
            {
                InputArmController.armController.PoseHandler.Animation(RightPoses, timeBetween, cancelled);
                RightUp = true;
            }
        }
        else
        {
            InputArmController.armController.PoseHandler.Animation(RightPoses, timeBetween, cancelled);
            RightUp = false;
        }
    }

    public override void HandleBothHandsAnimation(bool cancelled)
    {
        if (!cancelled)
        {
            RightUp = true;
            LeftUp = true;
        }
        else
        {
            RightUp = false;
            LeftUp = false;
            InputArmController.armController.PoseHandler.CurrentTargetPose = InputArmController.armController.PoseHandler.RestPose;
        }
    }
}