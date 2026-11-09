using System.Collections.Generic;
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    [SerializeField] float damage = 25f;

    bool active;
    Transform owner;
    readonly HashSet<EnemyHealth> hitThisSwing = new HashSet<EnemyHealth>();

    void Start()
    {
        Debug.Log($"[Sword] Spawned. Collider: {GetComponent<Collider>()}, " +
                  $"IsTrigger: {(GetComponent<Collider>() != null && GetComponent<Collider>().isTrigger)}, " +
                  $"Rigidbody: {GetComponent<Rigidbody>()}");
    }

    public void BeginSwing(Transform swinger)
    {
        Debug.Log("[Sword] BeginSwing called");
        owner = swinger;
        hitThisSwing.Clear();
        active = true;
    }

    public void EndSwing()
    {
        Debug.Log("[Sword] EndSwing called");
        active = false;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Sword] Touched {other.name} (active: {active})");
        TryHit(other);
    }

    void OnTriggerStay(Collider other) { TryHit(other); }

    void TryHit(Collider other)
    {
        if (!active) return;

        if (owner != null && other.transform.IsChildOf(owner)) return;

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null) return;
        if (enemy.IsDead) return;
        if (hitThisSwing.Contains(enemy)) return;

        hitThisSwing.Add(enemy);

        Vector3 hitPoint = other.ClosestPoint(transform.position);

        Debug.Log($"[Sword] HIT {enemy.name} for {damage}");
        enemy.TakeDamage(damage, hitPoint);
    }
}