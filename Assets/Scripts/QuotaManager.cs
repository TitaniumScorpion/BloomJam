using UnityEngine;
using System;

public class QuotaManager : MonoBehaviour
{
    [Header("Zone Progression")]
    [Tooltip("The total kills needed to clear each zone (Index 0 = Zone 1, Index 1 = Zone 2, etc.)")]
    public int[] targetQuotas = { 50, 100, 150 };
    
    [Tooltip("The index of the final zone that triggers victory (e.g., 2 for Zone 3)")]
    public int finalZoneIndex = 2;
    
    // Made static so progression persists across scene loads
    public static int currentKills = 0;
    public static int currentZoneIndex = 0;
    private bool zoneCleared = false;

    [Header("Level Transition")]
    [Tooltip("The elevator object that appears when the zone is cleared.")]
    public GameObject[] levelElevators; // Assign one elevator per zone here

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
        zoneCleared = false;

        // Initialize the UI with starting values for this specific zone
        int currentQuota = (currentZoneIndex < targetQuotas.Length) ? targetQuotas[currentZoneIndex] : 50;
        OnKillCountUpdated?.Invoke(currentKills, currentQuota);
            
        // Hide all elevators at start
        foreach (GameObject elevator in levelElevators)
        {
            if (elevator != null) elevator.SetActive(false);
        }
    }

    // Called by GameManager when actually starting the next zone after the elevator ride
    public void StartNextZone()
    {
        currentKills = 0;
        zoneCleared = false;
        int currentQuota = (currentZoneIndex < targetQuotas.Length) ? targetQuotas[currentZoneIndex] : 50;
        OnKillCountUpdated?.Invoke(currentKills, currentQuota);
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
        int currentQuota = (currentZoneIndex < targetQuotas.Length) ? targetQuotas[currentZoneIndex] : 50;
        OnKillCountUpdated?.Invoke(currentKills, currentQuota);
        
        if (zoneCleared) return;

        // Check if we reached the milestone for the current zone
        if (currentKills >= currentQuota)
        {
            zoneCleared = true;
            AdvanceZone();
        }
    }

    private void AdvanceZone()
    {
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
            Debug.Log($"Zone Cleared! Head to the elevator to advance to Zone {currentZoneIndex + 1}!");
            if (AudioManager.Instance != null && AudioManager.Instance.levelCompleteSound != null)
            {
                // Play as a global 2D sound (spatialBlend = 0f) with highest priority (0)
                AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.levelCompleteSound, Vector3.zero, AudioManager.Instance.levelCompleteVolume, 1f, 0, 0f);
            }
            OnZoneCleared?.Invoke(); // Tell spawners to stop
            
            if (currentZoneIndex < levelElevators.Length && levelElevators[currentZoneIndex] != null)
            {
                levelElevators[currentZoneIndex].SetActive(true); // Reveal the elevator for the current zone
            }

            // Increment the index so the GameManager knows which zone to load next
            currentZoneIndex++;
        }
    }
}