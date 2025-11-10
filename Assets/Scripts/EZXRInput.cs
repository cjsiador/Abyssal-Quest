using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using Valve.VR;
#endif
using System.Collections.Generic;

public static class EZXRInput
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private static CVRSystem system;

    private static uint leftControllerIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
    private static uint rightControllerIndex = OpenVR.k_unTrackedDeviceIndexInvalid;

    // Raw state tracking
    private static Dictionary<uint, VRControllerState_t> currStates = new Dictionary<uint, VRControllerState_t>();
    private static Dictionary<uint, VRControllerState_t> prevStates = new Dictionary<uint, VRControllerState_t>();

    // Explicitly store the last frame's button masks
    private static Dictionary<uint, ulong> prevButtonMasks = new Dictionary<uint, ulong>();
    private static Dictionary<uint, ulong> currButtonMasks = new Dictionary<uint, ulong>();

    public static void Initialize()
    {
        system = OpenVR.System;
        if (system == null)
            Debug.LogError("OpenVR system not initialized!");
    }

    public static void Update()
    {
        if (system == null)
        {
            system = OpenVR.System;
            if (system == null) return;
        }

        FindControllers();

        // Copy button masks to previous
        foreach (var kvp in currButtonMasks)
            prevButtonMasks[kvp.Key] = kvp.Value;

        // Refresh states
        UpdateControllerState(leftControllerIndex);
        UpdateControllerState(rightControllerIndex);
    }

    private static void FindControllers()
    {
        for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
        {
            if (system.GetTrackedDeviceClass(i) != ETrackedDeviceClass.Controller)
                continue;

            var role = system.GetControllerRoleForTrackedDeviceIndex(i);
            if (role == ETrackedControllerRole.LeftHand)
                leftControllerIndex = i;
            else if (role == ETrackedControllerRole.RightHand)
                rightControllerIndex = i;
        }
    }

    private static void UpdateControllerState(uint index)
    {
        if (index == OpenVR.k_unTrackedDeviceIndexInvalid) return;

        VRControllerState_t state = new VRControllerState_t();
        if (system.GetControllerState(index, ref state, (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t))))
        {
            currStates[index] = state;
            currButtonMasks[index] = state.ulButtonPressed;
        }
    }

    // ======================================================
    // BUTTON CHECK HELPERS (Fixed!)
    // ======================================================
    private static bool GetButton(uint controller, EVRButtonId button)
    {
        if (!currButtonMasks.ContainsKey(controller)) return false;
        return (currButtonMasks[controller] & (1UL << (int)button)) != 0;
    }

    private static bool GetButtonDown(uint controller, EVRButtonId button)
    {
        if (!currButtonMasks.ContainsKey(controller)) return false;

        ulong mask = 1UL << (int)button;
        bool curr = (currButtonMasks[controller] & mask) != 0;
        bool prev = prevButtonMasks.ContainsKey(controller) && ((prevButtonMasks[controller] & mask) != 0);
        return curr && !prev;
    }

    private static bool GetButtonUp(uint controller, EVRButtonId button)
    {
        if (!currButtonMasks.ContainsKey(controller)) return false;

        ulong mask = 1UL << (int)button;
        bool curr = (currButtonMasks[controller] & mask) != 0;
        bool prev = prevButtonMasks.ContainsKey(controller) && ((prevButtonMasks[controller] & mask) != 0);
        return !curr && prev;
    }

    // ======================================================
    // AXES
    // ======================================================
    private static Vector2 GetTouchpadAxis(uint controller)
    {
        if (!currStates.ContainsKey(controller)) return Vector2.zero;
        return new Vector2(currStates[controller].rAxis0.x, currStates[controller].rAxis0.y);
    }

    private static Vector2 GetJoystickAxis(uint controller)
    {
        if (!currStates.ContainsKey(controller)) return Vector2.zero;
        return new Vector2(currStates[controller].rAxis2.x, currStates[controller].rAxis2.y);
    }

    private static float GetTriggerPressure(uint controller)
    {
        if (!currStates.ContainsKey(controller)) return 0f;
        return Mathf.Clamp01(currStates[controller].rAxis1.x);
    }

    // ======================================================
    // TRIGGER INPUTS
    // ======================================================
    public static float leftTriggerPressure() => GetTriggerPressure(leftControllerIndex);
    public static float rightTriggerPressure() => GetTriggerPressure(rightControllerIndex);

    public static bool isLeftTriggerPressed() => GetButton(leftControllerIndex, EVRButtonId.k_EButton_SteamVR_Trigger);
    public static bool isLeftTriggerClicked() => GetButtonDown(leftControllerIndex, EVRButtonId.k_EButton_SteamVR_Trigger);
    public static bool isLeftTriggerReleased() => GetButtonUp(leftControllerIndex, EVRButtonId.k_EButton_SteamVR_Trigger);

    public static bool isRightTriggerPressed() => GetButton(rightControllerIndex, EVRButtonId.k_EButton_SteamVR_Trigger);
    public static bool isRightTriggerClicked() => GetButtonDown(rightControllerIndex, EVRButtonId.k_EButton_SteamVR_Trigger);
    public static bool isRightTriggerReleased() => GetButtonUp(rightControllerIndex, EVRButtonId.k_EButton_SteamVR_Trigger);

    // ======================================================
    // GRIP INPUTS
    // ======================================================
    public static bool isLeftGripPressed() => GetButton(leftControllerIndex, EVRButtonId.k_EButton_Grip);
    public static bool isLeftGripClicked() => GetButtonDown(leftControllerIndex, EVRButtonId.k_EButton_Grip);
    public static bool isLeftGripReleased() => GetButtonUp(leftControllerIndex, EVRButtonId.k_EButton_Grip);

    public static bool isRightGripPressed() => GetButton(rightControllerIndex, EVRButtonId.k_EButton_Grip);
    public static bool isRightGripClicked() => GetButtonDown(rightControllerIndex, EVRButtonId.k_EButton_Grip);
    public static bool isRightGripReleased() => GetButtonUp(rightControllerIndex, EVRButtonId.k_EButton_Grip);

    // ======================================================
    // MENU INPUTS
    // ======================================================
    public static bool isLeftMenuButtonPressed() => GetButton(leftControllerIndex, EVRButtonId.k_EButton_ApplicationMenu);
    public static bool isLeftMenuButtonClicked() => GetButtonDown(leftControllerIndex, EVRButtonId.k_EButton_ApplicationMenu);
    public static bool isLeftMenuButtonReleased() => GetButtonUp(leftControllerIndex, EVRButtonId.k_EButton_ApplicationMenu);

    public static bool isRightMenuButtonPressed() => GetButton(rightControllerIndex, EVRButtonId.k_EButton_ApplicationMenu);
    public static bool isRightMenuButtonClicked() => GetButtonDown(rightControllerIndex, EVRButtonId.k_EButton_ApplicationMenu);
    public static bool isRightMenuButtonReleased() => GetButtonUp(rightControllerIndex, EVRButtonId.k_EButton_ApplicationMenu);

    // ======================================================
    // TOUCHPAD INPUTS
    // ======================================================
    public static Vector2 leftTouchPadTouched() => GetTouchpadAxis(leftControllerIndex);
    public static Vector2 rightTouchPadTouched() => GetTouchpadAxis(rightControllerIndex);

    public static bool isLeftTouchPadPressed() => GetButton(leftControllerIndex, EVRButtonId.k_EButton_SteamVR_Touchpad);
    public static bool isLeftTouchPadClicked() => GetButtonDown(leftControllerIndex, EVRButtonId.k_EButton_SteamVR_Touchpad);
    public static bool isLeftTouchPadReleased() => GetButtonUp(leftControllerIndex, EVRButtonId.k_EButton_SteamVR_Touchpad);

    public static bool isRightTouchPadPressed() => GetButton(rightControllerIndex, EVRButtonId.k_EButton_SteamVR_Touchpad);
    public static bool isRightTouchPadClicked() => GetButtonDown(rightControllerIndex, EVRButtonId.k_EButton_SteamVR_Touchpad);
    public static bool isRightTouchPadReleased() => GetButtonUp(rightControllerIndex, EVRButtonId.k_EButton_SteamVR_Touchpad);

    // ======================================================
    // JOYSTICK INPUTS
    // ======================================================
    public static Vector2 leftSecondary2DAxis() => GetJoystickAxis(leftControllerIndex);
    public static Vector2 rightSecondary2DAxis() => GetJoystickAxis(rightControllerIndex);
#endif
}
