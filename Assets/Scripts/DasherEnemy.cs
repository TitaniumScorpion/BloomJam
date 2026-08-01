using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class DasherEnemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 3;
    public int dashDamage = 1;

    [Header("Wandering")]
    public float wanderSpeed = 3f;
    public float minWanderHeight = 2f;
    public float maxWanderHeight = 6f;

    [Header("Detection Zones")]
    [Tooltip("Inner radius — player entering this stops the dasher and starts the countdown")]
    public float detectRadius = 6f;
    [Tooltip("Outer radius — player must exit this within prepareDuration to cancel the dash")]
    public float escapeRadius = 10f;

    [Header("Dash")]
    public float prepareDuration = 1.5f;
    public float dashSpeed = 22f;
    public float dashDuration = 0.5f;

    [Header("Hit Flash")]
    public Renderer enemyRenderer;
    public Material flashMaterial;
    public float flashDuration = 0.1f;

    private enum DasherState { Wandering, Preparing, Dashing }
    private DasherState state;

    private int currentHealth;
    private Rigidbody rb;
    private Transform playerTransform;
    private Vector3 wanderTarget;
    private float prepareTimer;
    private Vector3 dashDirection;
    private float dashTimer;
    private bool wasFrozen;
    private Vector3 frozenVelocity;
    private Material originalMaterial;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.freezeRotation = true;

        if (enemyRenderer != null)
            originalMaterial = enemyRenderer.sharedMaterial;
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        state = DasherState.Wandering;
        wasFrozen = false;
        prepareTimer = 0f;
        dashTimer = 0f;
        wanderTarget = PickWanderTarget();

        if (enemyRenderer != null && originalMaterial != null)
            enemyRenderer.sharedMaterial = originalMaterial;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        QuotaManager.OnZoneCleared += Despawn;
        QuotaManager.OnGameCompleted += Despawn;
    }

    private void OnDisable()
    {
        rb.linearVelocity = Vector3.zero;
        QuotaManager.OnZoneCleared -= Despawn;
        QuotaManager.OnGameCompleted -= Despawn;
    }

    private void Despawn() => gameObject.SetActive(false);

    private void Update()
    {
        if (KatanaWeapon.IsBulletTimeActive) return;
        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        switch (state)
        {
            case DasherState.Wandering:
                if (distToPlayer <= detectRadius)
                {
                    state = DasherState.Preparing;
                    prepareTimer = 0f;
                    rb.linearVelocity = Vector3.zero;
                }
                break;

            case DasherState.Preparing:
                // Face the player
                Vector3 toPlayer = playerTransform.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(toPlayer), Time.deltaTime * 8f);

                // Player escaped the outer zone — cancel
                if (distToPlayer > escapeRadius)
                {
                    state = DasherState.Wandering;
                    wanderTarget = PickWanderTarget();
                    break;
                }

                prepareTimer += Time.deltaTime;
                if (prepareTimer >= prepareDuration)
                {
                    dashDirection = (playerTransform.position - transform.position).normalized;
                    dashTimer = 0f;
                    state = DasherState.Dashing;
                }
                break;

            case DasherState.Dashing:
                dashTimer += Time.deltaTime;
                if (dashTimer >= dashDuration)
                {
                    rb.linearVelocity = Vector3.zero;

                    if (distToPlayer <= escapeRadius)
                    {
                        // Player still nearby — prepare another dash
                        state = DasherState.Preparing;
                        prepareTimer = 0f;
                    }
                    else
                    {
                        state = DasherState.Wandering;
                        wanderTarget = PickWanderTarget();
                    }
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        if (KatanaWeapon.IsBulletTimeActive)
        {
            if (!wasFrozen)
            {
                frozenVelocity = rb.linearVelocity;
                rb.linearVelocity = Vector3.zero;
                wasFrozen = true;
            }
            return;
        }

        if (wasFrozen)
        {
            rb.linearVelocity = frozenVelocity;
            wasFrozen = false;
        }

        switch (state)
        {
            case DasherState.Wandering:
                Vector3 toTarget = wanderTarget - transform.position;
                if (toTarget.magnitude < 1f)
                    wanderTarget = PickWanderTarget();
                else
                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity,
                        toTarget.normalized * wanderSpeed, Time.fixedDeltaTime * 3f);
                break;

            case DasherState.Preparing:
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
                break;

            case DasherState.Dashing:
                rb.linearVelocity = dashDirection * dashSpeed;
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state != DasherState.Dashing) return;
        if (collision.gameObject.CompareTag("Player")
            && collision.gameObject.TryGetComponent(out PlayerHealth health))
            health.TakeDamage(dashDamage);
    }

    private Vector3 PickWanderTarget()
    {
        Vector2 circle = Random.insideUnitCircle * EnemySpawner.CurrentSpawnRadius * 0.75f;
        float height = Random.Range(minWanderHeight, maxWanderHeight);
        return EnemySpawner.CurrentArenaCenter + new Vector3(circle.x, height, circle.y);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            StandardSwarmer.ReportDeath();
            gameObject.SetActive(false);
        }
        else if (enemyRenderer != null && flashMaterial != null && gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        enemyRenderer.sharedMaterial = flashMaterial;
        yield return new WaitForSeconds(flashDuration);
        if (enemyRenderer != null && originalMaterial != null)
            enemyRenderer.sharedMaterial = originalMaterial;
    }
}
