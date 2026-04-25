using UnityEngine;
using System;

public class QuotaManager : MonoBehaviour
{
    [Header("Zone Progression")]
    [Tooltip("Cumulative kills required to complete each zone. E.g., Zone 1 ends at 50, Zone 2 at 150, Zone 3 at 300.")]
    public int[] zoneKillQuotas = { 50, 150, 300 };
    
    private int currentKills = 0;
    private int currentZoneIndex = 0;

    // Events to broadcast progression state to the UI, Spawner, or Game Manager
    public static event Action<int, int> OnKillCountUpdated; // Sends (currentKills, targetQuota)
    public static event Action<int> OnZoneAdvanced;          // Sends (newZoneIndex)
    public static event Action OnGameCompleted;              // Broadcasted when all 3 zones are beaten

    private void OnEnable()
    {
        // Subscribe to the enemy death event when this manager becomes active
        StandardSwarmer.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        // ALWAYS unsubscribe from static events when disabled to prevent memory leaks
        StandardSwarmer.OnEnemyDied -= HandleEnemyDied;
    }

    private void Start()
    {
        // Initialize the UI with starting values
        if (zoneKillQuotas.Length > 0)
            OnKillCountUpdated?.Invoke(currentKills, zoneKillQuotas[currentZoneIndex]);
    }

    private void HandleEnemyDied()
    {
        currentKills++;
        
        // If we've already beaten the game, just keep tracking extra kills
        if (currentZoneIndex >= zoneKillQuotas.Length) 
        {
            OnKillCountUpdated?.Invoke(currentKills, currentKills);
            return;
        }

        int targetQuota = zoneKillQuotas[currentZoneIndex];
        OnKillCountUpdated?.Invoke(currentKills, targetQuota);

        // Check if we reached the milestone for the current zone
        if (currentKills >= targetQuota)
        {
            AdvanceZone();
        }
    }

    private void AdvanceZone()
    {
        currentZoneIndex++;

        if (currentZoneIndex >= zoneKillQuotas.Length)
        {
            Debug.Log("All zones cleared! Game Completed!");
            OnGameCompleted?.Invoke();
            // TODO: Hook into FSM/GameManager to show Victory Screen and log clear time
        }
        else
        {
            Debug.Log($"Advanced to Zone {currentZoneIndex + 1}!");
            OnZoneAdvanced?.Invoke(currentZoneIndex);
            // TODO: Trigger Elite Boss spawn or environmental changes for the next zone
        }
    }
}