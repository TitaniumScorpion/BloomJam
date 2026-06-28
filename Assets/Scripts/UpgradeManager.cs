using UnityEngine;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject upgradePanel;
    public TMP_Text pistolUpgradeDescription;
    public TMP_Text swordUpgradeDescription;

    [Header("Weapon References")]
    public AutomaticPistol pistol;
    public KatanaWeapon katana;

    [Header("Zone 1 - Pistol Upgrade")]
    public float z1_PistolFireRate = 0.07f;

    [Header("Zone 1 - Sword Upgrade")]
    public float z1_SwordRangeIncrease = 1.5f;

    private static readonly string[] PistolDescriptions =
    {
        "Fire Rate Up\nOverheat adjusted to maintain the same duration.", // Zone 1
    };

    private static readonly string[] SwordDescriptions =
    {
        "Attack Range Up\n+1.5m reach on each slash.", // Zone 1
    };

    private QuotaManager quotaManager;
    private int upgradeZoneIndex;

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
        upgradeZoneIndex = QuotaManager.currentZoneIndex;

        // If no upgrade is defined for this zone, just reveal the elevator immediately
        if (upgradeZoneIndex >= PistolDescriptions.Length)
        {
            quotaManager?.RevealElevator();
            return;
        }

        // Wait for the zone-cleared message to finish before showing the upgrade panel
        Invoke(nameof(ShowPanel), 2.5f);
    }

    private void ShowPanel()
    {
        if (pistolUpgradeDescription != null)
            pistolUpgradeDescription.text = PistolDescriptions[upgradeZoneIndex];

        if (swordUpgradeDescription != null)
            swordUpgradeDescription.text = SwordDescriptions[upgradeZoneIndex];

        if (upgradePanel != null) upgradePanel.SetActive(true);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ChoosePistolUpgrade()
    {
        if (pistol != null)
        {
            switch (upgradeZoneIndex)
            {
                case 0:
                    float oldRate = pistol.fireRate;
                    pistol.fireRate = z1_PistolFireRate;
                    // Scale heatPerShot proportionally so overheat takes the same real-world time
                    pistol.heatPerShot *= z1_PistolFireRate / oldRate;
                    break;
            }
        }

        ClosePanel();
    }

    public void ChooseSwordUpgrade()
    {
        if (katana != null)
        {
            switch (upgradeZoneIndex)
            {
                case 0:
                    katana.attackRange += z1_SwordRangeIncrease;
                    break;
            }
        }

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
