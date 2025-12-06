using System.Collections.Generic;
using UnityEngine;

public class Rig : MonoBehaviour
{

    public StateType Currentstate;



    public Transform hip;
    public Transform torso;
    public Transform head;
    
    public Transform NONROTATIONOBJ;

    public CollisionHandler[] Feet;


    public List<Rigidbody> allRigs;
    public List<Rigidbody> Arms;



    public float control = 1;
    public float distancetoground = 0;
    public float sinceGrounded = 0;

    public StandingSC standing;
    public MovementSC movement;
    public CharacterInput characterinput;
    public JumpingSC jumping;
    public StepHandler stepHandler;

    public Vector3 Ground = new Vector3();
    public bool Grounded;
    public bool Ragdolling;

    private void Start()
    {
        standing = GetComponent<StandingSC>();
        movement = GetComponent<MovementSC>();
        characterinput = GetComponent<CharacterInput>();
        jumping = GetComponent<JumpingSC>();
        stepHandler = GetComponent<StepHandler>();
    }

    

    public float CalculateDistanceFromGround()
    {
        float distance = Mathf.Abs(Ground.y - head.position.y);
        distancetoground = distance;
        return distance;
    }
    private void Update()
    {
        int amountTouching = 0;

        foreach (CollisionHandler handler in Feet)
        {
            if (handler.TouchGround)
            {
                amountTouching++;
            }
        }
        Grounded = amountTouching > 0;
    }

    private void FixedUpdate()
    {


        if (Grounded)
        {
            sinceGrounded = 0f;
        }
        else
        {
            sinceGrounded += Time.deltaTime;
        }
    }

}
