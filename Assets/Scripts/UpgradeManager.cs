using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject upgradePanel;
    public TMP_Text pistolUpgradeDescription;
    public TMP_Text swordUpgradeDescription;
    public Button pistolUpgradeButton;
    public Button swordUpgradeButton;

    [Header("Weapon References")]
    public AutomaticPistol pistol;
    public KatanaWeapon katana;

    // ── Zone 1 ────────────────────────────────────────────────────────────────
    [Header("Zone 1 - Pistol")]
    public float z1_PistolFireRate = 0.07f;

    [Header("Zone 1 - Sword")]
    public float z1_SwordRadiusIncrease = 1.2f;

    // ── Zone 3 ────────────────────────────────────────────────────────────────
    [Header("Zone 3 - Pistol")]
    public float z3_MaxHeatMultiplier = 2f;

    [Header("Zone 3 - Sword")]
    public float z3_CooldownMultiplier = 0.55f;

    private static readonly string[] PistolDescriptions =
    {
        "FIRE RATE UP\nOverheat takes slightly longer.",
        "CHARGE SHOT\nTap to fire. Hold Q to charge a piercing laser beam.",
        "HEAT CAPACITY UP\nOverheat threshold doubled.",
        "AUTO SHOTGUN\nFiring for 3s triggers a wide burst. Interval drops to 2s with sustained fire.",
    };

    private static readonly string[] SwordDescriptions =
    {
        "WIDE SLASH\nIncreased attack radius for a broader sweep.",
        "ENERGY WAVES\nEach swing sends a piercing energy wave forward.",
        "SWIFT STRIKES\nSwing cooldown reduced by 45%.",
        "BULLET TIME  [E]\nFreeze all enemies. Mark them with your blade and unleash damage all at once on exit.",
    };

    private int pistolUpgradeLevel = 0;
    private int katanaUpgradeLevel = 0;
    private QuotaManager quotaManager;

    private void Start()
    {
        quotaManager = FindObjectOfType<QuotaManager>();
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    private void OnEnable()
    {
        QuotaManager.OnZoneCleared += OnZoneCleared;
    }

    private void OnDisable()
    {
        QuotaManager.OnZoneCleared -= OnZoneCleared;
        CancelInvoke();
    }

    private void OnZoneCleared()
    {
        // Skip panel if both weapons are fully upgraded
        if (pistolUpgradeLevel >= PistolDescriptions.Length && katanaUpgradeLevel >= SwordDescriptions.Length)
        {
            quotaManager?.RevealElevator();
            return;
        }

        Invoke(nameof(ShowPanel), 2.5f);
    }

    private void ShowPanel()
    {
        // Pistol card
        bool pistolAvailable = pistolUpgradeLevel < PistolDescriptions.Length;
        if (pistolUpgradeDescription != null)
            pistolUpgradeDescription.text = pistolAvailable ? PistolDescriptions[pistolUpgradeLevel] : "FULLY UPGRADED";
        if (pistolUpgradeButton != null)
            pistolUpgradeButton.interactable = pistolAvailable;

        // Sword card
        bool swordAvailable = katanaUpgradeLevel < SwordDescriptions.Length;
        if (swordUpgradeDescription != null)
            swordUpgradeDescription.text = swordAvailable ? SwordDescriptions[katanaUpgradeLevel] : "FULLY UPGRADED";
        if (swordUpgradeButton != null)
            swordUpgradeButton.interactable = swordAvailable;

        if (upgradePanel != null) upgradePanel.SetActive(true);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ChoosePistolUpgrade()
    {
        if (pistol != null)
        {
            switch (pistolUpgradeLevel)
            {
                case 0:
                    float oldRate = pistol.fireRate;
                    pistol.fireRate = z1_PistolFireRate;
                    pistol.heatPerShot *= (z1_PistolFireRate / oldRate) * 0.75f;
                    break;
                case 1:
                    pistol.UnlockChargeAttack();
                    break;
                case 2:
                    pistol.maxHeat *= z3_MaxHeatMultiplier;
                    break;
                case 3:
                    pistol.UnlockShotgun();
                    break;
            }
        }

        pistolUpgradeLevel++;
        ClosePanel();
    }

    public void ChooseSwordUpgrade()
    {
        if (katana != null)
        {
            switch (katanaUpgradeLevel)
            {
                case 0:
                    katana.attackRadius += z1_SwordRadiusIncrease;
                    break;
                case 1:
                    katana.UnlockWaves();
                    break;
                case 2:
                    katana.cooldownTime *= z3_CooldownMultiplier;
                    break;
                case 3:
                    katana.UnlockBulletTime();
                    break;
            }
        }

        katanaUpgradeLevel++;
        katana?.SetSwordVisual(katanaUpgradeLevel);
        ClosePanel();
    }

    private void ClosePanel()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        quotaManager?.RevealElevator();
    }
}
