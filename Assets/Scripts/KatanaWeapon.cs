using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class KatanaWeapon : HandheldWeapon
{
    [Header("Weapon Settings")]
    public float attackRange = 3.5f;
    public float attackRadius = 2f;
    public int damage = 5;
    public float cooldownTime = 0.8f;
    private float cooldownTimer;

    [Header("References")]
    public Transform cameraTransform;

    [Header("Swing Animation (procedural fallback)")]
    [Tooltip("How fast the view-model chases its rotation target.")]
    public float swingSpeed = 15f;
    public float swingDuration = 0.35f;
    [Tooltip("Used only by combo steps that leave their own procedural offset at zero.")]
    public Vector3 swingRotationOffset = new Vector3(10f, 100f, -40f);
    // Rest rotation the sway swings around — offset mid-swing, back to initial otherwise
    private Quaternion targetSwingRotation;

    // ── Attack Combo ─────────────────────────────────────────────────
    /// <summary>
    /// One step of the melee combo. The array of these IS the combo: add or remove
    /// entries and the cycle length follows.
    /// </summary>
    [Serializable]
    public class SwordSwing
    {
        [Tooltip("Label only — makes the array readable in the Inspector.")]
        public string name = "Swing";

        [Tooltip("Animator state to play for this step. Must match a state name in the sword's Animator Controller.")]
        public string animationStateName = "";

        [Tooltip("Seconds between the button press and the damage landing, so the hit matches the contact frame of the clip. 0 = instant (current behaviour). Ignored when Use Animation Event For Hit is on.")]
        public float hitDelay = 0f;

        [Tooltip("Attack cooldown for this step. 0 or less = use the weapon's Cooldown Time. Never applies during bullet time.")]
        public float cooldownOverride = 0f;

        [Tooltip("Scales this weapon's Damage for this step — give the finisher a bonus here.")]
        public float damageMultiplier = 1f;

        [Tooltip("Procedural fallback pose, used only while Sword Animator is empty. Zero = fall back to the weapon's Swing Rotation Offset.")]
        public Vector3 proceduralRotationOffset = Vector3.zero;
    }

    [Header("Attack Combo")]
    [Tooltip("Optional. Leave empty and the old procedural swing plays instead. Put this on a CHILD of the view-model — the sway drives the parent, the clips drive the child.")]
    public Animator swordAnimator;

    [Tooltip("State cross-faded back to when a swing reports it is finished. Leave empty if your controller uses Exit Time transitions instead.")]
    public string idleStateName = "Idle";

    [Tooltip("Cross-fade length into a swing state, in seconds.")]
    public float animationBlendTime = 0.05f;

    [Tooltip("Seconds of no attack after which the combo drops back to step 1.")]
    public float comboResetTime = 1f;

    [Tooltip("Let an Animation Event on the clip pick the hit frame instead of Hit Delay. Needs a SwordAnimationEvents component on the animated object, calling SwordHit().")]
    public bool useAnimationEventForHit = false;

    public SwordSwing[] comboSwings =
    {
        new SwordSwing { name = "1 — Left Swing",  animationStateName = "Swing_Left",  proceduralRotationOffset = new Vector3(85f,  20f, -20f) },
        new SwordSwing { name = "2 — Right Swing", animationStateName = "Swing_Right", proceduralRotationOffset = new Vector3(85f, -35f,  25f) },
        new SwordSwing { name = "3 — Cross Swing", animationStateName = "Swing_Cross", proceduralRotationOffset = new Vector3(110f,  0f,   0f) },
    };

    /// <summary>Which combo step the last attack played — for VFX/audio that want to know.</summary>
    public int ComboIndex => comboIndex;

    private int comboIndex = -1;
    private float lastAttackTime = float.NegativeInfinity;
    private SwordSwing pendingSwing;
    private bool swingHitResolved = true;
    private Coroutine pendingHitRoutine;

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
    public Material markedMaterial;

    [Header("Bullet Time Waves")]
    [Tooltip("Pool tag for the faster/larger sword wave spawned during bullet time")]
    public string bulletTimeWavePoolTag = "BulletTimeSwordWave";
    [Tooltip("Attack cooldown while bullet time is active")]
    public float bulletTimeAttackCooldown = 0.15f;

    public static bool IsBulletTimeActive = false;
    public static Material BulletTimeMarkMaterial { get; private set; }
    public static event Action OnBulletTimeStart;
    public static event Action OnBulletTimeEnd;

    private static readonly List<GameObject> pendingBulletTimeDeaths = new List<GameObject>();

    private bool isBulletTimeRunning = false;
    private float bulletTimeTimer = 0f;
    private float bulletTimeCooldownTimer = 0f;

    private GameObject bulletTimeBarObj;
    private RectTransform bulletTimeBarFill;
    private Image bulletTimeBarFillImage;

    protected override void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        targetSwingRotation = initialDisplayRotation; // cached by the base in Awake

        SetSwordVisual(0);
        BulletTimeMarkMaterial = markedMaterial;

        base.Start(); // applies the pre-game hide — must run last
    }

    private void Update()
    {
        if (!CanAct()) return;

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
        SwordSwing swing = AdvanceCombo();

        cooldownTimer = IsBulletTimeActive
            ? bulletTimeAttackCooldown
            : (swing != null && swing.cooldownOverride > 0f ? swing.cooldownOverride : cooldownTime);

        PlaySwingAnimation(swing);

        if (cameraTransform == null) return;

        // Arm the hit. A new swing always cancels the previous one's pending hit, so a
        // short cooldown can never let one press land two hits.
        if (pendingHitRoutine != null) StopCoroutine(pendingHitRoutine);
        pendingHitRoutine = null;
        pendingSwing = swing;
        swingHitResolved = false;

        // Animation-event mode hands the timing to the clip — OnSwingHit() resolves it.
        if (useAnimationEventForHit && swordAnimator != null) return;

        float delay = swing != null ? swing.hitDelay : 0f;
        if (delay <= 0f) ResolveSwingHit();
        else pendingHitRoutine = StartCoroutine(DelayedHitRoutine(delay));
    }

    /// <summary>
    /// Steps the combo forward, or restarts it if the player let the window lapse.
    /// Returns null when no combo is configured, which keeps the old single-swing path alive.
    /// </summary>
    private SwordSwing AdvanceCombo()
    {
        if (comboSwings == null || comboSwings.Length == 0)
        {
            lastAttackTime = Time.time;
            return null;
        }

        // Measured from the last press, per design: quiet for comboResetTime, back to step 1.
        // Time.time is safe here — bullet time never touches timeScale.
        if (Time.time - lastAttackTime > comboResetTime) comboIndex = 0;
        else comboIndex = (comboIndex + 1) % comboSwings.Length;

        lastAttackTime = Time.time;
        return comboSwings[comboIndex];
    }

    private void PlaySwingAnimation(SwordSwing swing)
    {
        if (swordAnimator != null && swordAnimator.isActiveAndEnabled
            && swing != null && !string.IsNullOrEmpty(swing.animationStateName))
        {
            // Fixed-time cross-fade restarted from 0, so re-triggering the same state
            // replays it instead of being swallowed as "already playing".
            swordAnimator.CrossFadeInFixedTime(swing.animationStateName, animationBlendTime, 0, 0f);
            return;
        }

        // No animator (or no clip named for this step) — the original procedural swing.
        if (displayWeapon != null) StartCoroutine(SwingRoutine(swing));
    }

    private IEnumerator DelayedHitRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        pendingHitRoutine = null;
        ResolveSwingHit();
    }

    /// <summary>Called from an Animation Event via <see cref="SwordAnimationEvents"/>.</summary>
    public void OnSwingHit() => ResolveSwingHit();

    /// <summary>
    /// Called from an Animation Event via <see cref="SwordAnimationEvents"/>. Optional —
    /// only needed if the controller has no Exit Time transition back to idle.
    /// </summary>
    public void OnSwingEnd()
    {
        if (swordAnimator == null || !swordAnimator.isActiveAndEnabled) return;
        if (string.IsNullOrEmpty(idleStateName)) return;
        swordAnimator.CrossFadeInFixedTime(idleStateName, animationBlendTime, 0, 0f);
    }

    /// <summary>The damage/wave application, run once per swing whatever triggered it.</summary>
    private void ResolveSwingHit()
    {
        if (swingHitResolved) return;
        swingHitResolved = true;

        // A delayed hit can outlive the player's control — e.g. the zone ends and the hub
        // opens between the press and the contact frame. Drop it rather than land it.
        if (!CanAct()) return;
        if (cameraTransform == null) return;

        if (IsBulletTimeActive)
        {
            // Fire a fast, large bullet-time wave forward
            Vector3 btWavePos = cameraTransform.position + cameraTransform.forward * 0.5f;
            ObjectPooler.Instance.SpawnFromPool(bulletTimeWavePoolTag, btWavePos, cameraTransform.rotation);
            return;
        }

        // Normal melee: OverlapCapsule for reliable close-range detection
        Vector3 startPoint = cameraTransform.position;
        Vector3 endPoint = cameraTransform.position + cameraTransform.forward * attackRange;
        Collider[] hitColliders = Physics.OverlapCapsule(startPoint, endPoint, attackRadius);

        float multiplier = pendingSwing != null ? pendingSwing.damageMultiplier : 1f;
        int swingDamage = Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));

        foreach (Collider col in hitColliders)
            if (col.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(swingDamage);

        // Normal wave (Lv2 unlock)
        if (wavesUnlocked)
        {
            Vector3 wavePos = cameraTransform.position + cameraTransform.forward * 0.5f;
            ObjectPooler.Instance.SpawnFromPool(wavePoolTag, wavePos, cameraTransform.rotation);
        }
    }

    private IEnumerator SwingRoutine(SwordSwing swing)
    {
        Vector3 offset = swingRotationOffset;
        if (swing != null && swing.proceduralRotationOffset != Vector3.zero)
            offset = swing.proceduralRotationOffset;

        targetSwingRotation = initialDisplayRotation * Quaternion.Euler(offset);
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
        pendingBulletTimeDeaths.Clear();
        OnBulletTimeStart?.Invoke();

        if (bulletTimeBarObj != null) bulletTimeBarObj.SetActive(true);
    }

    private void EndBulletTime()
    {
        isBulletTimeRunning = false;
        IsBulletTimeActive = false;
        bulletTimeCooldownTimer = bulletTimeCooldown;
        OnBulletTimeEnd?.Invoke();

        // Kill all enemies that reached 0 HP during bullet time. FlyingChaserEnemy covers
        // both the standard swarmer and the trail enemy.
        foreach (GameObject go in pendingBulletTimeDeaths)
        {
            if (go == null || !go.activeSelf) continue;
            if (go.TryGetComponent(out FlyingChaserEnemy chaser)) chaser.ForceDie();
            else if (go.TryGetComponent(out DasherEnemy dasher)) dasher.ForceDie();
        }
        pendingBulletTimeDeaths.Clear();
    }

    public static void RegisterBulletTimeDeath(GameObject enemy)
    {
        if (!pendingBulletTimeDeaths.Contains(enemy))
            pendingBulletTimeDeaths.Add(enemy);
    }

    // ── Bullet Time Bar (created programmatically) ────────────────────────────

    public void CreateBulletTimeBar()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
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

    // The swing is expressed purely as a moving rest rotation, so the shared sway
    // naturally blends into and out of it instead of fighting it.
    private void HandleWeaponSwayAndSwing() => ApplySway(targetSwingRotation, swingSpeed);
}
