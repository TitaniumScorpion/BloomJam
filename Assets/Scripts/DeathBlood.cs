using UnityEngine;

/// <summary>
/// Neutral home for the enemy death effect - a spray of blood that lands and stains the floor.
/// Every enemy's kill path calls this next to EnemyEvents.ReportDeath().
///
/// Kept separate from ReportDeath on purpose: that is the quota event, has no position, and
/// SpawnerDrone raises it from a weak-point callback rather than a hit.
///
/// The spray is aimed away from the player rather than along the killing blow. Kill paths do not
/// carry a hit direction - ForceDie is also reached from the katana's bullet-time resolution -
/// and "away from the player" reads correctly for every weapon anyway.
///
/// Like HitSparks, this deliberately ignores bullet time. Banked bullet-time kills only resolve
/// once it ends, so the spray almost never overlaps it.
/// </summary>
public static class DeathBlood
{
    /// <summary>Pool tag on the ObjectPooler. The prefab behind it is swappable - nothing here cares.</summary>
    public const string PoolTag = "DeathBlood";

    /// <summary>Sprays blood from a dying enemy's position, away from the player.</summary>
    public static void Spawn(Vector3 position)
    {
        if (ObjectPooler.Instance == null) return;

        Vector3 awayFromPlayer = Camera.main != null
            ? position - Camera.main.transform.position
            : Vector3.zero;

        // LookRotation throws on a zero vector - a kill point exactly at the camera would produce one
        Quaternion rotation = awayFromPlayer.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(awayFromPlayer)
            : Quaternion.LookRotation(Vector3.up);

        ObjectPooler.Instance.SpawnFromPool(PoolTag, position, rotation);
    }
}
