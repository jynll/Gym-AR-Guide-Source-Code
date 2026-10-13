using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    private User_Input userInput;

    // Event that notifies other scripts when a screen tap/click happens
    public static event Action<Vector2> OnTap;

    private void Awake()
    {
        userInput = new User_Input();
    }

    private void OnEnable()
    {
        userInput.Enable();
        // Subscribes to the "Tab" action inside your "MobileTouch" action map
        userInput.MobileTouch.Tab.performed += HandleTap;
    }

    private void OnDisable()
    {
        userInput.MobileTouch.Tab.performed -= HandleTap;
        userInput.Disable();
    }

    private void HandleTap(InputAction.CallbackContext context)
    {
        Vector2 tapPosition = Vector2.zero;

        // Get touch position on mobile or mouse position in editor
        if (Touchscreen.current != null)
        {
            tapPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            tapPosition = Mouse.current.position.ReadValue();
        }

        OnTap?.Invoke(tapPosition);
    }
}