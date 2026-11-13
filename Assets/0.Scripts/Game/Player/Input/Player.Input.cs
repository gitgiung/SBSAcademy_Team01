using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed partial class Player
{
    private Vector2 CurrentAim()
    {
        if (joystickController != null && joystickController.enabled
            && joystickController.MovementDirection.sqrMagnitude > 0.0001f)
            return joystickController.MovementDirection;
        Vector2 move = moveInput != null ? moveInput.ReadValue<Vector2>() : Vector2.zero;
        if (move.sqrMagnitude > 0.0001f) return move;
        return view != null ? view.FacingDirection : Vector2.right;
    }

    private static bool PointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private InputAction CloneOptional(string actionPath)
    {
        if (inputActions == null) return null;
        InputAction source = inputActions.FindAction(actionPath, false);
        return source != null ? source.Clone() : null;
    }
    private InputAction CreateInputClone(string actionPath)
    {
        if (inputActions == null)
        {
            Debug.LogError("Player requires an Input Action Asset reference.", this);
            return null;
        }

        InputAction source = inputActions.FindAction(actionPath, false);
        if (source == null)
        {
            Debug.LogError($"Input Action '{actionPath}' was not found.", this);
            return null;
        }

        return source.Clone();
    }

    private void DisposeInputActions()
    {
        moveInput?.Dispose();
        dashInput?.Dispose();
        parryInput?.Dispose();
        attackInput?.Dispose();
        heavyInput?.Dispose();
        moveInput = null;
        dashInput = null;
        parryInput = null;
        attackInput = null;
        heavyInput = null;
    }
}
