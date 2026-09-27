using System.Collections.Generic;
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
///
/// Sub-emitters are cleared but never played directly. A sub-emitter only emits when its
/// parent's particles trigger it; calling Play on one runs it as an ordinary system, firing its
/// own burst from its own transform - a stray blood splat hanging in mid-air, for instance.
/// </summary>
public class PooledParticleEffect : MonoBehaviour
{
    // Floor for the pool-return delay, in case the systems report no duration at all
    private const float MinimumLifetime = 0.1f;

    private ParticleSystem[] systems;
    private readonly List<ParticleSystem> playableSystems = new List<ParticleSystem>();
    private float lifetime;

    private void Awake()
    {
        // Include inactive children so one that starts disabled is still measured and restarted
        systems = GetComponentsInChildren<ParticleSystem>(true);

        // Which systems are sub-emitters, and of whom. A disabled Sub Emitters module means its
        // entries are ignored at runtime, so those children still play as normal systems
        var triggeredBy = new Dictionary<ParticleSystem, List<ParticleSystem>>();
        foreach (ParticleSystem system in systems)
        {
            ParticleSystem.SubEmittersModule subEmitters = system.subEmitters;
            if (!subEmitters.enabled) continue;

            for (int i = 0; i < subEmitters.subEmittersCount; i++)
            {
                ParticleSystem sub = subEmitters.GetSubEmitterSystem(i);
                if (sub == null) continue;

                if (!triggeredBy.TryGetValue(sub, out List<ParticleSystem> parents))
                    triggeredBy[sub] = parents = new List<ParticleSystem>();
                parents.Add(system);
            }
        }

        // Measured from the systems rather than serialized, so retiming the effect - or
        // replacing it wholesale - cannot leave a stale lifetime behind
        foreach (ParticleSystem system in systems)
        {
            if (!triggeredBy.ContainsKey(system)) playableSystems.Add(system);
            lifetime = Mathf.Max(lifetime, LastParticleDeath(system, triggeredBy, systems.Length));
        }

        lifetime = Mathf.Max(lifetime, MinimumLifetime);
    }

    /// <summary>
    /// Seconds after Play until the system's last particle can die. A sub-emitter's own duration
    /// is irrelevant: it can be triggered as late as its parent's last particle death, and its
    /// particles then live their full lifetime on top of that.
    /// </summary>
    private static float LastParticleDeath(ParticleSystem system,
        Dictionary<ParticleSystem, List<ParticleSystem>> triggeredBy, int depthBudget)
    {
        ParticleSystem.MainModule main = system.main;

        // depthBudget guards against a sub-emitter loop, which would otherwise recurse forever
        if (!triggeredBy.TryGetValue(system, out List<ParticleSystem> parents) || depthBudget <= 0)
            return main.duration + main.startLifetime.constantMax;

        float latestTrigger = 0f;
        foreach (ParticleSystem parent in parents)
            latestTrigger = Mathf.Max(latestTrigger, LastParticleDeath(parent, triggeredBy, depthBudget - 1));

        return latestTrigger + main.startLifetime.constantMax;
    }

    private void OnEnable()
    {
        // Clear before playing: SpawnFromPool recycles the oldest instance even while it is
        // still running, so under sustained fire a burst would otherwise inherit the tail of
        // the previous one and appear to start half-finished
        foreach (ParticleSystem system in systems)
            system.Clear(false);

        foreach (ParticleSystem system in playableSystems)
            system.Play(false);

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
