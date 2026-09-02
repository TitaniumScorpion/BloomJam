using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class TrailEnemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 1;
    public int collisionDamage = 1;

    [Header("Movement")]
    public float moveSpeed = 22f;
    public float minMoveSpeed = 2f;
    public float acceleration = 14f;
    public float rotationSpeed = 6f;
    public float tiltAmount = 30f;

    [Header("Wobble")]
    public float pitchWobbleSpeed = 8f;
    public float pitchWobbleAmount = 15f;

    [Header("Ground Avoidance")]
    public float hoverHeight = 1.5f;

    [Header("Hit Flash")]
    public Renderer enemyRenderer;
    public Material flashMaterial;
    public float flashDuration = 0.1f;

    [Header("Trail")]
    public string trailSegmentPoolTag = "TrailSegment";
    [Tooltip("Seconds between each trail segment being placed")]
    public float segmentSpawnInterval = 0.25f;
    [Tooltip("Max segments alive — oldest deactivates when exceeded, creating dissolve-from-start")]
    public int maxTrailSegments = 16;

    private int currentHealth;
    private Rigidbody rb;
    private Transform playerTransform;
    private float currentSpeed;
    private Quaternion baseRotation;
    private AudioSource moveAudioSource;
    private float moveSoundTimer;
    private Material originalMaterial;
    private Coroutine flashCoroutine;
    private bool markedForBulletTimeDeath;

    private readonly Queue<GameObject> activeSegments = new Queue<GameObject>();
    private float segmentTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        moveAudioSource = gameObject.AddComponent<AudioSource>();
        moveAudioSource.spatialBlend = 1f;
        moveAudioSource.minDistance = 3f;
        moveAudioSource.maxDistance = 50f;
        moveAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        moveAudioSource.playOnAwake = false;

        if (enemyRenderer != null)
            originalMaterial = enemyRenderer.sharedMaterial;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        currentSpeed = minMoveSpeed;
        baseRotation = transform.rotation;
        moveSoundTimer = Random.Range(0.5f, 1.5f);
        segmentTimer = segmentSpawnInterval;

        markedForBulletTimeDeath = false;

        if (enemyRenderer != null && originalMaterial != null)
            enemyRenderer.sharedMaterial = originalMaterial;

        QuotaManager.OnZoneCleared += Despawn;
        QuotaManager.OnGameCompleted += Despawn;
    }

    private void OnDisable()
    {
        if (moveAudioSource != null) moveAudioSource.Stop();
        ClearTrail();
        QuotaManager.OnZoneCleared -= Despawn;
        QuotaManager.OnGameCompleted -= Despawn;
    }

    private void Despawn() => gameObject.SetActive(false);

    private void ClearTrail()
    {
        while (activeSegments.Count > 0)
        {
            GameObject seg = activeSegments.Dequeue();
            if (seg != null && seg.activeSelf) seg.SetActive(false);
        }
    }

    private void Update()
    {
        if (KatanaWeapon.IsBulletTimeActive) return;

        segmentTimer -= Time.deltaTime;
        if (segmentTimer <= 0f)
        {
            segmentTimer = segmentSpawnInterval;
            SpawnSegment();
        }
    }

    private void SpawnSegment()
    {
        if (string.IsNullOrEmpty(trailSegmentPoolTag)) return;

        if (activeSegments.Count >= maxTrailSegments)
        {
            GameObject old = activeSegments.Dequeue();
            if (old != null && old.activeSelf) old.SetActive(false);
        }

        GameObject seg = ObjectPooler.Instance.SpawnFromPool(trailSegmentPoolTag, transform.position, Quaternion.identity);
        if (seg != null) activeSegments.Enqueue(seg);
    }

    private void FixedUpdate()
    {
        if (playerTransform == null) return;

        if (KatanaWeapon.IsBulletTimeActive)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 targetPosition = playerTransform.position + Vector3.up * 1.5f;
        Vector3 direction = (targetPosition - transform.position).normalized;

        Vector3 baseForward = baseRotation * Vector3.forward;
        Vector3 baseRight = baseRotation * Vector3.right;

        float alignment = Vector3.Dot(baseForward, direction);
        if (alignment > 0.6f)
            currentSpeed += acceleration * Time.fixedDeltaTime;
        else
            currentSpeed -= (acceleration * 3f) * Time.fixedDeltaTime;
        currentSpeed = Mathf.Clamp(currentSpeed, minMoveSpeed, moveSpeed);

        float speedPercent = (currentSpeed - minMoveSpeed) / (moveSpeed - minMoveSpeed);
        float currentRotationSpeed = Mathf.Lerp(rotationSpeed, 0.1f, speedPercent);

        Quaternion targetLook = Quaternion.LookRotation(direction);
        baseRotation = Quaternion.Slerp(baseRotation, targetLook, Time.fixedDeltaTime * currentRotationSpeed);

        float turnAmount = Vector3.Dot(baseRight, direction);
        float wobble = Mathf.Sin(Time.time * pitchWobbleSpeed) * pitchWobbleAmount;
        Quaternion tiltRotation = Quaternion.Euler(wobble, 0f, -turnAmount * tiltAmount);

        Quaternion smoothedFinalRotation = Quaternion.Slerp(transform.rotation, baseRotation * tiltRotation, Time.fixedDeltaTime * 12f);
        rb.MoveRotation(smoothedFinalRotation);

        Vector3 targetVelocity = (smoothedFinalRotation * Vector3.forward) * currentSpeed;

        if (Physics.SphereCast(transform.position, 0.5f, Vector3.down, out RaycastHit hit, hoverHeight * 1.5f))
        {
            if (hit.rigidbody == null && hit.distance < hoverHeight)
            {
                targetVelocity.y = Mathf.Max(targetVelocity.y, 0f);
                float upwardPush = (hoverHeight - hit.distance) * 20f;
                targetVelocity.y += upwardPush;
                if (rb.linearVelocity.y < 0f)
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            }
        }

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);

        moveSoundTimer -= Time.fixedDeltaTime;
        if (moveSoundTimer <= 0f)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.enemyMoveSound != null)
            {
                moveAudioSource.clip = AudioManager.Instance.enemyMoveSound;
                moveAudioSource.volume = AudioManager.Instance.enemyMoveVolume;
                moveAudioSource.pitch = Random.Range(0.8f, 1.2f);
                moveAudioSource.Play();
            }
            moveSoundTimer = Random.Range(1f, 2.5f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")
            && collision.gameObject.TryGetComponent(out PlayerHealth health))
            health.TakeDamage(collisionDamage);
    }

    public void TakeDamage(int damage)
    {
        if (markedForBulletTimeDeath) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            if (KatanaWeapon.IsBulletTimeActive)
            {
                markedForBulletTimeDeath = true;
                KatanaWeapon.RegisterBulletTimeDeath(gameObject);
                if (enemyRenderer != null && KatanaWeapon.BulletTimeMarkMaterial != null)
                    enemyRenderer.sharedMaterial = KatanaWeapon.BulletTimeMarkMaterial;
            }
            else
            {
                ForceDie();
            }
        }
        else if (enemyRenderer != null && flashMaterial != null && gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    public void ForceDie()
    {
        StandardSwarmer.ReportDeath();
        gameObject.SetActive(false);
    }

    private IEnumerator FlashRoutine()
    {
        enemyRenderer.sharedMaterial = flashMaterial;
        yield return new WaitForSeconds(flashDuration);
        if (enemyRenderer != null && originalMaterial != null)
            enemyRenderer.sharedMaterial = originalMaterial;
    }
}
