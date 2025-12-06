using UnityEngine;

public class ArmHandler : MonoBehaviour
{
    [SerializeField] Transform targetTransform;
    public float torqueStrength = 10f;
    public float savedStrength;
    [SerializeField] float damping = 1f;
    [SerializeField] float maxAngularVelocity = 20f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = maxAngularVelocity;
        savedStrength = torqueStrength;
    }

    void FixedUpdate()
    {

        Quaternion deltaRotation = targetTransform.rotation * Quaternion.Inverse(rb.rotation);
        deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

        if (angleInDegrees > 180f)
            angleInDegrees -= 360f;

        if (Mathf.Abs(angleInDegrees) < 0.01f)
            return;

        rotationAxis.Normalize();


        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
        Vector3 torqueP = rotationAxis * angleInRadians * torqueStrength;


        float angularVelocityAlongAxis = Vector3.Dot(rb.angularVelocity, rotationAxis);
        Vector3 torqueD = -rotationAxis * angularVelocityAlongAxis * damping;


        Vector3 totalTorque = torqueP + torqueD;

        rb.AddTorque(totalTorque, ForceMode.Force);
    }
}