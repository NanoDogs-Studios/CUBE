using UnityEngine;
using UnityEngine.InputSystem;

public class InputArmController : MonoBehaviour
{
    public ArmController armController;

    // Drag these from your Input Action Asset in the inspector
    [Header("Input Actions")]
    public InputActionReference leftClickActionRef;
    private InputAction leftClickAction;

    private void Awake()
    {
        // Grab the actual InputActions from the references
        if (leftClickActionRef != null)
            leftClickAction = leftClickActionRef.action;
    }

    private void OnEnable()
    {
        armController = GetComponent<ArmController>();

        if (leftClickAction != null)
        {
            leftClickAction.started += OnLeftClickStarted;
            leftClickAction.canceled += OnLeftClickCanceled;
            leftClickAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (leftClickAction != null)
        {
            leftClickAction.started -= OnLeftClickStarted;
            leftClickAction.canceled -= OnLeftClickCanceled;
            leftClickAction.Disable();
        }
    }

    private void OnLeftClickStarted(InputAction.CallbackContext context)
    {
        armController.ArmHandle(Hands.Right, false);
    }

    private void OnLeftClickCanceled(InputAction.CallbackContext context)
    {
        armController.ArmHandle(Hands.Right, true);
    }
}
