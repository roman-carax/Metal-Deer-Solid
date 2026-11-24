using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// TEMPORARY: swaps the Sandevistan from "world slows down" to "deer speeds up".
// Put this on the same object as SandevistanController. Remove it to go back to slow-mo.
[DefaultExecutionOrder(-1000)] // runs before the deer's scripts, so they read the new multiplier this frame
public class SandevistanSpeedMode : MonoBehaviour
{
    [Header("Speed-up")]
    public float speedUp = 3f;       // 3 = deer acts 3x faster
    public float rampSpeed = 15f;    // how quickly it reaches speedUp

    [Header("Timing (real seconds)")]
    public float duration = 4f;
    public float cooldown = 6f;
    public KeyCode key = KeyCode.E;

    [Header("Screen effect")]
    public SandevistanVolume volumeDriver;      // auto-found if left empty
    public bool keepColorAdjustments = false;   // the green tint / color filter
    public bool keepVignette = false;
    public bool keepChromaticAberration = false;
    public bool keepLensDistortion = true;      // the fisheye
    public float fadeInTime = 0.35f;
    public float fadeOutTime = 0.6f;

    SandevistanController sande;
    Volume volume;
    bool active;
    float activeTimer;
    float cooldownTimer;
    float multiplier = 1f;
    bool warned;

    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly string[] activeNames = { "SandyOn", "sandyOn", "IsActive", "isActive" };

    void Awake()
    {
        sande = GetComponent<SandevistanController>();
        if (sande == null)
        {
            Debug.LogError("SandevistanSpeedMode: no SandevistanController on this object.");
            enabled = false;
            return;
        }

        if (volumeDriver == null) volumeDriver = FindObjectOfType<SandevistanVolume>();
        if (volumeDriver != null) volume = volumeDriver.volume;
    }

    void OnEnable()
    {
        if (sande == null) return;

        // Stop the original controller so it never touches Time.timeScale.
        sande.enabled = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // Take over the volume from SandevistanVolume (which fades on timeScale).
        if (volumeDriver != null) volumeDriver.enabled = false;

        if (volume != null)
        {
            VolumeProfile profile = volume.profile; // runtime copy, your profile asset isn't changed
            SetEffect<ColorAdjustments>(profile, keepColorAdjustments);
            SetEffect<Vignette>(profile, keepVignette);
            SetEffect<ChromaticAberration>(profile, keepChromaticAberration);
            SetEffect<LensDistortion>(profile, keepLensDistortion);

            volume.weight = 0f;
            volume.enabled = false;
        }
    }

    void OnDisable()
    {
        if (sande == null) return;

        active = false;
        multiplier = 1f;
        PushToController();

        sande.enabled = true;
        if (volumeDriver != null) volumeDriver.enabled = true;
    }

    void Update()
    {
        float realDt = Time.unscaledDeltaTime;

        if (cooldownTimer > 0f) cooldownTimer -= realDt;

        if (Input.GetKeyDown(key) && !active && cooldownTimer <= 0f)
        {
            active = true;
            activeTimer = duration;
        }

        if (active)
        {
            activeTimer -= realDt;
            if (activeTimer <= 0f)
            {
                active = false;
                cooldownTimer = cooldown;
            }
        }

        multiplier = Mathf.MoveTowards(multiplier, active ? speedUp : 1f, rampSpeed * realDt);

        PushToController();
        UpdateVolume(realDt);
    }

    // Writes the multiplier and on/off flag into SandevistanController,
    // so SimpleCreatureMover, the animator, afterimages etc. keep working unchanged.
    void PushToController()
    {
        bool ok = SetMember("PlayerTimeMultiplier", multiplier);
        foreach (string n in activeNames) SetMember(n, active);

        if (!ok && !warned)
        {
            warned = true;
            Debug.LogWarning("SandevistanSpeedMode: couldn't find PlayerTimeMultiplier on SandevistanController.");
        }
    }

    bool SetMember(string name, object value)
    {
        System.Type t = sande.GetType();

        PropertyInfo p = t.GetProperty(name, Flags);
        if (p != null && p.CanWrite && p.PropertyType == value.GetType())
        {
            p.SetValue(sande, value);
            return true;
        }

        FieldInfo f = t.GetField(name, Flags) ?? t.GetField("<" + name + ">k__BackingField", Flags);
        if (f != null && f.FieldType == value.GetType())
        {
            f.SetValue(sande, value);
            return true;
        }

        return false;
    }

    void UpdateVolume(float realDt)
    {
        if (volume == null) return;

        if (active && !volume.enabled) volume.enabled = true;

        float target = active ? 1f : 0f;
        float time = active ? fadeInTime : fadeOutTime;
        volume.weight = time > 0f ? Mathf.MoveTowards(volume.weight, target, realDt / time) : target;

        if (!active && volume.weight <= 0f) volume.enabled = false;
    }

    static void SetEffect<T>(VolumeProfile profile, bool on) where T : VolumeComponent
    {
        if (profile.TryGet(out T effect)) effect.active = on;
    }
}