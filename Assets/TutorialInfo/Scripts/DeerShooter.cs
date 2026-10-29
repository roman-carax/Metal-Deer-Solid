using UnityEngine;
using static UnityEngine.UI.Image;
using System.Collections;

public class DeerShooter : MonoBehaviour
{
    [SerializeField] private float m_AimFov = 35f;        // zoomed-in field of view
    [SerializeField] private float m_AimZoomSpeed = 12f;  // how fast it zooms in/out
    private float m_DefaultFov;
    public bool IsAiming { get; private set; }
    [SerializeField] private LayerMask m_ShootableLayers;
    [SerializeField] private float m_FireCooldown = 0.5f;
    [SerializeField] private CameraManager m_CameraManager;
    [SerializeField] private Camera m_Camera;
    [SerializeField] private float m_Range = 100f;
    [SerializeField] private Transform m_FirePoint;
    [SerializeField] private LineRenderer m_Tracer;
    [SerializeField] private float m_TracerDuration = 0.08f;
    [SerializeField] private float m_PitchSensitivity = 2f;
    [SerializeField] private float m_MinPitch = -60f;
    [SerializeField] private float m_MaxPitch = 60f;
    [SerializeField] private GameObject m_Reticle;
    private float m_AimPitch;
    private Coroutine m_TracerRoutine;
    private float m_LastFireTime=-999f;
    void Awake()
    {
        if (m_Camera != null)
            m_DefaultFov = m_Camera.fieldOfView;
        if (m_Tracer != null)
        {
            m_Tracer.positionCount = 2;
            m_Tracer.useWorldSpace = true;
            m_Tracer.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool followingNow = m_CameraManager != null && m_CameraManager.IsFollowing;
        IsAiming = followingNow && Input.GetMouseButton(1);
        if (IsAiming)
        {
            m_AimPitch -= Input.GetAxis("Mouse Y") * m_PitchSensitivity;
            m_AimPitch = Mathf.Clamp(m_AimPitch, m_MinPitch, m_MaxPitch);
        }
        else
        {
            m_AimPitch = 0f;
        }

        if (m_CameraManager != null)
            m_CameraManager.SetAiming(IsAiming, m_AimPitch);

        if (m_Reticle != null && m_Reticle.activeSelf != IsAiming)
            m_Reticle.SetActive(IsAiming);
        if (m_Camera != null)
        {
            float targetFov = IsAiming ? m_AimFov : m_DefaultFov;
            float t = 1f - Mathf.Exp(-m_AimZoomSpeed * Time.deltaTime);
            m_Camera.fieldOfView = Mathf.Lerp(m_Camera.fieldOfView, targetFov, t);
        }
        bool following = m_CameraManager != null && m_CameraManager.IsFollowing;
        if (Input.GetButtonDown("Fire1") && Time.time - m_LastFireTime >= m_FireCooldown)
        {
            
            m_LastFireTime = Time.time;
            Vector3 aimPoint;
            if (following)
            {
                if(Physics.Raycast(m_Camera.transform.position, m_Camera.transform.forward, out RaycastHit hit, Mathf.Infinity, m_ShootableLayers, QueryTriggerInteraction.Ignore))
                {
                    aimPoint = hit.point;
                }
                else
                {
                    aimPoint = m_Camera.transform.position + m_Camera.transform.forward * m_Range;
                }
                Vector3 dir = (aimPoint - m_FirePoint.position).normalized;
                if (Physics.Raycast(m_FirePoint.position, dir, out RaycastHit shotHit, m_Range, m_ShootableLayers, QueryTriggerInteraction.Ignore))
                {
                    Debug.DrawLine(m_FirePoint.position, shotHit.point, Color.red, 1f);
                    Debug.Log("Hit: " + shotHit.collider.name);
                    ShowTracer(m_FirePoint.position, shotHit.point);
                }
                else
                {
                    Debug.DrawRay(m_FirePoint.position, dir * m_Range, Color.yellow, 1f);
                    ShowTracer(m_FirePoint.position, m_FirePoint.position + dir * m_Range);
                }
            }
            

        }
       
        
    }
    private void ShowTracer(Vector3 start, Vector3 end)
    {
        if (m_Tracer == null) return;

        if (m_TracerRoutine != null)
            StopCoroutine(m_TracerRoutine);

        m_TracerRoutine = StartCoroutine(TracerRoutine(start, end));
    }

    private IEnumerator TracerRoutine(Vector3 start, Vector3 end)
    {
        m_Tracer.SetPosition(0, start);
        m_Tracer.SetPosition(1, end);
        m_Tracer.enabled = true;

        yield return new WaitForSeconds(m_TracerDuration);

        m_Tracer.enabled = false;
        m_TracerRoutine = null;
    }
}
