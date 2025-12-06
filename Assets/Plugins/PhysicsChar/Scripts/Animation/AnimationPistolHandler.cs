using UnityEngine;

public class AnimationPistolHandler : AnimationHandler
{

    public WeaponPosePreset[] Pose;

    public override void HandleLeftHandAnimation(bool cancelled)
    {
        if (!cancelled)
        {
            InputArmController.armController.PoseHandler.SpawnedObject.GetComponent<Weapon>().ShootGun();
        }
    }

    public override void HandleRightHandAnimation(bool cancelled)
    {
        if (!cancelled)
        {
            Debug.Log("Aim");
            if (!RightUp && !LeftUp)
            {
                InputArmController.armController.PoseHandler.Animation(Pose, timeBetween, cancelled);
                RightUp = true;
            }
        }
        else
        {
            InputArmController.armController.PoseHandler.CurrentTargetPose = RestPose;
            RightUp = false;
        }
    }

    public override void HandleBothHandsAnimation(bool cancelled)
    {
       
    }
}

