using UnityEngine;
using System.Collections;

public class SpawnerDrone : MonoBehaviour
{
    [Header("Orbit")]
    public Transform arenaCenter;
    public float orbitRadius = 18f;
    public float orbitHeight = 5f;
    public float orbitSpeed = 0.4f;        // radians/s — full lap ≈ 15.7s
    public float selfRotationSpeed = 90f;  // degrees/s

    [Header("Entry")]
    public float entryStartRadius = 45f;
    public float entrySpeed = 10f;

    [Header("Enemy Spawning")]
    public string swarmerPoolTag = "StandardSwarmer";
    public string trailEnemyPoolTag = "TrailEnemy";
    [Tooltip("1 trail enemy spawns with every pack")]
    public bool spawnTrailEnemy = true;
    public int enemiesPerPack = 7;
    public float spawnInterval = 6f;
    public float spawnStagger = 0.1f;
    [Tooltip("Assign an empty child Transform at the top of the drone mesh. If left empty, falls back to the offset below.")]
    public Transform spawnPoint;
    public float packHeightOffset = 1.5f;

    private DroneWeakPoint[] weakPoints;
    private int destroyedWeakPoints;
    private float orbitAngle;
    private float entryTargetAngle;
    private float spawnTimer;
    private bool isOrbiting;

    private void Awake()
    {
        weakPoints = GetComponentsInChildren<DroneWeakPoint>(true);
    }

    private void OnEnable()
    {
        destroyedWeakPoints = 0;
        isOrbiting = false;
        spawnTimer = spawnInterval;

        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;

        entryTargetAngle = Random.Range(0f, Mathf.PI * 2f);
        orbitAngle = entryTargetAngle;

        transform.position = new Vector3(
            center.x + Mathf.Cos(entryTargetAngle) * entryStartRadius,
            center.y + orbitHeight,
            center.z + Mathf.Sin(entryTargetAngle) * entryStartRadius
        );

        QuotaManager.OnZoneCleared += OnZoneEnded;
        QuotaManager.OnGameCompleted += OnZoneEnded;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        QuotaManager.OnZoneCleared -= OnZoneEnded;
        QuotaManager.OnGameCompleted -= OnZoneEnded;
    }

    private void OnZoneEnded() => gameObject.SetActive(false);

    private void Update()
    {
        if (KatanaWeapon.IsBulletTimeActive) return;

        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;

        if (!isOrbiting)
        {
            Vector3 entryTarget = new Vector3(
                center.x + Mathf.Cos(entryTargetAngle) * orbitRadius,
                center.y + orbitHeight,
                center.z + Mathf.Sin(entryTargetAngle) * orbitRadius
            );
            transform.position = Vector3.MoveTowards(transform.position, entryTarget, entrySpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, entryTarget) < 0.5f)
                isOrbiting = true;
        }
        else
        {
            orbitAngle += orbitSpeed * Time.deltaTime;
            transform.position = new Vector3(
                center.x + Mathf.Cos(orbitAngle) * orbitRadius,
                center.y + orbitHeight,
                center.z + Mathf.Sin(orbitAngle) * orbitRadius
            );

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval;
                StartCoroutine(SpawnPack());
            }
        }

        transform.Rotate(0f, selfRotationSpeed * Time.deltaTime, 0f);
    }

    private IEnumerator SpawnPack()
    {
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up * packHeightOffset;
        for (int i = 0; i < enemiesPerPack; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            ObjectPooler.Instance.SpawnFromPool(swarmerPoolTag, basePos + offset, Quaternion.identity);
            yield return new WaitForSeconds(spawnStagger);
        }

        if (spawnTrailEnemy && !string.IsNullOrEmpty(trailEnemyPoolTag))
        {
            Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            ObjectPooler.Instance.SpawnFromPool(trailEnemyPoolTag, basePos + offset, Quaternion.identity);
        }
    }

    public void OnWeakPointDestroyed()
    {
        destroyedWeakPoints++;
        if (weakPoints.Length > 0 && destroyedWeakPoints >= weakPoints.Length)
        {
            EnemyEvents.ReportDeath();
            gameObject.SetActive(false);
        }
    }
}
