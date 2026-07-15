using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class KatanaWeapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float attackRange = 3.5f;
    public float attackRadius = 2f;
    public int damage = 5;
    public float cooldownTime = 0.8f;
    private float cooldownTimer;

    [Header("References")]
    public GameObject displayWeapon;
    public Transform cameraTransform;

    [Header("Tilt Settings")]
    public float tiltAmount = 5f;

    [Header("Sway Settings")]
    public float swayAmount = 0.05f;
    public float swaySpeed = 8f;
    private Vector3 initialDisplayPosition;

    [Header("Bob Settings")]
    public float bobSpeed = 14f;
    public float bobAmount = 0.05f;
    private float bobTimer;

    [Header("Swing Animation")]
    public float swingSpeed = 15f;
    public float swingDuration = 0.35f;
    public Vector3 swingRotationOffset = new Vector3(10f, 100f, -40f);
    private Quaternion initialDisplayRotation;
    private Quaternion targetSwingRotation;

    // ── Sword Visuals (per upgrade level) ────────────────────────────────────
    [Header("Sword Visuals")]
    [Tooltip("One entry per upgrade level: [0]=base, [1]=Lv2, [2]=Lv3, [3]=Lv4")]
    public GameObject[] swordLevelVisuals;

    // ── Sword Waves (unlocked at Lv2) ─────────────────────────────────────────
    [Header("Sword Waves (Lv2 Unlock)")]
    public bool wavesUnlocked = false;
    public string wavePoolTag = "SwordWave";

    // ── Bullet Time (unlocked at Lv4) ─────────────────────────────────────────
    [Header("Bullet Time (Lv4 Unlock)")]
    public bool bulletTimeUnlocked = false;
    public float bulletTimeDuration = 5f;
    public float bulletTimeCooldown = 15f;
    public int markedDamagePerHit = 5;
    public Material markedMaterial;

    public static bool IsBulletTimeActive = false;

    private bool isBulletTimeRunning = false;
    private float bulletTimeTimer = 0f;
    private float bulletTimeCooldownTimer = 0f;
    private Dictionary<StandardSwarmer, int> swarmerHitCounts = new Dictionary<StandardSwarmer, int>();

    private GameObject bulletTimeBarObj;
    private RectTransform bulletTimeBarFill;
    private Image bulletTimeBarFillImage;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (displayWeapon != null)
        {
            initialDisplayRotation = displayWeapon.transform.localRotation;
            initialDisplayPosition = displayWeapon.transform.localPosition;
            targetSwingRotation = initialDisplayRotation;
        }

        SetSwordVisual(0);
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && cooldownTimer <= 0f)
            Attack();

        if (bulletTimeUnlocked)
            HandleBulletTime();

        HandleWeaponSwayAndSwing();
    }

    // ── Attack ────────────────────────────────────────────────────────────────

    private void Attack()
    {
        cooldownTimer = cooldownTime;

        if (displayWeapon != null)
            StartCoroutine(SwingRoutine());

        if (cameraTransform == null) return;

        if (IsBulletTimeActive)
        {
            // Raycast from crosshair — no range cap, marks whatever enemy the player aims at
            Ray aimRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(aimRay, out RaycastHit aimHit, Mathf.Infinity))
            {
                if (aimHit.collider.TryGetComponent(out StandardSwarmer targeted))
                {
                    if (!swarmerHitCounts.ContainsKey(targeted))
                    {
                        swarmerHitCounts[targeted] = 0;
                        if (markedMaterial != null) targeted.Mark(markedMaterial);
                    }
                    swarmerHitCounts[targeted]++;
                }
                else if (aimHit.collider.TryGetComponent(out EnemyWeakPoint weakPoint))
                {
                    weakPoint.TakeDamage(damage);
                }
                else if (aimHit.collider.TryGetComponent(out DroneWeakPoint dronePoint))
                {
                    dronePoint.TakeDamage(damage);
                }
            }
        }
        else
        {
            // Normal melee: OverlapCapsule for reliable close-range detection
            Vector3 startPoint = cameraTransform.position;
            Vector3 endPoint = cameraTransform.position + cameraTransform.forward * attackRange;
            Collider[] hitColliders = Physics.OverlapCapsule(startPoint, endPoint, attackRadius);

            foreach (Collider col in hitColliders)
            {
                if (col.TryGetComponent(out StandardSwarmer swarmer))
                    swarmer.TakeDamage(damage);
                else if (col.TryGetComponent(out EnemyWeakPoint weakPoint))
                    weakPoint.TakeDamage(damage);
                else if (col.TryGetComponent(out DroneWeakPoint dronePoint))
                    dronePoint.TakeDamage(damage);
            }
        }

        // Spawn wave projectile (disabled during bullet time per design)
        if (wavesUnlocked && !IsBulletTimeActive && cameraTransform != null)
        {
            Vector3 wavePos = cameraTransform.position + cameraTransform.forward * 0.5f;
            ObjectPooler.Instance.SpawnFromPool(wavePoolTag, wavePos, cameraTransform.rotation);
        }
    }

    private IEnumerator SwingRoutine()
    {
        targetSwingRotation = initialDisplayRotation * Quaternion.Euler(swingRotationOffset);
        yield return new WaitForSeconds(swingDuration);
        targetSwingRotation = initialDisplayRotation;
    }

    // ── Bullet Time ───────────────────────────────────────────────────────────

    private void HandleBulletTime()
    {
        // Cooldown counts down in real time so it isn't affected by bullet time itself
        if (bulletTimeCooldownTimer > 0f)
            bulletTimeCooldownTimer -= Time.unscaledDeltaTime;

        // Activate
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
            && !isBulletTimeRunning && bulletTimeCooldownTimer <= 0f)
        {
            StartBulletTime();
        }

        // Duration countdown
        if (isBulletTimeRunning)
        {
            bulletTimeTimer -= Time.unscaledDeltaTime;
            UpdateBulletTimeBar(bulletTimeTimer / bulletTimeDuration);

            if (bulletTimeTimer <= 0f)
                EndBulletTime();
        }
        else if (bulletTimeCooldownTimer > 0f)
        {
            // Show refill progress while on cooldown
            UpdateBulletTimeBar(1f - (bulletTimeCooldownTimer / bulletTimeCooldown));
        }
        else
        {
            UpdateBulletTimeBar(1f); // Full = ready
        }
    }

    private void StartBulletTime()
    {
        isBulletTimeRunning = true;
        IsBulletTimeActive = true;
        bulletTimeTimer = bulletTimeDuration;
        swarmerHitCounts.Clear();

        if (bulletTimeBarObj != null) bulletTimeBarObj.SetActive(true);
    }

    private void EndBulletTime()
    {
        isBulletTimeRunning = false;
        IsBulletTimeActive = false;
        bulletTimeCooldownTimer = bulletTimeCooldown;

        // Deal accumulated damage to all marked enemies at once
        foreach (var kvp in swarmerHitCounts)
        {
            StandardSwarmer swarmer = kvp.Key;
            if (swarmer != null && swarmer.gameObject.activeInHierarchy)
            {
                swarmer.Unmark();
                swarmer.TakeDamage(markedDamagePerHit * kvp.Value);
            }
        }
        swarmerHitCounts.Clear();
    }

    // ── Bullet Time Bar (created programmatically) ────────────────────────────

    public void CreateBulletTimeBar()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        bulletTimeBarObj = new GameObject("BulletTimeBar");
        bulletTimeBarObj.transform.SetParent(canvas.transform, false);

        RectTransform containerRT = bulletTimeBarObj.AddComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.5f, 0.5f);
        containerRT.anchorMax = new Vector2(0.5f, 0.5f);
        containerRT.pivot = new Vector2(0.5f, 0.5f);
        containerRT.anchoredPosition = new Vector2(0f, -50f); // Below crosshair
        containerRT.sizeDelta = new Vector2(150f, 6f);

        // Background
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(bulletTimeBarObj.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(bulletTimeBarObj.transform, false);
        bulletTimeBarFillImage = fill.AddComponent<Image>();
        bulletTimeBarFillImage.color = new Color(0.2f, 0.85f, 1f, 1f);
        bulletTimeBarFill = fill.GetComponent<RectTransform>();
        bulletTimeBarFill.anchorMin = Vector2.zero;
        bulletTimeBarFill.anchorMax = Vector2.one;
        bulletTimeBarFill.offsetMin = bulletTimeBarFill.offsetMax = Vector2.zero;

        bulletTimeBarObj.SetActive(false);
    }

    private void UpdateBulletTimeBar(float fillAmount)
    {
        if (bulletTimeBarFill == null) return;

        fillAmount = Mathf.Clamp01(fillAmount);
        bulletTimeBarFill.anchorMax = new Vector2(fillAmount, 1f);

        if (bulletTimeBarFillImage != null)
        {
            // Cyan when active, orange when on cooldown, green when full/ready
            if (isBulletTimeRunning)
                bulletTimeBarFillImage.color = new Color(0.2f, 0.85f, 1f, 1f);
            else if (bulletTimeCooldownTimer > 0f)
                bulletTimeBarFillImage.color = new Color(1f, 0.55f, 0.1f, 1f);
            else
                bulletTimeBarFillImage.color = new Color(0.3f, 1f, 0.4f, 1f);
        }
    }

    // ── Public Unlock Methods (called by UpgradeManager) ─────────────────────

    public void SetSwordVisual(int level)
    {
        if (swordLevelVisuals == null || swordLevelVisuals.Length == 0) return;
        int clamped = Mathf.Clamp(level, 0, swordLevelVisuals.Length - 1);
        for (int i = 0; i < swordLevelVisuals.Length; i++)
            if (swordLevelVisuals[i] != null)
                swordLevelVisuals[i].SetActive(i <= clamped);
    }

    public void UnlockWaves() => wavesUnlocked = true;

    public void LockWaves() => wavesUnlocked = false;

    public void UnlockBulletTime()
    {
        bulletTimeUnlocked = true;
        if (bulletTimeBarObj == null)
            CreateBulletTimeBar();
        else
            bulletTimeBarObj.SetActive(true);
    }

    public void LockBulletTime()
    {
        if (isBulletTimeRunning) EndBulletTime();
        bulletTimeUnlocked = false;
        if (bulletTimeBarObj != null) bulletTimeBarObj.SetActive(false);
    }

    // ── Weapon Sway ───────────────────────────────────────────────────────────

    private void HandleWeaponSwayAndSwing()
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

        float speedMagnitude = Mathf.Clamp01(Mathf.Abs(moveX) + Mathf.Abs(moveY));
        if (speedMagnitude > 0.1f) bobTimer += Time.deltaTime * bobSpeed;

        Vector3 bobOffset = new Vector3(
            Mathf.Cos(bobTimer * 0.5f) * (bobAmount * 0.5f),
            Mathf.Sin(bobTimer) * bobAmount, 0f) * speedMagnitude;

        Vector3 targetPosition = initialDisplayPosition
            + new Vector3(-moveX * swayAmount, -moveY * swayAmount, 0f)
            + bobOffset;
        displayWeapon.transform.localPosition =
            Vector3.Lerp(displayWeapon.transform.localPosition, targetPosition, Time.deltaTime * swaySpeed);

        Quaternion tiltRotation = Quaternion.Euler(moveY * tiltAmount, 0f, -moveX * tiltAmount);
        displayWeapon.transform.localRotation =
            Quaternion.Lerp(displayWeapon.transform.localRotation,
                targetSwingRotation * tiltRotation, Time.deltaTime * swingSpeed);
    }
}
