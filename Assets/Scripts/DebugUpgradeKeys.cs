using UnityEngine;
using UnityEngine.InputSystem;

// TEMPORARY — delete before shipping
// 1-4: toggle pistol upgrades   5-8: toggle sword upgrades
public class DebugUpgradeKeys : MonoBehaviour
{
    public AutomaticPistol pistol;
    public KatanaWeapon katana;

    private const float PistolFireRate      = 0.07f;
    private const float MaxHeatMultiplier   = 2f;
    private const float SwordRadiusIncrease = 1.2f;
    private const float CooldownMultiplier  = 0.55f;

    // Originals captured at start before any upgrades are applied
    private float originalFireRate;
    private float originalHeatPerShot;
    private float originalMaxHeat;
    private float originalAttackRadius;
    private float originalCooldownTime;

    private readonly bool[] pistolActive = new bool[4];
    private readonly bool[] swordActive  = new bool[4];

    private readonly string[] pistolLabels =
    {
        "1 - Pistol Lv1: Fire Rate Up",
        "2 - Pistol Lv2: Charge Shot",
        "3 - Pistol Lv3: Heat Capacity x2",
        "4 - Pistol Lv4: Auto Shotgun",
    };
    private readonly string[] swordLabels =
    {
        "5 - Sword Lv1: Wide Slash",
        "6 - Sword Lv2: Energy Waves",
        "7 - Sword Lv3: Faster Cooldown",
        "8 - Sword Lv4: Bullet Time",
    };

    private void Start()
    {
        if (pistol != null)
        {
            originalFireRate    = pistol.fireRate;
            originalHeatPerShot = pistol.heatPerShot;
            originalMaxHeat     = pistol.maxHeat;
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

        if (Keyboard.current.digit1Key.wasPressedThisFrame) TogglePistol(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) TogglePistol(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) TogglePistol(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) TogglePistol(3);

        if (Keyboard.current.digit5Key.wasPressedThisFrame) ToggleSword(0);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) ToggleSword(1);
        if (Keyboard.current.digit7Key.wasPressedThisFrame) ToggleSword(2);
        if (Keyboard.current.digit8Key.wasPressedThisFrame) ToggleSword(3);
    }

    private void TogglePistol(int level)
    {
        if (pistol == null) return;
        pistolActive[level] = !pistolActive[level];

        if (pistolActive[level])
        {
            switch (level)
            {
                case 0:
                    pistol.fireRate    = PistolFireRate;
                    pistol.heatPerShot = originalHeatPerShot * (PistolFireRate / originalFireRate) * 0.75f;
                    break;
                case 1: pistol.UnlockChargeAttack(); break;
                case 2: pistol.maxHeat = originalMaxHeat * MaxHeatMultiplier; break;
                case 3: pistol.UnlockShotgun(); break;
            }
        }
        else
        {
            switch (level)
            {
                case 0:
                    pistol.fireRate    = originalFireRate;
                    pistol.heatPerShot = originalHeatPerShot;
                    break;
                case 1: pistol.LockChargeAttack(); break;
                case 2: pistol.maxHeat = originalMaxHeat; break;
                case 3: pistol.LockShotgun(); break;
            }
        }
    }

    private void ToggleSword(int level)
    {
        if (katana == null) return;
        swordActive[level] = !swordActive[level];

        if (swordActive[level])
        {
            switch (level)
            {
                case 0: katana.attackRadius = originalAttackRadius + SwordRadiusIncrease; break;
                case 1: katana.UnlockWaves(); break;
                case 2: katana.cooldownTime = originalCooldownTime * CooldownMultiplier; break;
                case 3: katana.UnlockBulletTime(); break;
            }
        }
        else
        {
            switch (level)
            {
                case 0: katana.attackRadius = originalAttackRadius; break;
                case 1: katana.LockWaves(); break;
                case 2: katana.cooldownTime = originalCooldownTime; break;
                case 3: katana.LockBulletTime(); break;
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 260, 220));
        GUI.color = new Color(1f, 1f, 0f, 0.85f);
        GUILayout.Label("── DEBUG UPGRADES ──");
        for (int i = 0; i < 4; i++)
            GUILayout.Label((pistolActive[i] ? "✓ " : "  ") + pistolLabels[i]);
        GUILayout.Space(4);
        for (int i = 0; i < 4; i++)
            GUILayout.Label((swordActive[i] ? "✓ " : "  ") + swordLabels[i]);
        GUILayout.EndArea();
    }
}
