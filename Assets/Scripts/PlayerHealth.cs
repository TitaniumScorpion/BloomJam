using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Set to 1 for true fragility, or higher if you want the player to survive a few hits.")]
    public int maxHealth = 1;
    private int currentHealth;
    
    [Header("Environment Hazards")]
    [Tooltip("If the player falls below this Y position, they die instantly.")]
    public float killHeight = -15f; 

    // Event broadcasted when the player dies
    public static event Action OnPlayerDied;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // Instantly kill the player if they fall off the arena
        if (transform.position.y < killHeight && currentHealth > 0)
        {
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return; // Prevent double-triggering death

        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage! Current health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player died! Game Over.");
        OnPlayerDied?.Invoke();
        
        // Detach the camera so it stays active and we don't get a "No cameras rendering" error
        Camera playerCam = GetComponentInChildren<Camera>();
        if (playerCam != null)
        {
            playerCam.transform.SetParent(null);
        }

        gameObject.SetActive(false); // Disables the player entirely
    }
}