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
    [Tooltip("Shown instead of the upgrade cards when the left panel is focused but nothing is available yet")]
    public GameObject noUpgradeAvailableMessage;

    [Header("Weapon References")]
    public AutomaticPistol pistol;
    public KatanaWeapon katana;

    [Header("Panel Index")]
    [Tooltip("This panel's index in ElevatorHub.panels — the upgrade UI only shows while this one is focused")]
    public int leftPanelIndex = 1;

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

    // True once a zone's been cleared and the player hasn't spent that zone's upgrade choice yet.
    // Starts false — no upgrades are available at the very start of the game.
    private bool hasPendingUpgrade;

    private void Start()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (noUpgradeAvailableMessage != null) noUpgradeAvailableMessage.SetActive(false);
    }

    private void OnEnable()
    {
        QuotaManager.OnZoneCleared += OnZoneCleared;
        ElevatorHub.OnPanelFocusChanged += OnPanelFocusChanged;
        ElevatorHub.OnHubModeExit += HidePanel;
    }

    private void OnDisable()
    {
        QuotaManager.OnZoneCleared -= OnZoneCleared;
        ElevatorHub.OnPanelFocusChanged -= OnPanelFocusChanged;
        ElevatorHub.OnHubModeExit -= HidePanel;
    }

    private void OnZoneCleared()
    {
        // Nothing to flag if both weapons are already fully upgraded
        hasPendingUpgrade = pistolUpgradeLevel < PistolDescriptions.Length || katanaUpgradeLevel < SwordDescriptions.Length;
    }

    private void OnPanelFocusChanged(int index)
    {
        if (index != leftPanelIndex)
        {
            HidePanel();
            return;
        }

        if (hasPendingUpgrade)
            ShowPanel();
        else
            ShowNoUpgradeMessage();
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
        if (noUpgradeAvailableMessage != null) noUpgradeAvailableMessage.SetActive(false);
    }

    private void ShowNoUpgradeMessage()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (noUpgradeAvailableMessage != null) noUpgradeAvailableMessage.SetActive(true);
    }

    private void HidePanel()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (noUpgradeAvailableMessage != null) noUpgradeAvailableMessage.SetActive(false);
    }

    public void ChoosePistolUpgrade()
    {
        // Guards against clicking before the panel's actually focused/populated
        if (!ElevatorHub.IsActive || !hasPendingUpgrade) return;

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
        OnUpgradeChosen();
    }

    public void ChooseSwordUpgrade()
    {
        // Guards against clicking before the panel's actually focused/populated
        if (!ElevatorHub.IsActive || !hasPendingUpgrade) return;

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
        OnUpgradeChosen();
    }

    private void OnUpgradeChosen()
    {
        hasPendingUpgrade = false;
        ShowNoUpgradeMessage();
    }
}
