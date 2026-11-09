using UnityEngine;
using System.Collections;

public class Parry_Swing : MonoBehaviour
{
    [SerializeField] KeyCode k_swing = KeyCode.V;
    [SerializeField] KeyCode k_parryn = KeyCode.F;
    [SerializeField] Vector3 swordLocalEuler = new Vector3(90f, 0f, 0f);
    [SerializeField] bool hideWhenIdle = true;
    [SerializeField] GameObject swordPrefab;
    [SerializeField] float pivotHeight = 1f;
    [SerializeField] float radius = 1.5f;
    [SerializeField] float startAngle = -110f;
    [SerializeField] float endAngle = 110f;
    [SerializeField] float swingDuration = 0.25f;
    [SerializeField] AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] bool alternateDirection = true;

    Transform pivot;
    GameObject sword;
    SwordHitbox hitbox;
    bool swinging;
    bool reverseNext;


    void Awake()
    {
        
        pivot = new GameObject("SwordPivot").transform;
        pivot.SetParent(transform, false);

        if (swordPrefab != null)
        {
            sword = Instantiate(swordPrefab, pivot);
            sword.SetActive(!hideWhenIdle);
            hitbox = sword.GetComponent<SwordHitbox>();
            
        }
        

        pivot.localRotation = Quaternion.Euler(0f, startAngle, 0f);
        ApplyLayout();
    }

    void Update()
    {
        // Re-applied every frame so you can tweak values live in Play mode.
        ApplyLayout();

        if (Input.GetKeyDown(k_swing) && !swinging && sword != null)
        {
            StartCoroutine(Swing());
        }

        if (Input.GetKeyDown(k_parryn))
        {
            // Parry goes here later.
        }

        
    }

    void ApplyLayout()
    {
        pivot.localPosition = new Vector3(0f, pivotHeight, 0f);

        if (sword != null)
        {
            sword.transform.localPosition = new Vector3(0f, 0f, radius);
            sword.transform.localRotation = Quaternion.Euler(swordLocalEuler);
            if (hitbox != null) hitbox.BeginSwing(transform);
        }
    }

    IEnumerator Swing()
    {
        swinging = true;
        sword.SetActive(true);

        float from = reverseNext ? endAngle : startAngle;
        float to = reverseNext ? startAngle : endAngle;
        if (alternateDirection) reverseNext = !reverseNext;

        float t = 0f;
        while (t < 1f)
        {
            t += PlayerDeltaTime() / Mathf.Max(swingDuration, 0.01f);
            float angle = Mathf.LerpUnclamped(from, to, swingCurve.Evaluate(Mathf.Clamp01(t)));
            pivot.localRotation = Quaternion.Euler(0f, angle, 0f);
            yield return null;
        }
        if (hitbox != null) hitbox.EndSwing();
        if (hideWhenIdle) sword.SetActive(false);
        swinging = false;
    }


    float PlayerDeltaTime()
    {
        return Time.timeScale > 0f ? Time.deltaTime / Time.timeScale : 0f;
    }
}
