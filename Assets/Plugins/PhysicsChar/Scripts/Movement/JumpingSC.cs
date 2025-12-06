using UnityEngine;

public class JumpingSC : MonoBehaviour
{
    Rig data;

    public float Force;

    public RigWithMultiplier[] rigs;

    public AnimationCurve curve;

    private void Start()
    {
        data = GetComponent<Rig>();
    }

    public void Jump()
    {
        foreach (RigWithMultiplier rig in rigs)
        {
            rig.part.AddForce(data.NONROTATIONOBJ.up * rig.multiplier * data.control * Force * data.control * curve.Evaluate(10f), ForceMode.Impulse);
        }
    }
}
