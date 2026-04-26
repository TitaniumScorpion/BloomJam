using UnityEngine;
using UnityEngine.InputSystem;

public class AutomaticPistol : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float fireRate = 0.1f; // Time in seconds between shots (0.1 = 10 shots per second)
    private float fireTimer;

    [Header("References")]
    public string projectilePoolTag = "PlayerProjectile";
    public string muzzleFlashPoolTag = "MuzzleFlash";
    public Transform firePoint;
    public Camera playerCamera;

    [Header("Visual Weapon Sway")]
    public GameObject displayWeapon;
    public float tiltAmount = 5f;
    public float tiltSpeed = 8f;
    public float swayAmount = 0.05f;
    public float swaySpeed = 8f;
    public float bobSpeed = 14f;
    public float bobAmount = 0.05f;

    private Quaternion initialDisplayRotation;
    private Vector3 initialDisplayPosition;
    private float bobTimer;

    private void Start()
    {
        // Automatically grab the main camera if one isn't assigned in the inspector
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Ensure the weapon is visible at start and remember its default placement
        if (displayWeapon != null)
        {
            initialDisplayRotation = displayWeapon.transform.localRotation;
            initialDisplayPosition = displayWeapon.transform.localPosition;
        }
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

        HandleWeaponSway();
    }

    private void Shoot()
    {
        fireTimer = fireRate;

        if (AudioManager.Instance != null && AudioManager.Instance.pistolShootSound != null)
        {
            float randomPitch = Random.Range(0.9f, 1.1f); // Add slight pitch variation for machine gun effect
            // Give player shooting a high priority (64) so it doesn't get culled by the audio engine
            AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.pistolShootSound, firePoint != null ? firePoint.position : transform.position, AudioManager.Instance.pistolShootVolume, randomPitch, 64);
        }

        if (firePoint != null && playerCamera != null)
        {
            // Spawn the muzzle flash and instantly parent it to the firePoint so it moves with the gun
            GameObject flash = ObjectPooler.Instance.SpawnFromPool(muzzleFlashPoolTag, firePoint.position, firePoint.rotation);
            if (flash != null) flash.transform.SetParent(firePoint);

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

    private void HandleWeaponSway()
    {
        if (displayWeapon != null && displayWeapon.activeSelf)
        {
            float moveX = 0f;
            float moveY = 0f;

            if (Keyboard.current != null)
            {
                // Read raw keyboard input for movement
                if (Keyboard.current.dKey.isPressed) moveX += 1f;
                if (Keyboard.current.aKey.isPressed) moveX -= 1f;
                if (Keyboard.current.wKey.isPressed) moveY += 1f;
                if (Keyboard.current.sKey.isPressed) moveY -= 1f;
            }

            // Tilt
            Quaternion targetRotation = initialDisplayRotation * Quaternion.Euler(moveY * tiltAmount, 0f, -moveX * tiltAmount);
            displayWeapon.transform.localRotation = Quaternion.Lerp(displayWeapon.transform.localRotation, targetRotation, Time.deltaTime * tiltSpeed);

            // Calculate continuous bobbing (Figure-8 pattern)
            float speedMagnitude = Mathf.Clamp01(Mathf.Abs(moveX) + Mathf.Abs(moveY));
            if (speedMagnitude > 0.1f)
            {
                bobTimer += Time.deltaTime * bobSpeed;
            }

            Vector3 bobOffset = new Vector3(
                Mathf.Cos(bobTimer * 0.5f) * (bobAmount * 0.5f), 
                Mathf.Sin(bobTimer) * bobAmount, 
                0f
            ) * speedMagnitude;

            // Positional sway
            Vector3 targetPosition = initialDisplayPosition + new Vector3(-moveX * swayAmount, -moveY * swayAmount, 0f) + bobOffset;
            displayWeapon.transform.localPosition = Vector3.Lerp(displayWeapon.transform.localPosition, targetPosition, Time.deltaTime * swaySpeed);
        }
    }
}