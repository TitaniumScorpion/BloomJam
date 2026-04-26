using UnityEngine;
using System;

public class QuotaManager : MonoBehaviour
{
    [Header("Zone Progression")]
    [Tooltip("The total kills needed to clear THIS specific zone (e.g., 50 for Zone 1, 100 for Zone 2, 150 for Zone 3)")]
    public int targetQuota = 50;
    
    [Tooltip("Check this ONLY in the final zone (Zone 3) to trigger the Victory Screen instead of an elevator")]
    public bool isFinalZone = false;
    
    // Made static so progression persists across scene loads
    public static int currentKills = 0;
    public static int currentZoneIndex = 0;
    private bool zoneCleared = false;

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
        // Reset kills at the start of every zone so the quota starts at 0
        currentKills = 0;

        // Initialize the UI with starting values for this specific zone
        OnKillCountUpdated?.Invoke(currentKills, targetQuota);
            
        if (levelElevator != null)
            levelElevator.SetActive(false); // Hide elevator at the start
        else if (!isFinalZone)
            Debug.LogWarning("Level Elevator is missing! Please assign it in the Inspector.");
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
        OnKillCountUpdated?.Invoke(currentKills, targetQuota);
        
        if (zoneCleared) return;

        // Check if we reached the milestone for the current zone
        if (currentKills >= targetQuota)
        {
            zoneCleared = true;
            AdvanceZone();
        }
    }

    private void AdvanceZone()
    {
        currentZoneIndex++;

        if (isFinalZone)
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