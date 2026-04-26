using UnityEngine;
using System;

public class QuotaManager : MonoBehaviour
{
    [Header("Zone Progression")]
    [Tooltip("Cumulative kills required to complete each zone. E.g., Zone 1 ends at 50, Zone 2 at 150, Zone 3 at 300.")]
    public int[] zoneKillQuotas = { 50, 150, 300 };
    
    // Made static so progression persists across scene loads
    public static int currentKills = 0;
    public static int currentZoneIndex = 0;

    [Header("Level Transition")]
    [Tooltip("The elevator object that appears when the zone is cleared.")]
    public GameObject levelElevator;

    // Events to broadcast progression state to the UI, Spawner, or Game Manager
    public static event Action<int, int> OnKillCountUpdated; // Sends (currentKills, targetQuota)
    public static event Action OnZoneCleared;                // Broadcasted when a zone is finished to stop spawners
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
            
        if (levelElevator != null)
            levelElevator.SetActive(false); // Hide elevator at the start
    }

    // Call this from your Game Over or Main Menu script when starting a fresh run!
    public static void ResetProgression()
    {
        currentKills = 0;
        currentZoneIndex = 0;
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
            if (AudioManager.Instance != null && AudioManager.Instance.levelCompleteSound != null)
            {
                // Play as a global 2D sound (spatialBlend = 0f) with highest priority (0)
                AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.levelCompleteSound, Vector3.zero, AudioManager.Instance.levelCompleteVolume, 1f, 0, 0f);
            }
            OnGameCompleted?.Invoke();
            // TODO: Hook into FSM/GameManager to show Victory Screen and log clear time
        }
        else
        {
            Debug.Log($"Zone Cleared! Head to the elevator to advance to Zone {currentZoneIndex + 1}!");
            if (AudioManager.Instance != null && AudioManager.Instance.levelCompleteSound != null)
            {
                // Play as a global 2D sound (spatialBlend = 0f) with highest priority (0)
                AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.levelCompleteSound, Vector3.zero, AudioManager.Instance.levelCompleteVolume, 1f, 0, 0f);
            }
            OnZoneCleared?.Invoke(); // Tell spawners to stop
            
            if (levelElevator != null)
                levelElevator.SetActive(true); // Reveal the elevator
        }
    }
}