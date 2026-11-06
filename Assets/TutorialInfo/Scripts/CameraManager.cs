using System.Collections.Generic;
using UnityEngine;

// Plain data class, not a MonoBehaviour, so it can share this file.
[System.Serializable]
public class CameraLocation
{
    public string id;
    public Transform loc;
    [Tooltip("If true, the camera stays at this marker's position but turns every frame to keep the deer in view.")]
    public bool trackSubject = true;
}


public class CameraManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Main Camera (the object with the Camera component).")]
    public Transform cameraTransform;
    [Tooltip("The deer from the Hierarchy: the object that actually moves.")]
    public Transform followDeer;

    [Header("Locations")]
    public List<CameraLocation> cameraLocations = new List<CameraLocation>();
    [Tooltip("Optional id to start at on Play. Leave blank to start at the shot for wherever the deer is standing.")]
    public string startLocationId;

    [Header("Finding The Right Shot")]
    [Tooltip("Height above the deer's pivot used to check which zone it is standing in (yellow sphere when this object is selected).")]
    public float zoneProbeHeight = 0.5f;
    [Tooltip("Layers your CameraZone triggers are on. Everything is fine.")]
    public LayerMask zoneLayers = ~0;

    [Header("Fixed Mode Tracking")]
    [Tooltip("How quickly a tracking camera turns to keep up with the deer. Higher is tighter.")]
    public float trackSharpness = 4f;

    [Header("Follow Mode")]
    public Vector3 followOffset = new Vector3(0f, 2f, -4f);
    public float followHeightLookOffset = 1f;
    public float followSharpness = 8f;
    public Transform aimEyePoint;
    public LayerMask hidewhenAiming;
    public float hideDistance = 0.6f;
    public float aimNearClip = 0.05f;
    private bool aiming;
    private float aimPitch;
    private Camera cam;
    int defaultCullingMask;
    float defaultNearClip;


    public bool blendIntoFollow = false;
    [SerializeField] private bool followMode;

    // Derived runtime index; never serialized. Rebuilt on Awake and whenever
    // the Inspector changes during Play, so edits like ticking Track Subject
    // take effect immediately.
    private readonly Dictionary<string, CameraLocation> lookup = new Dictionary<string, CameraLocation>();

    // The current shot is stored by id, not by object, so it always reads the
    // live entry from the list rather than a stale copy.
    private string targetId;

    // What LateUpdate applied last frame, to detect changes of shot.
    private bool hasApplied;
    private bool appliedFollow;
    private string appliedTargetId;

    // What LateUpdate wrote last frame, to detect other scripts moving the camera.
    private bool wroteLastFrame;
    private Vector3 lastWrittenPos;
    private Quaternion lastWrittenRot;
    private bool warnedOtherWriter;

    public bool IsFollowing => followMode;
    public string CurrentLocationId => targetId;

    void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        BuildLookup();
        CheckSetup();
        cam=cameraTransform.GetComponent<Camera>();
        defaultCullingMask = cam.cullingMask;
        defaultNearClip = cam.nearClipPlane;
    }

    void Start()
    {

        if (!string.IsNullOrEmpty(startLocationId))
            SwitchTo(startLocationId);
    }

    void OnValidate()
    {
        // Inspector edits during Play: re-read the list so they apply right away.
        if (Application.isPlaying)
            BuildLookup();
    }

    [ContextMenu("Rebuild Lookup")]
    public void BuildLookup()
    {
        lookup.Clear();

        foreach (var location in cameraLocations)
        {
            if (location == null) continue;

            if (string.IsNullOrEmpty(location.id))
            {
                Debug.LogWarning("CameraManager: a camera location has a blank id; skipping it.", this);
                continue;
            }
            if (location.loc == null)
            {
                Debug.LogWarning($"CameraManager: camera location '{location.id}' has no transform assigned.", this);
                continue;
            }
            if (lookup.ContainsKey(location.id))
            {
                Debug.LogWarning($"CameraManager: duplicate camera location id '{location.id}'; keeping the first.", this);
                continue;
            }

            lookup.Add(location.id, location);
        }
    }

    public void SwitchTo(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("CameraManager: SwitchTo was called with a blank id.", this);
            return;
        }
        if (!lookup.ContainsKey(id))
        {
            Debug.LogWarning($"CameraManager: no camera location named '{id}'.", this);
            return;
        }
        targetId = id;
    }

    public void SetFollowMode(bool on)
    {
        followMode = on;
    }
    public void SetAiming(bool on, float pitch)
    {
        aiming = on;
        aimPitch = pitch;
    }


    void LateUpdate()
    {
        if (cameraTransform == null) return;

        WarnIfSomethingElseMovedCamera();

        bool wrote = (followMode && followDeer != null) ? ApplyFollow() : ApplyFixed();

        wroteLastFrame = wrote;
        if (wrote)
        {
            lastWrittenPos = cameraTransform.position;
            lastWrittenRot = cameraTransform.rotation;
        }
    }

    bool ApplyFollow()
    {
        Vector3 desiredPos;
        Quaternion desiredRot;
        if (aiming && aimEyePoint != null)
        {
            desiredPos = aimEyePoint.position;
            desiredRot = Quaternion.Euler(aimPitch, followDeer.eulerAngles.y, 0f);
        }
        else {
            desiredPos = followDeer.TransformPoint(followOffset);
            Vector3 lookAt = followDeer.position + Vector3.up * followHeightLookOffset;
            Vector3 toLook = lookAt - desiredPos;
           
            desiredRot = toLook.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(toLook, Vector3.up)
            : cameraTransform.rotation;
        }
        
        
        

        
        bool cut = !hasApplied || (!appliedFollow && !blendIntoFollow);

        if (cut)
        {
            cameraTransform.SetPositionAndRotation(desiredPos, desiredRot);
        }
        else
        {
            float t = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
            cameraTransform.SetPositionAndRotation(
                Vector3.Lerp(cameraTransform.position, desiredPos, t),
                Quaternion.Slerp(cameraTransform.rotation, desiredRot, t));
        }
        UpdateFirstPersonHiding();
        hasApplied = true;
        appliedFollow = true;
        return true;
    }
    void UpdateFirstPersonHiding()
    {
        if (cam == null) return;

        bool close = aiming && aimEyePoint != null &&
                     Vector3.Distance(cameraTransform.position, aimEyePoint.position) < hideDistance;

        if (close)
        {
            cam.cullingMask = defaultCullingMask & ~hidewhenAiming;
            cam.nearClipPlane = aimNearClip;
        }
        else
        {
            cam.cullingMask = defaultCullingMask;
            cam.nearClipPlane = defaultNearClip;
        }
    }

    bool ApplyFixed()
    {
        UpdateFirstPersonHiding();
        bool returningFromFollow = hasApplied && appliedFollow;
        CameraLocation target = CurrentTarget();

        if (returningFromFollow || target == null)
        {
            CameraLocation resolved = ResolveLocationForDeer();
            if (resolved != null)
            {
                target = resolved;
                targetId = resolved.id;
            }
        }

        if (target == null || target.loc == null) return false;

        // Any change of shot is a hard cut, rotation included.
        bool cut = !hasApplied || appliedFollow || appliedTargetId != targetId;

        cameraTransform.position = target.loc.position;

        if (target.trackSubject && followDeer != null)
        {
            // Tracking: stay at the marker, turn to face the deer every frame.
            Vector3 lookAt = followDeer.position + Vector3.up * followHeightLookOffset;
            Vector3 dir = lookAt - target.loc.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRot = Quaternion.LookRotation(dir, Vector3.up);
                if (cut)
                {
                    cameraTransform.rotation = desiredRot;
                }
                else
                {
                    float t = 1f - Mathf.Exp(-trackSharpness * Time.unscaledDeltaTime);
                    cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRot, t);
                }
            }
        }
        else
        {
            // Locked shot: use the angle framed on the marker.
            cameraTransform.rotation = target.loc.rotation;
        }

        hasApplied = true;
        appliedFollow = false;
        appliedTargetId = targetId;
        return true;
    }

    CameraLocation CurrentTarget()
    {
        if (string.IsNullOrEmpty(targetId)) return null;
        lookup.TryGetValue(targetId, out CameraLocation location);
        return location;
    }

    /// where deer zone
    CameraLocation ResolveLocationForDeer()
    {
        if (followDeer == null)
            return NearestLocation(cameraTransform.position);

        CameraLocation fromZone = LocationFromZoneAt(followDeer.position + Vector3.up * zoneProbeHeight);
        if (fromZone != null)
            return fromZone;

        return NearestLocation(followDeer.position);
    }

    CameraLocation LocationFromZoneAt(Vector3 point)
    {
        Collider[] hits = Physics.OverlapSphere(point, 0.05f, zoneLayers, QueryTriggerInteraction.Collide);
        CameraLocation found = null;

        foreach (var hit in hits)
        {
            var zone = hit.GetComponent<CameraZone>();
            if (zone == null || string.IsNullOrEmpty(zone.locationId)) continue;
            if (!lookup.TryGetValue(zone.locationId, out CameraLocation location)) continue;

            // Standing where two zones overlap (a doorway): keep the current
            // shot if it's one of them, so the camera doesn't flip needlessly.
            if (location.id == targetId) return location;
            if (found == null) found = location;
        }
        return found;
    }

    CameraLocation NearestLocation(Vector3 point)
    {
        CameraLocation best = null;
        float bestDist = float.MaxValue;

        foreach (var location in lookup.Values)
        {
            if (location.loc == null) continue;
            float d = (location.loc.position - point).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = location;
            }
        }
        return best;
    }


    void CheckSetup()
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("CameraManager: Camera Transform is empty and no camera is tagged MainCamera. Nothing will move.", this);
        }
        else if (cameraTransform.GetComponent<Camera>() == null)
        {
            Debug.LogWarning($"CameraManager: Camera Transform is '{cameraTransform.name}', which has no Camera component. " +
                             "Drag the Main Camera in, not a marker.", this);
        }

        if (followDeer == null)
        {
            Debug.LogWarning("CameraManager: Follow Deer is empty, so tracking and follow mode can't work. " +
                             "Drag the deer from the Hierarchy into it.", this);
            return;
        }

        if (!followDeer.gameObject.scene.IsValid())
        {
            Debug.LogWarning("CameraManager: Follow Deer is the deer prefab from the Project window, which never moves. " +
                             "Drag the deer from the Hierarchy instead.", this);
            return;
        }

        if (followDeer.GetComponent<CharacterController>() == null)
        {
            var mover = followDeer.GetComponentInChildren<CharacterController>();
            if (mover != null)
                Debug.LogWarning($"CameraManager: Follow Deer is '{followDeer.name}', but the object that actually moves is its child " +
                                 $"'{mover.name}'. Drag that one in instead.", this);
        }
    }

    void WarnIfSomethingElseMovedCamera()
    {
        if (!wroteLastFrame || warnedOtherWriter) return;

        bool moved = (cameraTransform.position - lastWrittenPos).sqrMagnitude > 0.000001f
                  || Quaternion.Angle(cameraTransform.rotation, lastWrittenRot) > 0.01f;
        if (!moved) return;

        warnedOtherWriter = true;
        Debug.LogWarning("CameraManager: something else moved the camera since last frame. Look for another camera script, " +
                         "a Cinemachine Brain or an Animator on the camera, or the camera being a child of the deer. " +
                         "Remove it, or the two will fight.", cameraTransform);
    }

    void OnDrawGizmos()
    {
        if (cameraLocations == null) return;
        Gizmos.color = Color.cyan;
        foreach (var location in cameraLocations)
        {
            if (location == null || location.loc == null) continue;
            Gizmos.DrawWireSphere(location.loc.position, 0.25f);
            Gizmos.DrawRay(location.loc.position, location.loc.forward * 1.5f);
        }
    }

    void OnDrawGizmosSelected()
    {
        
        if (followDeer == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(followDeer.position + Vector3.up * zoneProbeHeight, 0.1f);
    }
}
