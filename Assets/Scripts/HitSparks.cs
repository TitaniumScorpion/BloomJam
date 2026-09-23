using UnityEngine;

/// <summary>
/// Neutral home for the "player attack connected" impact effect — a burst of metal sparks at
/// the point of contact, complementing the hit flash each enemy plays on itself.
///
/// Spawned by the *attacker*, not by the enemy's TakeDamage, for three reasons:
///  - TakeDamage(int) has no contact point, so an enemy could only emit from its own pivot,
///    which for a flying drone is the middle of the mesh.
///  - EnemyWeakPoint.TakeDamage forwards to parentEnemy.TakeDamage, so a receiver-side hook
///    would fire two bursts for one weak-point hit.
///  - Every attacker already holds the geometry (a Collider, or a full RaycastHit).
///
/// Deliberately does nothing about bullet time. Bullet time does not touch Time.timeScale and
/// player projectiles do not freeze either, so sparks continuing at full speed while enemies
/// hang still is the consistent behaviour here, not an oversight.
/// </summary>
public static class HitSparks
{
    /// <summary>Pool tag on the ObjectPooler. The prefab behind it is swappable — nothing here cares.</summary>
    public const string PoolTag = "HitSpark";

    /// <summary>
    /// Bursts sparks at a world point. <paramref name="awayFromSurface"/> is the direction the
    /// spray should travel — i.e. back toward whatever landed the hit. It does not need to be
    /// normalized and may be zero, which falls back to spraying straight up.
    /// </summary>
    public static void Spawn(Vector3 point, Vector3 awayFromSurface)
    {
        if (ObjectPooler.Instance == null) return;

        // LookRotation throws on a zero vector, and a grazing hit can produce one
        Quaternion rotation = awayFromSurface.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(awayFromSurface)
            : Quaternion.LookRotation(Vector3.up);

        ObjectPooler.Instance.SpawnFromPool(PoolTag, point, rotation);
    }

    /// <summary>
    /// Overload for attacks that only know which collider they overlapped. Resolves a point on
    /// the enemy's surface rather than using the attacker's own position, which at 80 m/s can
    /// sit a trigger-radius short of the hit.
    ///
    /// Call this BEFORE IDamageable.TakeDamage, never after. Most enemies here die in one hit,
    /// and a dead enemy deactivates itself - once its collider leaves the physics scene
    /// ClosestPoint cannot resolve and every burst silently lands on world origin.
    /// </summary>
    public static void SpawnOnCollider(Collider hitCollider, Vector3 attackerPosition, Vector3 awayFromSurface)
    {
        // ClosestPoint degrades to returning the input point on a non-convex MeshCollider
        // rather than throwing, so no collider-shape guard is needed
        Spawn(hitCollider.ClosestPoint(attackerPosition), awayFromSurface);
    }
}
