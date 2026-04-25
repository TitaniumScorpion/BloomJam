using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Set to 1 for true fragility, or higher if you want the player to survive a few hits.")]
    public int maxHealth = 1;
    private int currentHealth;

    // Event broadcasted when the player dies
    public static event Action OnPlayerDied;

    private void Start()
    {
        currentHealth = maxHealth;
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
        gameObject.SetActive(false); // Disables the player entirely
    }
}