using UnityEngine;

public class FrameRateLimiter : MonoBehaviour
{
    [SerializeField] private int m_TargetFps = 30;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = m_TargetFps;
    }
}