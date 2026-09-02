using System;

/// <summary>
/// Neutral home for enemy-wide gameplay events.
///
/// The kill event used to live on StandardSwarmer, which meant unrelated enemies
/// (AdvancedEnemy, SpawnerDrone, DasherEnemy) all had to call
/// StandardSwarmer.ReportDeath() to register a kill they had nothing to do with.
/// Anything that dies and should count toward the zone quota reports it here instead.
/// </summary>
public static class EnemyEvents
{
    /// <summary>Raised whenever any enemy dies in a way that counts toward the zone quota.</summary>
    public static event Action OnEnemyDied;

    public static void ReportDeath() => OnEnemyDied?.Invoke();
}
