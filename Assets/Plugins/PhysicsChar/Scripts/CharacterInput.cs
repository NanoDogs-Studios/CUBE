using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInput : MonoBehaviour
{
    public bool isMoving;
    public bool CanRagdollWithQ;
    public Vector3 movementInput;
    Rig data;
    JumpingSC jumpingSC;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction ragdollAction;
    private InputAction jumpAction;

    private void Start()
    {
        data = GetComponent<Rig>();
        jumpingSC = GetComponent<JumpingSC>();

        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
        }

        moveAction = playerInput.actions["Move"];
        ragdollAction = playerInput.actions["Ragdoll"];
        jumpAction = playerInput.actions["Jump"];

        if (ragdollAction != null)
        {
            ragdollAction.started += OnRagdollStarted;
            ragdollAction.canceled += OnRagdollCanceled;
        }

        if (jumpAction != null)
        {
            jumpAction.started += OnJumpStarted;
        }
    }

    private void OnDestroy()
    {
        if (ragdollAction != null)
        {
            ragdollAction.started -= OnRagdollStarted;
            ragdollAction.canceled -= OnRagdollCanceled;
        }

        if (jumpAction != null)
        {
            jumpAction.started -= OnJumpStarted;
        }
    }

    private void Update()
    {
        ProcessMovementInput();
        data.Currentstate = isMoving ? StateType.Walk : StateType.Idle;
    }

    private void ProcessMovementInput()
    {
        Vector2 inputVector = Vector2.zero;

        if (moveAction != null)
        {
            inputVector = moveAction.ReadValue<Vector2>();
        }
        else
        {
            inputVector = new Vector2(
                (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0)
            );
        }

        movementInput = Vector3.zero;

        if (inputVector.y > 0.1f)
        {
            movementInput += data.NONROTATIONOBJ.forward * inputVector.y;
            isMoving = true;
        }
        else if (inputVector.y < -0.1f)
        {
            movementInput += -data.NONROTATIONOBJ.forward * Mathf.Abs(inputVector.y);
            isMoving = true;
        }

        if (inputVector.x < -0.1f)
        {
            movementInput += -data.NONROTATIONOBJ.right * Mathf.Abs(inputVector.x);
            isMoving = true;
        }
        else if (inputVector.x > 0.1f)
        {
            movementInput += data.NONROTATIONOBJ.right * inputVector.x;
            isMoving = true;
        }

        if (inputVector.magnitude < 0.1f)
        {
            isMoving = false;
        }

        if (movementInput.magnitude > 1f)
        {
            movementInput.Normalize();
        }
    }

    private void OnRagdollStarted(InputAction.CallbackContext context)
    {
        if (CanRagdollWithQ)
        {
            data.Ragdolling = true;
            data.control = 0f;
        }
    }

    private void OnRagdollCanceled(InputAction.CallbackContext context)
    {
        if (CanRagdollWithQ)
        {
            data.Ragdolling = false;
            data.control = 1f;
        }
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        if (data.Grounded && !data.Ragdolling)
        {
            jumpingSC.Jump();
        }
    }

    private void FixedUpdate()
    {
        if (isMoving && movementInput != Vector3.zero)
        {
            data.movement.Move(movementInput);
        }
        else if (data.Grounded)
        {
            // make sure the player doesnt move when no input
            data.movement.Move(Vector3.zero);
            Rigidbody controlRB = this.transform.Find("Hip").GetComponent<Rigidbody>();
            controlRB.linearVelocity = Vector3.zero;
            controlRB.angularVelocity = Vector3.zero;
        }
    }
}