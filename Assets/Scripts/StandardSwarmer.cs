using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StandardSwarmer : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 1;
    private int currentHealth;
    public float moveSpeed = 8f; // Should be slightly slower than the player's base speed

    private Transform playerTransform;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Find the player once when the object is instantiated to save performance
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void OnEnable()
    {
        // Because we are Object Pooling, we must reset the health every time it spawns
        currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        if (playerTransform == null) return;

        // Calculate the direction directly towards the player
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        
        // Move towards the player using physics
        rb.MovePosition(transform.position + direction * moveSpeed * Time.fixedDeltaTime);

        // Rotate to look at the player, but lock the Y-axis so they don't tilt up/down
        Vector3 lookTarget = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        transform.LookAt(lookTarget);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // TODO: Play death sound, spawn ink/neon particles, and broadcast an event for the Quota Manager

        // Deactivate the game object to return it to the Object Pool
        gameObject.SetActive(false);
    }
}