using UnityEngine;

public class MovementSC : MonoBehaviour
{
    Rig data;
    public float Force;
    private void Awake()
    {
        data = GetComponent<Rig>();
    }
    public void Move(Vector3 move)
    {
        foreach (Rigidbody rb in data.allRigs)
        {
            rb.AddForce(move * Force * data.control, ForceMode.Acceleration);
        }
    }

    public void ResetSpeed(float original)
    {
        Force = original;
    }

    public void ResetAfter(float time, float original)
    {
        Invoke("ResetSpeed", time);
    }
}