using UnityEngine;

/// <summary>
/// Forwards Animation Events from the animated sword model up to <see cref="KatanaWeapon"/>.
///
/// Unity delivers an Animation Event to components on the GameObject that owns the Animator,
/// and the Animator lives on a child of the view-model (so the clips and the weapon sway
/// drive different transforms). This relay bridges that gap — put it next to the Animator.
///
/// Usage in the Animation window: add an event on the contact frame of each swing clip and
/// pick <c>SwordHit</c>. Optionally add <c>SwordSwingEnd</c> on the last frame if the
/// controller has no Exit Time transition back to idle.
/// </summary>
public class SwordAnimationEvents : MonoBehaviour
{
    [Tooltip("Left empty, this is found on a parent at Awake.")]
    public KatanaWeapon katana;

    private void Awake()
    {
        if (katana == null) katana = GetComponentInParent<KatanaWeapon>();
    }

    /// <summary>Animation Event: the blade connects on this frame.</summary>
    public void SwordHit()
    {
        if (katana != null) katana.OnSwingHit();
    }

    /// <summary>Animation Event: this swing is done, return to idle.</summary>
    public void SwordSwingEnd()
    {
        if (katana != null) katana.OnSwingEnd();
    }
}
