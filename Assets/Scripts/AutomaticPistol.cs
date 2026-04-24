using UnityEngine;
using UnityEngine.InputSystem;

public class AutomaticPistol : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float fireRate = 0.1f; // Time in seconds between shots (0.1 = 10 shots per second)
    private float fireTimer;

    [Header("References")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    private void Update()
    {
        // Manage the cooldown timer
        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;

        // Check if the left mouse button is held down
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            if (fireTimer <= 0f)
            {
                Shoot();
            }
        }
    }

    private void Shoot()
    {
        fireTimer = fireRate;

        // Spawn the projectile at the firePoint's position and rotation
        if (projectilePrefab != null && firePoint != null)
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
    }
}