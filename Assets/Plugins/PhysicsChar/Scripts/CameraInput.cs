using UnityEngine;
using UnityEngine.InputSystem;

public class CameraInput : MonoBehaviour
{
    public float mouseSensitivity = 20f;
    public float gamepadSensitivity = 100f;
    public float smoothTime = 0.1f;
    public Rotation_MM PlayerBody;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float xRotationVelocity;
    private float yRotationVelocity;

    public PlayerInput playerInput;
    public InputAction lookAction;
    private Vector2 lookInput;

    void Awake()
    {
        lookAction = playerInput.actions.FindAction("Look");
    }

    void Update()
    {
        if (lookAction != null)
        {
            if (playerInput != null && playerInput.currentControlScheme == "Gamepad")
            {
                mouseSensitivity = gamepadSensitivity;
            }
            else
            {
                mouseSensitivity = 20f;
            }

            // Read input from the Look action
            lookInput = lookAction.ReadValue<Vector2>();

            float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
            float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            // Smooth damping for smoother rotation
            float smoothXRotation = Mathf.SmoothDampAngle(transform.localEulerAngles.x, xRotation, ref xRotationVelocity, smoothTime);
            float smoothYRotation = Mathf.SmoothDampAngle(transform.localEulerAngles.y, yRotation, ref yRotationVelocity, smoothTime);

            transform.rotation = Quaternion.Euler(smoothXRotation, smoothYRotation, 0f);
            PlayerBody.TargetRotation = new Vector3(0, smoothYRotation, 0);
        }
    }
}