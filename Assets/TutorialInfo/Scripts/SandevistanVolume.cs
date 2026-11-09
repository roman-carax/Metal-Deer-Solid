using UnityEngine.Rendering;
using UnityEngine;


public class SandevistanVolume : MonoBehaviour
{
    public Volume volume;

    public float slowScale = 0.2f;

    public float fadeInTime = 0.35f;
    public float fadeOutTime = 0.6f;

    void Start()
    {
        if (volume != null)
        {
            volume.weight = 0;
            volume.enabled = false;
        }
    }

    void Update()
    {
        if (volume == null) return;
        if (Time.timeScale <= 0f) return;

        float target = Mathf.InverseLerp(1f, slowScale, Time.timeScale);
        if (target > 0f && !volume.enabled)
        {
            volume.enabled = true;
        }

        bool fadingIn = target > volume.weight;
        float duration = fadingIn ? fadeInTime : fadeOutTime;
        float step = duration > 0f ? Time.unscaledDeltaTime / duration : 1f;

        volume.weight = Mathf.MoveTowards(volume.weight, target, step);
        if (volume.weight <= 0f && target <= 0f && volume.enabled)
        {
            volume.enabled = false;
        }
    }

    void OnDisable()
    {
        if (volume != null) volume.weight = 0f;
    }
}