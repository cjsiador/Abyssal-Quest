using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using Valve.VR;
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
public class ViveTrackerManager : MonoBehaviour
{
    [Header("Prefab for Tracker Visuals (optional)")]
    [SerializeField] private GameObject trackerPrefab;

    [Header("Discovered Trackers (filled at runtime)")]
    [SerializeField] private List<ViveTrackerInfo> viveTrackerInfos = new List<ViveTrackerInfo>();

    [Header("Vive Tracker Offset")]
    [SerializeField] private Transform referenceOrigin;
    [SerializeField] private Vector3 viveTrackerOffsetPos;
    [SerializeField] private Vector3 viveTrackerOffsetPosScale;
    [SerializeField] private Quaternion viveTrackerOffsetRot;

    private TrackedDevicePose_t[] poses;
    private EVRInitError initErr = EVRInitError.None;
    private bool openVrReady = false;

    void Awake()
    {
        try
        {
            if (OpenVR.System == null)
            {
                initErr = EVRInitError.None;

                OpenVR.Init(ref initErr, EVRApplicationType.VRApplication_Background);

                if (initErr != EVRInitError.None)
                {
                    Debug.Log("[OVRProbe] OpenVR init failed: " + initErr);
                    if (initErr == EVRInitError.Init_VRClientDLLNotFound)
                        Debug.LogWarning("[OVRProbe] SteamVR not found or not running. Install/launch SteamVR.");

                    enabled = false;
                    return;
                }
            }

            openVrReady = (OpenVR.System != null);
            Debug.Log("[OVRProbe] Initialized. Ensure SteamVR is running.");
        }
        catch (Exception e)
        {
            Debug.Log(e);
            enabled = false;
        }
    }

    void Start()
    {
        if (enabled) poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    }

    void OnEnable()
    {
        if (OpenVR.System == null)
        {
            OpenVR.Init(ref initErr, EVRApplicationType.VRApplication_Background);
            if (initErr != EVRInitError.None)
            {
                Debug.Log("[OVRProbe] OpenVR init failed: " + initErr);
                enabled = false;
                return;
            }
        }

        openVrReady = (OpenVR.System != null);
        Debug.Log("[OVRProbe] Initialized. Ensure SteamVR is running.");
    }

    void OnDisable()
    {
        if (OpenVR.System != null)
        {
            OpenVR.Shutdown();
        }
        openVrReady = false;
    }

    void Update()
    {
        if (OpenVR.System == null) return;

        OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, poses);

        for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
        {
            if (OpenVR.System.GetTrackedDeviceClass(i) != ETrackedDeviceClass.Controller && OpenVR.System.GetTrackedDeviceClass(i) != ETrackedDeviceClass.GenericTracker) continue;

            var p = poses[i];
            if (!p.bDeviceIsConnected || !p.bPoseIsValid) continue;

            var rt = new SteamVR_Utils.RigidTransform(p.mDeviceToAbsoluteTracking);

            string serial = GetStringProp(i, ETrackedDeviceProperty.Prop_SerialNumber_String);

            var viveTrackerInfo = SearchTrackers(serial);

            foreach (var tracker in viveTrackerInfo)
            {
                float xPos = rt.pos.x + viveTrackerOffsetPos.x;
                float yPos = rt.pos.y + viveTrackerOffsetPos.y;
                float zPos = rt.pos.z + viveTrackerOffsetPos.z;

                float xRot = rt.rot.x + viveTrackerOffsetRot.x;
                float yRot = rt.rot.y + viveTrackerOffsetRot.y;
                float zRot = rt.rot.z + viveTrackerOffsetRot.z;
                float wRot = rt.rot.w + viveTrackerOffsetRot.w;

                Vector3 pos = new Vector3(xPos, yPos, zPos);
                Quaternion rot = new Quaternion(xRot, yRot, zRot, wRot);

                tracker.TrackerObject.transform.position = pos;
                tracker.TrackerObject.transform.rotation = rt.rot;
            }
        }
    }

    // -------------------------
    // Device Discovery / Listing
    // -------------------------

    [ContextMenu("List All Devices (Any Class)")]
    void ListDevices()
    {
        if (!openVrReady || OpenVR.System == null)
        {
            Debug.Log("[OVRProbe] OpenVR not ready.");
            return;
        }

        var sb = new StringBuilder("== OpenVR Devices ==\n");
        for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
        {
            var cls = OpenVR.System.GetTrackedDeviceClass(i);
            if (cls == ETrackedDeviceClass.Invalid) continue;

            string serial = GetStringProp(i, ETrackedDeviceProperty.Prop_SerialNumber_String);
            string type = GetStringProp(i, ETrackedDeviceProperty.Prop_ControllerType_String);

            sb.AppendLine($"#{i}  class={cls}  serial='{serial}'  type='{type}'");
        }
        Debug.Log(sb.ToString());
    }

    [ContextMenu("Refresh Tracker List (GenericTracker only)")]
    public void RefreshTrackerList()
    {
        // Clean up any old GameObjects
        foreach (var old in viveTrackerInfos)
        {
            if (old.TrackerObject != null)
                Destroy(old.TrackerObject);
        }

        viveTrackerInfos ??= new List<ViveTrackerInfo>();
        viveTrackerInfos.Clear();

        if (!openVrReady || OpenVR.System == null)
        {
            Debug.Log("[OVRProbe] OpenVR not ready.");
            return;
        }

        OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, poses);

        for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
        {
            if (OpenVR.System.GetTrackedDeviceClass(i) != ETrackedDeviceClass.GenericTracker) continue;

            var pose = poses[i];
            if (!pose.bDeviceIsConnected) continue;

            string serial = GetStringProp(i, ETrackedDeviceProperty.Prop_SerialNumber_String);
            string roleStr = GetTrackerRole(i);
            string controllerType = GetStringProp(i, ETrackedDeviceProperty.Prop_ControllerType_String);

            // Create a new marker GameObject if prefab provided
            GameObject trackerObj = null;
            if (trackerPrefab != null)
            {
                trackerObj = Instantiate(trackerPrefab, Vector3.zero, Quaternion.identity);
                trackerObj.name = $"Tracker_{i}_{serial}";
            }

            viveTrackerInfos.Add(new ViveTrackerInfo
            {
                DeviceIndex = i,
                SerialNumber = serial,
                ControllerRole = roleStr,
                ControllerType = controllerType,
                TrackerObject = trackerObj
            });
        }

        Debug.Log($"[OVRProbe] Found {viveTrackerInfos.Count} GenericTracker device(s).");
    }


    // -------------------------
    // Device Setup
    // -------------------------

    private void Calibrate()
    {
        if (viveTrackerInfos == null || referenceOrigin == null) return;

        // Store offset so target matches referenceOrigin
        viveTrackerOffsetPos = referenceOrigin.position - viveTrackerInfos[0].TrackerObject.transform.position;
        viveTrackerOffsetRot = Quaternion.Inverse(viveTrackerInfos[0].TrackerObject.transform.rotation) * referenceOrigin.rotation;
        // Save once when you press Calibrate()
    }

    // -------------------------
    // Helpers
    // -------------------------

    private string GetStringProp(uint deviceIndex, ETrackedDeviceProperty prop)
    {
        var error = ETrackedPropertyError.TrackedProp_Success;
        var sb = new StringBuilder(128);
        OpenVR.System.GetStringTrackedDeviceProperty(deviceIndex, prop, sb, (uint)sb.Capacity, ref error);
        if (error != ETrackedPropertyError.TrackedProp_Success)
            return string.Empty;
        return sb.ToString();
    }

    private string GetTrackerRole(uint deviceIndex)
    {
        var error = ETrackedPropertyError.TrackedProp_Success;
        int roleInt = OpenVR.System.GetInt32TrackedDeviceProperty(
            deviceIndex,
            ETrackedDeviceProperty.Prop_ControllerRoleHint_Int32,
            ref error
        );

        var role = (ETrackedControllerRole)roleInt;
        return role.ToString();
    }

    // -------------------------
    // Search API
    // -------------------------

    public List<ViveTrackerInfo> SearchTrackers(string query)
    {
        if (viveTrackerInfos == null) return new List<ViveTrackerInfo>();
        if (string.IsNullOrEmpty(query)) return new List<ViveTrackerInfo>(viveTrackerInfos);

        return viveTrackerInfos.Where(v =>
               (!string.IsNullOrEmpty(v.SerialNumber) &&
                v.SerialNumber.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            || (!string.IsNullOrEmpty(v.ControllerRole) &&
                v.ControllerRole.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            || (!string.IsNullOrEmpty(v.ControllerType) &&
                v.ControllerType.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            || v.DeviceIndex.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
        ).ToList();
    }

    // -------------------------
    // Data Model
    // -------------------------

    [Serializable]
    public class ViveTrackerInfo
    {
        public uint DeviceIndex;       // OpenVR device index
        public string SerialNumber;    // Prop_SerialNumber_String
        public string ControllerRole;  // Prop_ControllerRoleHint_Int32 -> ETrackedControllerRole
        public string ControllerType;  // Prop_ControllerType_String
        public GameObject TrackerObject; // Marker GameObject in scene
    }
}
#endif