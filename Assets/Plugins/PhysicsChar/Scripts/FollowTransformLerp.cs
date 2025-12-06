using UnityEngine;

public class FollowTransformLerp : MonoBehaviour
{
    public Transform target;

    public float speed = 5f;

    private void LateUpdate()
    {
        if (speed == 0f)
        {
            base.transform.position = target.position;
        }
        else
        {
            base.transform.position = FRILerp.Lerp(base.transform.position, target.position, speed);
        }
    }
}
