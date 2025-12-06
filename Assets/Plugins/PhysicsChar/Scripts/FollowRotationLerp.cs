using UnityEngine;

public class FollowRotationLerp : MonoBehaviour
{
    public Transform target;
    public float rotationSpeed = 5f;

    private void LateUpdate()
    {

        if (rotationSpeed > 0f)
        {
            float targetYRotation = target.eulerAngles.y;
            float currentYRotation = transform.eulerAngles.y;

            float newYRotation = Mathf.LerpAngle(currentYRotation, targetYRotation, rotationSpeed * Time.deltaTime);

            transform.eulerAngles = new Vector3(0f, newYRotation, 0f);
        }
    }
}