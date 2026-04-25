using UnityEngine;
using UnityEngine.InputSystem;

public class AutomaticPistol : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float fireRate = 0.1f; // Time in seconds between shots (0.1 = 10 shots per second)
    private float fireTimer;

    [Header("References")]
    public string projectilePoolTag = "PlayerProjectile";
    public Transform firePoint;
    public Camera playerCamera;

    private void Start()
    {
        // Automatically grab the main camera if one isn't assigned in the inspector
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        // Don't process input or cooldowns if the game is paused (e.g., during the countdown)
        if (Time.timeScale == 0f) return;

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

        if (firePoint != null && playerCamera != null)
        {
            // Raycast from the center of the screen to find exactly what the player is aiming at
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 targetPoint;

            if (Physics.Raycast(ray, out RaycastHit hit))
                targetPoint = hit.point; // We hit something, aim at the hit point
            else
                targetPoint = ray.GetPoint(100f); // We didn't hit anything, aim at a point far away

            // Calculate the rotation needed to look from the gun's barrel to the target point
            Vector3 direction = targetPoint - firePoint.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Grab a projectile from the Object Pool with the corrected rotation
            ObjectPooler.Instance.SpawnFromPool(projectilePoolTag, firePoint.position, targetRotation);
        }
    }
}