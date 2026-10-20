using UnityEngine;

/// <summary>
/// Press (or hold) a key to switch between fixed cameras and follow-behind-the-deer.
/// Uses the legacy Input Manager to match SimpleCreatureMover.
/// </summary>
public class CameraModeToggle : MonoBehaviour
{
    public CameraManager manager;
    public KeyCode toggleKey = KeyCode.C;
    [Tooltip("If true, follow only while the key is held.")]
    public bool holdToFollow = false;

    void Update()
    {
        if (manager == null) return;

        if (holdToFollow)
            manager.SetFollowMode(Input.GetKey(toggleKey));
        else if (Input.GetKeyDown(toggleKey))
            manager.SetFollowMode(!manager.IsFollowing);
    }
}
