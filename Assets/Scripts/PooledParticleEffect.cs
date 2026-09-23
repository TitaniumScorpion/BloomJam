using UnityEngine;

/// <summary>
/// Drives a one-shot particle effect that lives in the ObjectPooler, and returns it to the pool
/// when the burst finishes.
///
/// A ParticleSystem's "Play On Awake" only fires from Awake, which for a pooled instance runs
/// exactly once - when ObjectPooler.Start instantiates it. The instance plays its burst there
/// and is deactivated a line later, so every SetActive(true) afterwards re-enables a system
/// that has already completed. It emits nothing and the effect is invisible, with no error to
/// point at it. Replaying explicitly on enable is the fix.
///
/// Prefer this over AutoDeactivate for anything containing a ParticleSystem. AutoDeactivate
/// only handles the pool return, and its hand-set lifetime has to be re-tuned by hand whenever
/// the effect is retimed - a number that is too short truncates the burst silently.
/// </summary>
public class PooledParticleEffect : MonoBehaviour
{
    // Floor for the pool-return delay, in case the systems report no duration at all
    private const float MinimumLifetime = 0.1f;

    private ParticleSystem[] systems;
    private float lifetime;

    private void Awake()
    {
        // Include inactive children so one that starts disabled is still measured and restarted
        systems = GetComponentsInChildren<ParticleSystem>(true);

        // Measured from the systems rather than serialized, so retiming the effect - or
        // replacing it wholesale - cannot leave a stale lifetime behind
        foreach (ParticleSystem system in systems)
        {
            ParticleSystem.MainModule main = system.main;
            lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
        }

        lifetime = Mathf.Max(lifetime, MinimumLifetime);
    }

    private void OnEnable()
    {
        // Clear before playing: SpawnFromPool recycles the oldest instance even while it is
        // still running, so under sustained fire a burst would otherwise inherit the tail of
        // the previous one and appear to start half-finished
        foreach (ParticleSystem system in systems)
        {
            system.Clear(false);
            system.Play(false);
        }

        Invoke(nameof(Deactivate), lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
