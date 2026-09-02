using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// TEMPORARY — delete before shipping.
/// 1-4 toggle pistol upgrades, 5-8 toggle sword upgrades, in any order and combination.
///
/// Applying an upgrade calls straight into UpgradeManager, so what you test here is the
/// real upgrade rather than a second copy of it. Reverting stays local, because the live
/// progression never needs to undo anything — it only ever moves forward.
/// </summary>
public class DebugUpgradeKeys : MonoBehaviour
{
    public AutomaticPistol pistol;
    public KatanaWeapon katana;
    [Tooltip("Source of the upgrade effects and values. Found automatically if left empty.")]
    public UpgradeManager upgradeManager;

    private static readonly Key[] PistolKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
    private static readonly Key[] SwordKeys = { Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8 };

    // Captured at Start, before any upgrade is applied — the only way to undo a tuning change
    private float originalFireRate;
    private float originalHeatPerShot;
    private float originalMaxHeat;
    private float originalAttackRadius;
    private float originalCooldownTime;

    private readonly bool[] pistolActive = new bool[UpgradeManager.PistolDescriptions.Length];
    private readonly bool[] swordActive = new bool[UpgradeManager.SwordDescriptions.Length];

    private void Start()
    {
        if (upgradeManager == null) upgradeManager = FindFirstObjectByType<UpgradeManager>();

        if (pistol != null)
        {
            originalFireRate = pistol.fireRate;
            originalHeatPerShot = pistol.heatPerShot;
            originalMaxHeat = pistol.maxHeat;
        }
        if (katana != null)
        {
            originalAttackRadius = katana.attackRadius;
            originalCooldownTime = katana.cooldownTime;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        for (int i = 0; i < PistolKeys.Length; i++)
            if (Keyboard.current[PistolKeys[i]].wasPressedThisFrame) TogglePistol(i);

        for (int i = 0; i < SwordKeys.Length; i++)
            if (Keyboard.current[SwordKeys[i]].wasPressedThisFrame) ToggleSword(i);
    }

    private void TogglePistol(int level)
    {
        if (pistol == null || upgradeManager == null) return;

        pistolActive[level] = !pistolActive[level];

        if (pistolActive[level])
            upgradeManager.ApplyPistolUpgrade(level);
        else
            RevertPistolUpgrade(level);
    }

    // Restores from the captured originals rather than inverting the maths, so repeated
    // toggling cannot accumulate rounding drift
    private void RevertPistolUpgrade(int level)
    {
        switch (level)
        {
            case 0:
                pistol.fireRate = originalFireRate;
                pistol.heatPerShot = originalHeatPerShot;
                break;
            case 1: pistol.LockChargeAttack(); break;
            case 2: pistol.maxHeat = originalMaxHeat; break;
            case 3: pistol.LockShotgun(); break;
        }
    }

    private void ToggleSword(int level)
    {
        if (katana == null || upgradeManager == null) return;

        swordActive[level] = !swordActive[level];

        if (swordActive[level])
            upgradeManager.ApplySwordUpgrade(level);
        else
            RevertSwordUpgrade(level);

        // Blade visual reflects how many upgrades are live, not which — the toggles can be
        // enabled in any order, so a level index would be meaningless here
        int activeCount = 0;
        foreach (bool active in swordActive)
            if (active) activeCount++;
        katana.SetSwordVisual(activeCount);
    }

    private void RevertSwordUpgrade(int level)
    {
        switch (level)
        {
            case 0: katana.attackRadius = originalAttackRadius; break;
            case 1: katana.LockWaves(); break;
            case 2: katana.cooldownTime = originalCooldownTime; break;
            case 3: katana.LockBulletTime(); break;
        }
    }

    /// <summary>First line of an upgrade description — the full text is too long for the overlay.</summary>
    private static string ShortLabel(string description)
    {
        int newline = description.IndexOf('\n');
        return newline < 0 ? description : description.Substring(0, newline);
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 240));
        GUI.color = new Color(1f, 1f, 0f, 0.85f);
        GUILayout.Label("── DEBUG UPGRADES ──");

        for (int i = 0; i < pistolActive.Length; i++)
            GUILayout.Label($"{(pistolActive[i] ? "✓" : "  ")} {i + 1} - Pistol: {ShortLabel(UpgradeManager.PistolDescriptions[i])}");

        GUILayout.Space(4);

        for (int i = 0; i < swordActive.Length; i++)
            GUILayout.Label($"{(swordActive[i] ? "✓" : "  ")} {i + 5} - Sword: {ShortLabel(UpgradeManager.SwordDescriptions[i])}");

        GUILayout.EndArea();
    }
}
