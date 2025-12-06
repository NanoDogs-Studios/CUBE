
using UnityEngine;

public class StandingSC : MonoBehaviour
{
    public Rig rig;

    public LayerMask groundlayer;
    [Space]
    public AnimationCurve liftCurve;
    [Space]
    public RigWithMultiplier[] rigToLift;
    [Space]

    public float heightOffset;
    


    private void Start()
    {
        rig = GetComponent<Rig>();
    }

    private void FixedUpdate()
    {
        KeepStanding();
        LiftRigs();
    }

    void KeepStanding()
    {
        RaycastHit hit;
        if (Physics.Raycast(rig.torso.transform.position, Vector3.down, out hit, 1.9f, groundlayer))

        {
            Debug.DrawRay(rig.torso.transform.position, Vector3.down * hit.distance, Color.yellow);
         //   rig.Grounded = true;
            rig.Ground = hit.point;
        } else
        {
            rig.Ground = Vector3.zero;
        //    rig.Grounded = false;   
        }
    }



    void LiftRigs()
    {
        foreach (RigWithMultiplier rigw in rigToLift)
        {
            if (rig.Grounded)
            {
                float distance = rig.CalculateDistanceFromGround();
                if (rig.Ground != Vector3.zero)
                {
                    rigw.part.AddForce(Vector3.up * rig.control * 100 * rigw.multiplier * liftCurve.Evaluate(distance / base.transform.root.localScale.x + heightOffset), ForceMode.Acceleration);
                }
            }
        }
    }
}
