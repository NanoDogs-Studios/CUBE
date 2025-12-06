using Unity.VisualScripting;
using UnityEngine;

public class PartAnimation : MonoBehaviour
{
    public LeftRight leftright;

    public AnimationLift[] lift;

    Rigidbody partrb;

    public StateType currentState;

    private Rig data;

    private void Start()
    {
        partrb = GetComponent<Rigidbody>();
        data = GetComponentInParent<Rig>();
    }
    private void Update()
    {
        currentState = data.Currentstate;
    }

    public void Step()
    {
        foreach (AnimationLift l in lift)
        {
            if (l.lift == currentState)
            {
                partrb.AddTorque(GetTransformDirection(l) * data.control * l.curve.Evaluate(10f), ForceMode.Force);
            }
        }

    }


    Vector3 GetTransformDirection(AnimationLift lift)
    {
        if (lift.space == Space_MM.World)
        {
            return lift.strength;
        }
        else if (lift.space == Space_MM.Local)
        {
            return base.transform.TransformDirection(lift.strength);
        }
        else if (lift.space == Space_MM.Hip)
        {
            return data.hip.transform.TransformDirection(lift.strength);
        }
        else return Vector3.zero;
    }

}

public enum StateType
{
    Idle = 0,
    Walk = 1,
    Run = 2
}