using UnityEngine;
using System;

public class QuotaManager : MonoBehaviour
{
    [Header("Zone Progression")]
    [Tooltip("The total kills needed to clear each zone (Index 0 = Zone 1, Index 1 = Zone 2, etc.)")]
    public int[] targetQuotas = { 50, 100, 150, 200, 250 };

    [Tooltip("The index of the final zone that triggers victory (e.g., 4 for Zone 5)")]
    public int finalZoneIndex = 4;
    
    // Made static so progression persists across scene loads
    public static int currentKills = 0;
    public static int currentZoneIndex = 0;
    public static bool IsZoneCleared { get; private set; }

    // Zones fully cleared this run. Tracked separately from currentZoneIndex because the final
    // zone clears without advancing the index — the index alone can't tell "cleared zone 5" from
    // "standing in zone 5". Used by the completion percentage on the death screen.
    public static int zonesCompleted = 0;

    // Static mirrors of the two inspector fields below, so the completion percentage can be read
    // after death without hunting for the (by then possibly disabled) QuotaManager in the scene.
    private static int[] quotaTable;
    private static int lastZoneIndex;

    // Events to broadcast progression state to the UI, Spawner, or Game Manager
    public static event Action<int, int> OnKillCountUpdated; // Sends (currentKills, targetQuota)
    public static event Action OnZoneCleared;                // Broadcasted when a zone is finished to stop spawners
    public static event Action OnGameCompleted;              // Broadcasted when all 3 zones are beaten

    private void Awake()
    {
        quotaTable = targetQuotas;
        lastZoneIndex = finalZoneIndex;
    }

    private void OnEnable()
    {
        // Subscribe to the enemy death event when this manager becomes active
        EnemyEvents.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        // ALWAYS unsubscribe from static events when disabled to prevent memory leaks
        EnemyEvents.OnEnemyDied -= HandleEnemyDied;
    }

    private void Start()
    {
        // Reset kills at the start of every zone so the quota starts at 0
        currentKills = 0;
        IsZoneCleared = false;

        // Initialize the UI with starting values for this specific zone
        int currentQuota = (currentZoneIndex < targetQuotas.Length) ? targetQuotas[currentZoneIndex] : 50;
        OnKillCountUpdated?.Invoke(currentKills, currentQuota);
            
    }

    // Called by GameManager when actually starting the next zone after the elevator ride
    public void StartNextZone()
    {
        currentKills = 0;
        IsZoneCleared = false;
        int currentQuota = (currentZoneIndex < targetQuotas.Length) ? targetQuotas[currentZoneIndex] : 50;
        OnKillCountUpdated?.Invoke(currentKills, currentQuota);
    }

    // Call this from your Game Over or Main Menu script when starting a fresh run!
    public static void ResetProgression()
    {
        currentKills = 0;
        currentZoneIndex = 0;
        zonesCompleted = 0;
        IsZoneCleared = false;
    }

    /// <summary>
    /// How far through the whole run the player got, 0-100. Counts every zone's quota:
    /// fully cleared zones contribute their entire quota, the zone in progress contributes
    /// its kills so far.
    /// </summary>
    public static float GetCompletionPercent()
    {
        if (quotaTable == null || quotaTable.Length == 0) return 0f;

        // finalZoneIndex may stop short of the array's end — only count zones that are actually played
        int zoneCount = Mathf.Min(quotaTable.Length, lastZoneIndex + 1);

        int totalQuota = 0;
        for (int i = 0; i < zoneCount; i++) totalQuota += quotaTable[i];
        if (totalQuota <= 0) return 0f;

        int killsBanked = 0;
        for (int i = 0; i < zonesCompleted && i < zoneCount; i++) killsBanked += quotaTable[i];

        // While a zone is cleared but not yet restarted, currentKills still holds that zone's
        // final tally — already banked above, so adding it again would double-count.
        if (!IsZoneCleared && currentZoneIndex < zoneCount)
            killsBanked += Mathf.Min(currentKills, quotaTable[currentZoneIndex]);

        return Mathf.Clamp01((float)killsBanked / totalQuota) * 100f;
    }

    private void HandleEnemyDied()
    {
        currentKills++;
        int currentQuota = (currentZoneIndex < targetQuotas.Length) ? targetQuotas[currentZoneIndex] : 50;
        OnKillCountUpdated?.Invoke(currentKills, currentQuota);
        
        if (IsZoneCleared) return;

        // Check if we reached the milestone for the current zone
        if (currentKills >= currentQuota)
        {
            IsZoneCleared = true;
            AdvanceZone();
        }
    }

    private void AdvanceZone()
    {
        zonesCompleted++;

        if (currentZoneIndex >= finalZoneIndex)
        {
            Debug.Log("All zones cleared! Game Completed!");
            if (AudioManager.Instance != null && AudioManager.Instance.levelCompleteSound != null)
            {
                // Play as a global 2D sound (spatialBlend = 0f) with highest priority (0)
                AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.levelCompleteSound, Vector3.zero, AudioManager.Instance.levelCompleteVolume, 1f, 0, 0f);
            }
            OnGameCompleted?.Invoke();
        }
        else
        {
            // Advance immediately — the upgrade choice is now decoupled from progression timing;
            // the player picks it whenever they focus the left panel back in the elevator.
            currentZoneIndex++;

            Debug.Log($"Zone Cleared! Head to the elevator to advance to Zone {currentZoneIndex + 1}!");
            if (AudioManager.Instance != null && AudioManager.Instance.levelCompleteSound != null)
            {
                // Play as a global 2D sound (spatialBlend = 0f) with highest priority (0)
                AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.levelCompleteSound, Vector3.zero, AudioManager.Instance.levelCompleteVolume, 1f, 0, 0f);
            }
            OnZoneCleared?.Invoke(); // Tell spawners to stop; UpgradeManager flags an upgrade as pending.
        }
    }
}