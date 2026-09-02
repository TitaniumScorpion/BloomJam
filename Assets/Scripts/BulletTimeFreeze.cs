using UnityEngine;

/// <summary>
/// Suspends and restores a Rigidbody's motion for the duration of bullet time.
///
/// Bullet time is not implemented with Time.timeScale — the player keeps moving at full
/// speed while everything else stops — so each moving object is responsible for holding
/// itself still. This packages that stash/restore so the three call sites cannot drift
/// apart, and so a new projectile or enemy type gets it in one line.
///
/// Gravity is stored rather than assumed: enemy projectiles arc under gravity and must
/// have it restored, while sword waves and dashers never use it. Storing the original
/// value covers both without a special case.
///
/// Declare as a plain field and call Tick on it directly. It is a mutable struct, so
/// copying it to a local (`var f = freeze;`) would update the copy and silently do nothing.
/// </summary>
public struct BulletTimeFreeze
{
    private bool isFrozen;
    private Vector3 storedVelocity;
    private bool storedUseGravity;

    /// <summary>
    /// Freezes while bullet time is active. Returns true if the object is currently frozen,
    /// in which case the caller should skip the rest of its physics step.
    /// </summary>
    public bool Tick(Rigidbody rb) => Tick(rb, KatanaWeapon.IsBulletTimeActive);

    /// <param name="shouldFreeze">
    /// Lets a caller add its own condition — the bullet-time sword wave stays moving while
    /// everything else is held still.
    /// </param>
    public bool Tick(Rigidbody rb, bool shouldFreeze)
    {
        if (shouldFreeze)
        {
            if (!isFrozen)
            {
                storedVelocity = rb.linearVelocity;
                storedUseGravity = rb.useGravity;
                rb.linearVelocity = Vector3.zero;
                rb.useGravity = false;
                isFrozen = true;
            }
            return true;
        }

        if (isFrozen)
        {
            rb.linearVelocity = storedVelocity;
            rb.useGravity = storedUseGravity;
            isFrozen = false;
        }
        return false;
    }

    /// <summary>
    /// Clears the frozen flag without touching the Rigidbody. Pooled objects must call this
    /// from OnEnable, or an instance recycled mid-bullet-time would restore a stale velocity.
    /// </summary>
    public void Reset() => isFrozen = false;
}
