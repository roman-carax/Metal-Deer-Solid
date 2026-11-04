using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class EnemyHealth : MonoBehaviour
{
    
    [SerializeField] private float m_MaxHealth = 100f;
    [SerializeField] private bool m_DestroyOnDeath = true;
    [SerializeField] private float m_DestroyDelay = 0f;

    
    [SerializeField] private Renderer m_Renderer;
    [SerializeField] private Color m_FlashColor = Color.red;
    [SerializeField] private float m_FlashDuration = 0.1f;

    
    public UnityEvent<float> OnDamaged;
    public UnityEvent OnDied;

    private float m_CurrentHealth;
    private bool m_IsDead;
    private Color m_OriginalColor;
    private Coroutine m_FlashRoutine;

    public float CurrentHealth => m_CurrentHealth;
    public float MaxHealth => m_MaxHealth;
    public bool IsDead => m_IsDead;

    private void Awake()
    {
        m_CurrentHealth = m_MaxHealth;

        if (m_Renderer == null) m_Renderer = GetComponentInChildren<Renderer>();
        if (m_Renderer != null) m_OriginalColor = m_Renderer.material.color;
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (m_IsDead || amount <= 0f) return;

        m_CurrentHealth = Mathf.Max(m_CurrentHealth - amount, 0f);
        Debug.Log($"{name} took {amount} damage at {hitPoint}. HP: {m_CurrentHealth}/{m_MaxHealth}");

        OnDamaged?.Invoke(m_CurrentHealth);
        Flash();

        if (m_CurrentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (m_IsDead || amount <= 0f) return;
        m_CurrentHealth = Mathf.Min(m_CurrentHealth + amount, m_MaxHealth);
    }

    private void Die()
    {
        m_IsDead = true;
        Debug.Log($"{name} died");
        OnDied?.Invoke();

        if (m_DestroyOnDeath) Destroy(gameObject, m_DestroyDelay);
    }

    private void Flash()
    {
        if (m_Renderer == null) return;
        if (m_FlashRoutine != null) StopCoroutine(m_FlashRoutine);
        m_FlashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        m_Renderer.material.color = m_FlashColor;
        yield return new WaitForSeconds(m_FlashDuration);
        if (m_Renderer != null) m_Renderer.material.color = m_OriginalColor;
        m_FlashRoutine = null;
    }
}