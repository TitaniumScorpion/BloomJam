using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject inGameUI; // Holds crosshair, quota text, etc.
    public GameObject deathScreen;
    public GameObject victoryScreen;

    [Header("UI Elements")]
    public TMP_Text quotaText;

    public static GameManager Instance;

    private void Awake()
    {
        // Singleton pattern: Ensure only ONE GameManager ever exists
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); // Must be at the root of the hierarchy to persist
            DontDestroyOnLoad(gameObject); // Carry this object into the next scene!
        }
        else
        {
            Destroy(gameObject); // If we reload Zone 1, destroy the duplicate
        }
    }

    private void OnEnable()
    {
        // Listen to events from our other systems
        QuotaManager.OnKillCountUpdated += UpdateQuotaText;
        QuotaManager.OnGameCompleted += ShowVictoryScreen;
        PlayerHealth.OnPlayerDied += ShowDeathScreen;
    }

    private void OnDisable()
    {
        QuotaManager.OnKillCountUpdated -= UpdateQuotaText;
        QuotaManager.OnGameCompleted -= ShowVictoryScreen;
        PlayerHealth.OnPlayerDied -= ShowDeathScreen;
    }

    private void Start()
    {
        // Ensure the correct UI panels are active at the start
        if (inGameUI != null) inGameUI.SetActive(true);
        if (deathScreen != null) deathScreen.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(false);
    }

    private void UpdateQuotaText(int currentKills, int targetQuota)
    {
        if (quotaText != null)
        {
            quotaText.text = $"QUOTA: {currentKills} / {targetQuota}";
        }
    }

    private void ShowDeathScreen()
    {
        if (inGameUI != null) inGameUI.SetActive(false);
        if (deathScreen != null) deathScreen.SetActive(true);
        
        // Unlock the cursor so the player can click the Restart button
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ShowVictoryScreen()
    {
        if (inGameUI != null) inGameUI.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(true);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // --- Button Methods ---
    
    public void RestartGame()
    {
        // Reset our static kill counts and zone progress for the new run
        QuotaManager.ResetProgression(); 
        
        // Load the very first scene in the Build Settings (Zone 1)
        SceneManager.LoadScene(0); 

        // Reset UI states since Start() won't be called again on a persistent object
        if (inGameUI != null) inGameUI.SetActive(true);
        if (deathScreen != null) deathScreen.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(false);
        
        // Re-lock the cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}