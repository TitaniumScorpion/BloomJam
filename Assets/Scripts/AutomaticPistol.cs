using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class AutomaticPistol : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float fireRate = 0.1f;
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
    [ColorUsage(true, true)] public Color overheatGlowColor = new Color(2f, 0f, 0f, 1f);
    public int materialIndex = 0;
    [Tooltip("The exact Reference name in your custom Toon shader for emission/glow.")]
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
    [Tooltip("Muzzle flash was disabled during testing. Tick to re-enable — needs a 'MuzzleFlash' pool on the ObjectPooler.")]
    public bool muzzleFlashEnabled = false;
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

    // ── Charge Attack (unlocked at Lv2) ──────────────────────────────────────
    [Header("Charge Attack (Lv2 Unlock)")]
    public bool chargeUnlocked = false;
    [Tooltip("Seconds after releasing LMB in which a second hold triggers charge mode")]
    public float chargeActivationWindow = 0.5f;
    public float maxChargeTime = 2.5f;
    public int chargeMinDamage = 3;
    public int chargeMaxDamage = 20;
    public float beamMinWidth = 0.02f;
    public float beamMaxWidth = 0.15f;
    public Material beamMaterial;
    private LineRenderer beamRenderer;
    private float chargeTime = 0f;
    private float chargeWindowTimer = 0f;

    private enum FireState { Idle, NormalFire, WaitForCharge, Charging }
    private FireState fireState = FireState.Idle;

    // ── Auto Shotgun (unlocked at Lv4) ───────────────────────────────────────
    [Header("Auto Shotgun (Lv4 Unlock)")]
    public bool shotgunUnlocked = false;
    public int shotgunPellets = 4;
    public float shotgunRingAngle = 8f; // degrees each bullet is offset from center
    public float shotgunSpawnSideOffset = 0.6f; // horizontal offset from fire point
    private float continuousFireTimer = 0f;
    private float currentShotgunThreshold = 3f;
    private const float shotgunThresholdMin = 2f;

    private Quaternion initialDisplayRotation;
    private Vector3 initialDisplayPosition;
    private float bobTimer;
    private Vector3 stableFirePointLocalPos;

    private void OnEnable()
    {
        KatanaWeapon.OnBulletTimeStart += HideWeapon;
        KatanaWeapon.OnBulletTimeEnd += ShowWeapon;
        ElevatorHub.OnHubModeEnter += HideWeapon;
        ElevatorHub.OnHubModeExit += ShowWeapon;
    }

    private void OnDisable()
    {
        KatanaWeapon.OnBulletTimeStart -= HideWeapon;
        KatanaWeapon.OnBulletTimeEnd -= ShowWeapon;
        ElevatorHub.OnHubModeEnter -= HideWeapon;
        ElevatorHub.OnHubModeExit -= ShowWeapon;
    }

    private void HideWeapon() { if (displayWeapon != null) displayWeapon.SetActive(false); }
    private void ShowWeapon() { if (displayWeapon != null) displayWeapon.SetActive(true); }

    private void Start()
    {
        playerController = GetComponentInParent<PlayerController>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (displayWeapon != null)
        {
            initialDisplayRotation = displayWeapon.transform.localRotation;
            initialDisplayPosition = displayWeapon.transform.localPosition;
        }

        if (firePoint != null && playerCamera != null)
            stableFirePointLocalPos = playerCamera.transform.InverseTransformPoint(firePoint.position);

        if (weaponRenderer != null)
        {
            weaponMaterial = weaponRenderer.materials[materialIndex];
            weaponMaterial.EnableKeyword("_EMISSION");
            emissionColorID = Shader.PropertyToID(emissionPropertyName);
        }

        // Stay hidden until the player actually presses Start — ElevatorHub.OnHubModeExit
        // (fired from GameManager.StartCurrentZone) shows it again from there on
        if (!GameManager.HasGameStarted) HideWeapon();
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (!GameManager.HasGameStarted) return;
        if (KatanaWeapon.IsBulletTimeActive) return;
        if (ElevatorHub.IsActive) return;

        currentRecoilPosition = Vector3.Lerp(currentRecoilPosition, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);
        currentRecoilEuler = Vector3.Lerp(currentRecoilEuler, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);

        if (fireTimer > 0f) fireTimer -= Time.deltaTime;
        if (weaponShakeTimer > 0f) weaponShakeTimer -= Time.deltaTime;

        HandleOverheatCooling();
        UpdateWeaponGlow();

        HandleFiring();

        if (shotgunUnlocked)
            HandleShotgunMechanic(fireState == FireState.NormalFire);

        HandleWeaponSway();
    }

    // ── Overheat & Glow ──────────────────────────────────────────────────────

    private void HandleOverheatCooling()
    {
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
    }

    private void UpdateWeaponGlow()
    {
        if (weaponMaterial == null) return;

        float currentVisualSpeed = (currentHeat > visualHeat) ? visualHeatingSpeed : visualCoolingSpeed;
        visualHeat = Mathf.Lerp(visualHeat, currentHeat, Time.deltaTime * currentVisualSpeed);
        float heatPercent = visualHeat / maxHeat;
        float glowCurve = heatPercent * heatPercent;
        weaponMaterial.SetColor(emissionColorID, Color.Lerp(Color.black, overheatGlowColor, glowCurve));
    }

    // ── Normal Shot ───────────────────────────────────────────────────────────

    private void Shoot()
    {
        fireTimer = fireRate;
        coolingTimer = coolingDelay;

        currentHeat += heatPerShot;
        if (currentHeat >= maxHeat) { currentHeat = maxHeat; isOverheated = true; }

        if (AudioManager.Instance != null && AudioManager.Instance.pistolShootSound != null)
        {
            float randomPitch = Random.Range(0.9f, 1.1f);
            AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.pistolShootSound,
                firePoint != null ? firePoint.position : transform.position,
                AudioManager.Instance.pistolShootVolume, randomPitch, 64);
        }

        if (firePoint != null && playerCamera != null)
        {
            if (muzzleFlashEnabled)
            {
                GameObject flash = ObjectPooler.Instance.SpawnFromPool(muzzleFlashPoolTag, firePoint.position, firePoint.rotation);
                if (flash != null) flash.transform.SetParent(firePoint);
            }

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit) ? hit.point : ray.GetPoint(1000f);

            Vector3 stableFirePointPos = playerCamera.transform.TransformPoint(stableFirePointLocalPos);
            Vector3 direction = targetPoint - stableFirePointPos;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            ObjectPooler.Instance.SpawnFromPool(projectilePoolTag, stableFirePointPos, targetRotation);
        }

        playerController?.AddCameraShake(camShakeMagnitude, camShakeDuration);
        weaponShakeTimer = weaponShakeDuration;
        currentRecoilPosition += new Vector3(0f, 0f, -recoilKickback);
        currentRecoilEuler += new Vector3(-recoilRotation,
            Random.Range(-recoilRotation * 0.2f, recoilRotation * 0.2f),
            Random.Range(-recoilRotation * 0.2f, recoilRotation * 0.2f));
    }

    // ── Firing State Machine ──────────────────────────────────────────────────

    private void HandleFiring()
    {
        if (Mouse.current == null) return;

        bool lmbDown = Mouse.current.leftButton.wasPressedThisFrame;
        bool lmbUp   = Mouse.current.leftButton.wasReleasedThisFrame;

        switch (fireState)
        {
            case FireState.Idle:
                if (lmbDown)
                    fireState = FireState.NormalFire;
                break;

            case FireState.NormalFire:
                if (fireTimer <= 0f && !isOverheated)
                    Shoot();

                if (lmbUp)
                {
                    if (chargeUnlocked)
                    {
                        fireState = FireState.WaitForCharge;
                        chargeWindowTimer = chargeActivationWindow;
                    }
                    else
                    {
                        fireState = FireState.Idle;
                    }
                }
                break;

            case FireState.WaitForCharge:
                chargeWindowTimer -= Time.deltaTime;
                if (chargeWindowTimer <= 0f)
                {
                    fireState = FireState.Idle;
                    break;
                }
                if (lmbDown)
                {
                    fireState = FireState.Charging;
                    chargeTime = 0f;
                    if (beamRenderer != null) beamRenderer.enabled = true;
                }
                break;

            case FireState.Charging:
                if (isOverheated)
                {
                    CancelCharge();
                    break;
                }
                chargeTime = Mathf.Min(chargeTime + Time.deltaTime, maxChargeTime);
                UpdateBeamVisual();

                if (lmbUp)
                {
                    FireChargedBeam();
                    CancelCharge();
                }
                break;
        }
    }

    private void UpdateBeamVisual()
    {
        if (beamRenderer == null || playerCamera == null || firePoint == null) return;

        float t = chargeTime / maxChargeTime;
        float width = Mathf.Lerp(beamMinWidth, beamMaxWidth, t);
        beamRenderer.startWidth = width;
        beamRenderer.endWidth = width * 0.3f;

        beamRenderer.SetPosition(0, firePoint.position);

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 endPoint = Physics.Raycast(ray, out RaycastHit hit, 500f) ? hit.point : ray.GetPoint(500f);
        beamRenderer.SetPosition(1, endPoint);
    }

    private void FireChargedBeam()
    {
        float chargePercent = chargeTime / maxChargeTime;
        int damage = Mathf.RoundToInt(Mathf.Lerp(chargeMinDamage, chargeMaxDamage, chargePercent));

        // Full charge adds extra heat
        currentHeat += heatPerShot * (1f + chargePercent * 3f);
        if (currentHeat >= maxHeat) { currentHeat = maxHeat; isOverheated = true; }
        coolingTimer = coolingDelay;

        if (AudioManager.Instance != null && AudioManager.Instance.pistolShootSound != null)
        {
            AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.pistolShootSound,
                firePoint != null ? firePoint.position : transform.position,
                AudioManager.Instance.pistolShootVolume * 2f, 0.6f + chargePercent * 0.3f, 64);
        }

        if (playerCamera != null)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.TryGetComponent(out IDamageable damageable))
                    damageable.TakeDamage(damage);
                else if (!hit.collider.isTrigger && hit.rigidbody == null)
                    break; // Stop piercing at solid environment
            }
        }

        playerController?.AddCameraShake(camShakeMagnitude * 4f, camShakeDuration * 3f);
        StartCoroutine(BeamFlashRoutine());
    }

    private IEnumerator BeamFlashRoutine()
    {
        yield return new WaitForSeconds(0.12f);
        if (beamRenderer != null) beamRenderer.enabled = false;
    }

    private void CancelCharge()
    {
        chargeTime = 0f;
        fireState = FireState.Idle;
        if (beamRenderer != null) beamRenderer.enabled = false;
    }

    // ── Auto Shotgun ──────────────────────────────────────────────────────────

    private void HandleShotgunMechanic(bool isActivelyShooting)
    {
        if (isActivelyShooting && !isOverheated)
        {
            continuousFireTimer += Time.deltaTime;

            if (continuousFireTimer >= currentShotgunThreshold)
            {
                FireShotgunBlast();
                continuousFireTimer = 0f;
                currentShotgunThreshold = Mathf.Max(currentShotgunThreshold - 1f, shotgunThresholdMin);
            }
        }
        else
        {
            continuousFireTimer = 0f;
            currentShotgunThreshold = 3f; // Reset threshold when player stops firing
        }
    }

    private void FireShotgunBlast()
    {
        if (playerCamera == null) return;

        currentHeat += heatPerShot * shotgunPellets * 0.4f;
        if (currentHeat >= maxHeat) { currentHeat = maxHeat; isOverheated = true; }
        coolingTimer = coolingDelay;

        playerController?.AddCameraShake(camShakeMagnitude * 5f, camShakeDuration * 2f);

        if (AudioManager.Instance != null && AudioManager.Instance.pistolShootSound != null)
        {
            AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.pistolShootSound,
                firePoint != null ? firePoint.position : transform.position,
                AudioManager.Instance.pistolShootVolume * 3f, 0.5f, 64);
        }

        Vector3 basePos = firePoint != null ? firePoint.position : transform.position;
        Vector3 spawnPos = basePos + playerCamera.transform.right * shotgunSpawnSideOffset;
        for (int i = 0; i < shotgunPellets; i++)
        {
            float angle = (360f / shotgunPellets) * i * Mathf.Deg2Rad;
            float offsetX = Mathf.Sin(angle) * shotgunRingAngle;
            float offsetY = Mathf.Cos(angle) * shotgunRingAngle;
            Quaternion ringRot = playerCamera.transform.rotation * Quaternion.Euler(offsetX, offsetY, 0f);
            ObjectPooler.Instance.SpawnFromPool(projectilePoolTag, spawnPos, ringRot);
        }
    }

    // ── Public Unlock Methods (called by UpgradeManager) ─────────────────────

    public void UnlockChargeAttack()
    {
        chargeUnlocked = true;
        if (beamRenderer == null)
        {
            beamRenderer = gameObject.AddComponent<LineRenderer>();
            beamRenderer.positionCount = 2;
            beamRenderer.startWidth = beamMinWidth;
            beamRenderer.endWidth = beamMinWidth;
            beamRenderer.useWorldSpace = true;
            beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beamRenderer.receiveShadows = false;
            beamRenderer.material = beamMaterial != null ? beamMaterial : new Material(Shader.Find("Sprites/Default"));
            beamRenderer.enabled = false;
        }
    }

    public void LockChargeAttack()
    {
        chargeUnlocked = false;
        CancelCharge(); // resets fireState to Idle
    }

    public void UnlockShotgun()
    {
        shotgunUnlocked = true;
        currentShotgunThreshold = 4f;
    }

    public void LockShotgun()
    {
        shotgunUnlocked = false;
    }

    // ── Weapon Sway ───────────────────────────────────────────────────────────

    private void HandleWeaponSway()
    {
        if (displayWeapon == null || !displayWeapon.activeSelf) return;

        float moveX = 0f;
        float moveY = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed) moveX += 1f;
            if (Keyboard.current.aKey.isPressed) moveX -= 1f;
            if (Keyboard.current.wKey.isPressed) moveY += 1f;
            if (Keyboard.current.sKey.isPressed) moveY -= 1f;
        }

        Vector3 randomShake = Vector3.zero;
        Quaternion randomRotShake = Quaternion.identity;

        if (weaponShakeTimer > 0f)
        {
            randomShake = new Vector3(
                Random.Range(-weaponShakeMagnitude, weaponShakeMagnitude),
                Random.Range(-weaponShakeMagnitude, weaponShakeMagnitude),
                Random.Range(-weaponShakeMagnitude, weaponShakeMagnitude));

            float rotMag = weaponShakeMagnitude * 200f;
            randomRotShake = Quaternion.Euler(
                Random.Range(-rotMag, rotMag),
                Random.Range(-rotMag, rotMag),
                Random.Range(-rotMag, rotMag));
        }

        Quaternion swayRotation = initialDisplayRotation * Quaternion.Euler(moveY * tiltAmount, 0f, -moveX * tiltAmount);
        Quaternion recoilRot = Quaternion.Euler(currentRecoilEuler);
        displayWeapon.transform.localRotation = Quaternion.Lerp(displayWeapon.transform.localRotation,
            swayRotation * recoilRot, Time.deltaTime * tiltSpeed) * randomRotShake;

        float speedMagnitude = Mathf.Clamp01(Mathf.Abs(moveX) + Mathf.Abs(moveY));
        if (speedMagnitude > 0.1f) bobTimer += Time.deltaTime * bobSpeed;

        Vector3 bobOffset = new Vector3(
            Mathf.Cos(bobTimer * 0.5f) * (bobAmount * 0.5f),
            Mathf.Sin(bobTimer) * bobAmount,
            0f) * speedMagnitude;

        Vector3 targetPosition = initialDisplayPosition
            + new Vector3(-moveX * swayAmount, -moveY * swayAmount, 0f)
            + bobOffset
            + currentRecoilPosition;

        displayWeapon.transform.localPosition =
            Vector3.Lerp(displayWeapon.transform.localPosition, targetPosition, Time.deltaTime * swaySpeed)
            + randomShake;
    }
}
