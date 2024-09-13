using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// central station to get inputs for controllers of different purposes,
/// through the use of Input System Package.
/// </summary>
public class GameInput : MonoBehaviour
{
    private GameInputActions gameInputActions;          //asset for modifying various sets of inputs

    private void Awake()
    {
        gameInputActions = new GameInputActions();    
        gameInputActions.Player.Enable();
        gameInputActions.Driver.Enable();
        gameInputActions.UI.Enable();
    }

    /// <summary>
    /// inputs for the movement of PlayerController objects, alread normalized by Input System
    /// </summary>
    /// <returns></returns>
    public Vector2 GetWalkVector()
    {
        Vector2 inputVector = gameInputActions.Player.Walk.ReadValue<Vector2>();
        return inputVector;
    }

    /// <summary>
    /// inputs for the movement of CarDriver objects, alread normalized by Input System
    /// </summary>
    /// <returns></returns>
    public Vector2 GetDriveVector()
    {
        Vector2 inputVector= gameInputActions.Driver.Drive.ReadValue<Vector2>();
        return inputVector;
    }

    /// <summary>
    /// inputs for brake force of CarDriver objects
    /// </summary>
    /// <returns></returns>
    public float GetBrakeValue()
    {
        float inputValue= gameInputActions.Driver.Brake.ReadValue<float>();
        return inputValue;
    }

    public float GetMouseScrollValue()
    {
        float scrollValue= gameInputActions.UI.MouseScroll.ReadValue<float>();
        return scrollValue;
    }

    public Vector2 GetMousePanValue()
    {
        Vector2 panlValue = gameInputActions.UI.MousePan.ReadValue<Vector2>();
        return panlValue;
    }

    public bool GetMouseClickValue()
    {
        bool clickValue= gameInputActions.UI.MousePress.WasPressedThisFrame();
        return clickValue;
    }
    public bool GetMousePressValue()
    {
        bool pressValue = gameInputActions.UI.MousePress.IsPressed();
        return pressValue;
    }

    public bool GetMouseReleaseValue()
    {
        bool released = gameInputActions.UI.MousePress.WasReleasedThisFrame();
        return released;
    }

}
