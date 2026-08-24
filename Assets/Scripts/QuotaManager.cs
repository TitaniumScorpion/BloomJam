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
        IsZoneCleared = false;
    }

    // Called by UpgradeManager after the player picks their upgrade.
    // Just advances the zone index — the player now has to walk back into the
    // elevator themselves; ElevatorEntryTrigger calls GameManager.ReturnToElevatorHub() then.
    public void RevealElevator()
    {
        currentZoneIndex++;
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
            OnZoneCleared?.Invoke(); // Tell spawners to stop. UpgradeManager reveals elevator after upgrade choice.
        }
    }
}