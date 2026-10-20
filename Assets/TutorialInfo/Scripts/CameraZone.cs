using UnityEngine;

/// <summary>
/// Put on a GameObject with a BoxCollider set to Is Trigger.
/// When the player enters, tells the manager to switch to this zone's camera.
/// Only reacts to enter (never reverts on exit), so overlapping zones at
/// doorways don't clobber each other.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CameraZone : MonoBehaviour
{
    public CameraManager manager;
    [Tooltip("Must match an id in the CameraManager's list exactly.")]
    public string locationId;
    public string playerTag = "Player";

    void Reset()
    {
        // Auto-tick Is Trigger when the component is first added.
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (manager == null)
        {
            Debug.LogWarning($"CameraZone '{name}' has no manager assigned.", this);
            return;
        }
        manager.SwitchTo(locationId);
    }

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
    }
}
