using UnityEngine;

public class Gravity_MM : MonoBehaviour
{
    private Rig data;

    public float gravity = 20f;

    private void Start()
    {
        data = GetComponent<Rig>();
    }

    private void FixedUpdate()
    {
        if (!data.Grounded)
        {
            for (int i = 0; i < data.allRigs.Count; i++)
            {
                data.allRigs[i].AddForce(Vector3.down * data.control * gravity * data.sinceGrounded * data.control, ForceMode.Acceleration);
            }
        }
    }
}
