using UnityEngine;

public class Rotation_MM : MonoBehaviour
{
    Rig data;
    public float torque;
    public RigWithMultiplier[] rigs;
    public Vector3 TargetRotation;

    private void Start()
    {
        data = GetComponent<Rig>();
    }

    private void FixedUpdate()
    {
        if (data == null) return;
        Quaternion targetQuaternion = Quaternion.Euler(TargetRotation);

        for (int i = 0; i < rigs.Length; i++)
        {
            if (rigs[i].part == null) continue;

            Quaternion currentRotation = rigs[i].part.rotation;
            Quaternion neededRotation = targetQuaternion * Quaternion.Inverse(currentRotation);

            Vector3 torqueDirection;
            float torqueMagnitude;
            neededRotation.ToAngleAxis(out torqueMagnitude, out torqueDirection);


            if (torqueMagnitude > 180f)
                torqueMagnitude -= 360f;

            Vector3 torqueToApply = torqueDirection * (torqueMagnitude * Mathf.Deg2Rad);

            rigs[i].part.AddTorque(torqueToApply * data.control * torque * rigs[i].multiplier,
                                  ForceMode.Acceleration);
        }
    }
}