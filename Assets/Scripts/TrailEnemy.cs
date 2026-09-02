using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A flying chaser that lays a trail of damaging segments behind it, turning its flight path
/// into a temporary hazard the player has to route around.
///
/// Flight, health, hit flash and bullet-time handling all come from FlyingChaserEnemy; only
/// the trail is implemented here.
/// </summary>
public class TrailEnemy : FlyingChaserEnemy
{
    [Header("Trail")]
    public string trailSegmentPoolTag = "TrailSegment";
    [Tooltip("Seconds between each trail segment being placed")]
    public float segmentSpawnInterval = 0.25f;
    [Tooltip("Max segments alive — oldest deactivates when exceeded, creating dissolve-from-start")]
    public int maxTrailSegments = 16;

    // FIFO so the oldest segment is always the one retired, dissolving the tail from its start
    private readonly Queue<GameObject> activeSegments = new Queue<GameObject>();
    private float segmentTimer;

    protected override void OnEnable()
    {
        base.OnEnable();
        segmentTimer = segmentSpawnInterval;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ClearTrail(); // Never leave orphaned hazards in the arena after dying
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
            GameObject oldest = activeSegments.Dequeue();
            if (oldest != null && oldest.activeSelf) oldest.SetActive(false);
        }

        GameObject segment = ObjectPooler.Instance.SpawnFromPool(trailSegmentPoolTag, transform.position, Quaternion.identity);
        if (segment != null) activeSegments.Enqueue(segment);
    }

    private void ClearTrail()
    {
        while (activeSegments.Count > 0)
        {
            GameObject segment = activeSegments.Dequeue();
            if (segment != null && segment.activeSelf) segment.SetActive(false);
        }
    }
}
