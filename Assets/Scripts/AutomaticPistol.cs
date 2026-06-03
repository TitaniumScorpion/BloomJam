using UnityEngine;
using UnityEngine.InputSystem;

public class AutomaticPistol : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float fireRate = 0.1f; // Time in seconds between shots (0.1 = 10 shots per second)
    private float fireTimer;

    [Header("Overheat Mechanics")]
    public float maxHeat = 100f;
    public float heatPerShot = 12f;
    public float coolingRate = 60f;
    public float coolingDelay = 0.2f;
    private float currentHeat = 0f;
    private bool isOverheated = false;
    private float coolingTimer = 0f;

    [Header("Overheat Visuals")]
    public Renderer weaponRenderer;
    [ColorUsage(true, true)] public Color overheatGlowColor = new Color(2f, 0f, 0f, 1f); // HDR color for Bloom
    public int materialIndex = 0;
    [Tooltip("The exact Reference name in your custom Toon shader for emission/glow. Usually _EmissionColor, _Emission, or _Glow.")]
    public string emissionPropertyName = "_EmissionColor";
    [UnityEngine.Serialization.FormerlySerializedAs("visualHeatSpeed")]
    public float visualHeatingSpeed = 10f;
    public float visualCoolingSpeed = 10f;
    private Material weaponMaterial;
    private int emissionColorID;
    private float visualHeat = 0f;

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
    
    [Header("Recoil Settings")]
    public float recoilKickback = 0.3f;
    public float recoilRotation = 25f;
    public float recoilRecoverySpeed = 20f;
    private Vector3 currentRecoilPosition;
    private Vector3 currentRecoilEuler;

    [Header("Camera Shake")]
    public float camShakeMagnitude = 0.05f;
    public float camShakeDuration = 0.05f;
    private PlayerController playerController;

    [Header("Weapon Shake")]
    public float weaponShakeMagnitude = 0.015f;
    public float weaponShakeDuration = 0.05f;
    private float weaponShakeTimer;

    private Quaternion initialDisplayRotation;
    private Vector3 initialDisplayPosition;
    private float bobTimer;
    private Vector3 stableFirePointLocalPos;

    private void Start()
    {
        // Find the PlayerController so we can trigger screen shake
        playerController = GetComponentInParent<PlayerController>();

        // Automatically grab the main camera if one isn't assigned in the inspector
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Ensure the weapon is visible at start and remember its default placement
        if (displayWeapon != null)
        {
            initialDisplayRotation = displayWeapon.transform.localRotation;
            initialDisplayPosition = displayWeapon.transform.localPosition;
        }

        // Store the exact local position of the fire point relative to the camera
        // so we can calculate perfect bullet trajectories completely independent of weapon sway/shake
        if (firePoint != null && playerCamera != null)
        {
            stableFirePointLocalPos = playerCamera.transform.InverseTransformPoint(firePoint.position);
        }

        // Cache the weapon's material so we can make it glow
        if (weaponRenderer != null)
        {
            weaponMaterial = weaponRenderer.materials[materialIndex];
            weaponMaterial.EnableKeyword("_EMISSION");
            emissionColorID = Shader.PropertyToID(emissionPropertyName);
        }
    }

    private void Update()
    {
        // Don't process input or cooldowns if the game is paused (e.g., during the countdown)
        if (Time.timeScale == 0f) return;

        // Smoothly return recoil to zero
        currentRecoilPosition = Vector3.Lerp(currentRecoilPosition, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);
        currentRecoilEuler = Vector3.Lerp(currentRecoilEuler, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);

        // Manage the cooldown timer
        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;
            
        if (weaponShakeTimer > 0f)
            weaponShakeTimer -= Time.deltaTime;

        // Manage Overheat Cooling
        if (coolingTimer > 0f)
        {
            coolingTimer -= Time.deltaTime;
        }
        else if (currentHeat > 0f)
        {
            currentHeat -= coolingRate * Time.deltaTime;
            if (currentHeat <= 0f)
            {
                currentHeat = 0f;
                isOverheated = false;
            }
        }

        // Update weapon glow based on current heat
        if (weaponMaterial != null)
        {
            float currentVisualSpeed = (currentHeat > visualHeat) ? visualHeatingSpeed : visualCoolingSpeed;

            // Smoothly transition the visual heat to match the actual heat so it doesn't jump instantly when shooting
            visualHeat = Mathf.Lerp(visualHeat, currentHeat, Time.deltaTime * currentVisualSpeed);
            float heatPercent = visualHeat / maxHeat;
            
            // Square the percentage to create an exponential curve.
            // This makes the glow stay subtle at lower heat, and ramp up aggressively as it approaches max heat.
            float glowCurve = heatPercent * heatPercent;
            Color currentGlow = Color.Lerp(Color.black, overheatGlowColor, glowCurve);
            weaponMaterial.SetColor(emissionColorID, currentGlow);
        }

        // Check if the left mouse button is held down
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            if (fireTimer <= 0f && !isOverheated)
            {
                Shoot();
            }
        }

        HandleWeaponSway();
    }

    private void Shoot()
    {
        fireTimer = fireRate;
        coolingTimer = coolingDelay;

        currentHeat += heatPerShot;
        if (currentHeat >= maxHeat)
        {
            currentHeat = maxHeat;
            isOverheated = true;
        }

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
                targetPoint = ray.GetPoint(1000f); // We didn't hit anything, aim at a point very far away

            // Calculate a stable fire point that isn't affected by weapon shake, recoil, or bobbing
            Vector3 stableFirePointPos = playerCamera.transform.TransformPoint(stableFirePointLocalPos);

            // Calculate the perfect rotation needed to look from the stable point to the target point
            Vector3 direction = targetPoint - stableFirePointPos;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Grab a projectile from the Object Pool with the corrected rotation, but still spawn at the visual barrel
            ObjectPooler.Instance.SpawnFromPool(projectilePoolTag, firePoint.position, targetRotation);
        }
        
        // Trigger camera shake
        if (playerController != null)
        {
            playerController.AddCameraShake(camShakeMagnitude, camShakeDuration);
        }
        
        // Trigger weapon shake
        weaponShakeTimer = weaponShakeDuration;
        
        // Apply Recoil
        currentRecoilPosition += new Vector3(0f, 0f, -recoilKickback);
        currentRecoilEuler += new Vector3(-recoilRotation, Random.Range(-recoilRotation * 0.2f, recoilRotation * 0.2f), Random.Range(-recoilRotation * 0.2f, recoilRotation * 0.2f));
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

            // Calculate random vibration while firing
            Vector3 randomShake = Vector3.zero;
            Quaternion randomRotShake = Quaternion.identity;
            
            if (weaponShakeTimer > 0f)
            {
                randomShake = new Vector3(
                    Random.Range(-weaponShakeMagnitude, weaponShakeMagnitude),
                    Random.Range(-weaponShakeMagnitude, weaponShakeMagnitude),
                    Random.Range(-weaponShakeMagnitude, weaponShakeMagnitude)
                );
                
                float rotMag = weaponShakeMagnitude * 200f; // Scale position magnitude to degrees (0.015 -> ~3 degrees)
                randomRotShake = Quaternion.Euler(
                    Random.Range(-rotMag, rotMag), 
                    Random.Range(-rotMag, rotMag), 
                    Random.Range(-rotMag, rotMag)
                );
            }

            // Tilt
            Quaternion swayRotation = initialDisplayRotation * Quaternion.Euler(moveY * tiltAmount, 0f, -moveX * tiltAmount);
            Quaternion recoilRot = Quaternion.Euler(currentRecoilEuler);
            
            displayWeapon.transform.localRotation = Quaternion.Lerp(displayWeapon.transform.localRotation, swayRotation * recoilRot, Time.deltaTime * tiltSpeed) * randomRotShake;

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
            Vector3 targetPosition = initialDisplayPosition + new Vector3(-moveX * swayAmount, -moveY * swayAmount, 0f) + bobOffset + currentRecoilPosition;
            
            displayWeapon.transform.localPosition = Vector3.Lerp(displayWeapon.transform.localPosition, targetPosition, Time.deltaTime * swaySpeed) + randomShake;
        }
    }
}