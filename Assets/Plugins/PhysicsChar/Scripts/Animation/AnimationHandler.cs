using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    public InputArmController InputArmController;
    public PosePreset RestPose;

    [Space]
    public bool LeftUp;
    public bool RightUp;

    public float timeBetween = 1f;

    public void RunAnimation(Hands side, bool cancelled, float timeBetweenPoses)
    {
        timeBetween = timeBetweenPoses;

        switch (side)
        {
            case Hands.Left:
                HandleLeftHandAnimation(cancelled);
                break;

            case Hands.Right:
                HandleRightHandAnimation(cancelled);
                break;

            case Hands.Both:
                HandleBothHandsAnimation(cancelled);
                break;
        }
    }

    public virtual void HandleLeftHandAnimation(bool cancelled)
    {
        
    }

    public virtual void HandleRightHandAnimation(bool cancelled)
    {
       
    }

    public virtual void HandleBothHandsAnimation(bool cancelled)
    {
      
    }
}
