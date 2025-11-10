using UnityEngine;

public class EZXRInputTester : MonoBehaviour
{
    [Header("Trigger Pressure")]
    public float leftTriggerPressure;
    public float rightTriggerPressure;

    [Header("Trigger Buttons")]
    public bool leftTriggerPressed;
    public bool rightTriggerPressed;

    [Header("Grip Buttons")]
    public bool leftGripPressed;
    public bool rightGripPressed;

    [Header("Menu Buttons")]
    public bool leftMenuPressed;
    public bool rightMenuPressed;

    [Header("Touchpad")]
    public Vector2 leftTouchPad;
    public bool leftTouchPadPressed;

    public Vector2 rightTouchPad;
    public bool rightTouchPadPressed;

    private void Update()
    {
        EZXRInput.Update();

        // Triggers
        leftTriggerPressure = EZXRInput.leftTriggerPressure();
        rightTriggerPressure = EZXRInput.rightTriggerPressure();

        leftTriggerPressed = EZXRInput.isLeftTriggerPressed();
        rightTriggerPressed = EZXRInput.isRightTriggerPressed();

        // Grips
        leftGripPressed = EZXRInput.isLeftGripPressed();

        rightGripPressed = EZXRInput.isRightGripPressed();

        // Menu
        leftMenuPressed = EZXRInput.isLeftMenuButtonPressed();

        rightMenuPressed = EZXRInput.isRightMenuButtonPressed();

        // Touchpads
        leftTouchPad = EZXRInput.leftTouchPadTouched();
        leftTouchPadPressed = EZXRInput.isLeftTouchPadPressed();

        rightTouchPad = EZXRInput.rightTouchPadTouched();
        rightTouchPadPressed = EZXRInput.isRightTouchPadPressed();
    }
}
