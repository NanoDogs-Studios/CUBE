using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class WheelExpander : MonoBehaviour
{
    public Animator animator;
    public InputActionReference emoteWheelButton;

    private void OnEnable()
    {
        emoteWheelButton.action.performed += OnEmoteWheelPressed;
        emoteWheelButton.action.canceled += OnEmoteWheelReleased;
    }

    private void OnDisable()
    {
        emoteWheelButton.action.performed -= OnEmoteWheelPressed;
        emoteWheelButton.action.canceled -= OnEmoteWheelReleased;
    }

    private void OnEmoteWheelReleased(InputAction.CallbackContext context)
    {
        animator.Play("WheelHide");
        Invoke("HideWheel", 0.167f);
    }

    private void OnEmoteWheelPressed(InputAction.CallbackContext context)
    {
        animator.gameObject.SetActive(true);
        animator.Play("WheelGrow");
    }

    private void HideWheel()
    {
        animator.gameObject.SetActive(false);
    }
}
