using UnityEngine;

public class ConstantForce_MM : MonoBehaviour
{
    public float upForce;

    private Rigidbody rig;

    private Rig data;

    private void Start()
    {
        rig = GetComponent<Rigidbody>();
        data = GetComponentInParent<Rig>();
    }

    private void FixedUpdate()
    {
        rig.AddForce(Vector3.up * upForce * data.control, ForceMode.Acceleration);
    }
}
