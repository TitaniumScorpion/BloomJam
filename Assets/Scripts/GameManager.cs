using System.Collections;
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
    
    [Header("Run Timer UI")]
    public TMP_Text deathTimeText;
    public TMP_Text victoryTimeText;
    private float currentRunTime = 0f;
    private bool isTimerRunning = false;

    [Header("Zone Transition UI")]
    public GameObject zoneTransitionScreen;
    public TMP_Text zoneTransitionText;

    [Header("Zone Cleared UI")]
    public GameObject levelCompletedMessage;

    public static GameManager Instance;
    private Coroutine transitionCoroutine;

    [Header("Single Scene Progression")]
    public GameObject[] zones; // Drag Zone 1, Zone 2, Zone 3 parent objects here
    public Transform[] playerSpawnPoints; // Drag the spawn point transforms for each zone here
    public GameObject playerObject; // Drag the Player here

    //YILMAZ THE KOD MAESTER WAS HERE
    [SerializeField] private float skyboxRotationSpeed = 1f;

    private void Awake()
    {
        // Singleton pattern for the CURRENT scene only
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    private void OnEnable()
    {
        // Listen to events from our other systems
        QuotaManager.OnKillCountUpdated += UpdateQuotaText;
        QuotaManager.OnZoneCleared += ShowLevelCompletedMessage;
        QuotaManager.OnGameCompleted += ShowVictoryScreen;
        PlayerHealth.OnPlayerDied += ShowDeathScreen;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        QuotaManager.OnKillCountUpdated -= UpdateQuotaText;
        QuotaManager.OnZoneCleared -= ShowLevelCompletedMessage;
        QuotaManager.OnGameCompleted -= ShowVictoryScreen;
        PlayerHealth.OnPlayerDied -= ShowDeathScreen;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (deathScreen != null) deathScreen.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (levelCompletedMessage != null) levelCompletedMessage.SetActive(false);
        
        // If the scene loaded before OnEnable could catch it (often happens on the very first play), manually start transition
        if (transitionCoroutine == null && SceneManager.GetActiveScene().buildIndex != 0)
        {
            transitionCoroutine = StartCoroutine(ZoneTransitionRoutine());
        }
    }

    private void Update()
    {
        // Track the run time silently in the background
        if (isTimerRunning)
        {
            currentRunTime += Time.deltaTime;
        }

        //YILMAZ THE KOD MAESTER WAS HERE
        RenderSettings.skybox.SetFloat("_Rotation", RenderSettings.skybox.GetFloat("_Rotation") + skyboxRotationSpeed * Time.deltaTime); 
        //YILMAZ THE KOD MAESTER WAS HERE

    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Hide the end screens just in case
        if (deathScreen != null) deathScreen.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (levelCompletedMessage != null) levelCompletedMessage.SetActive(false);

        // Do not trigger zone transition or lock cursor if returning to the Main Menu
        if (scene.buildIndex == 0)
        {
            if (inGameUI != null) inGameUI.SetActive(false);
            if (zoneTransitionScreen != null) zoneTransitionScreen.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // Stop any existing transition and start a fresh one for the newly loaded zone
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(ZoneTransitionRoutine());
    }

    public void AdvanceToNextZone()
    {
        // Called by the Elevator when it reaches the top
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(ZoneTransitionRoutine());
    }

    private IEnumerator ZoneTransitionRoutine()
    {
        isTimerRunning = false;
        Time.timeScale = 0f; // Freeze the physics and spawning entirely
        
        if (inGameUI != null) inGameUI.SetActive(false);
        if (zoneTransitionScreen != null)
        {
            zoneTransitionScreen.SetActive(true);
            CanvasGroup cg = zoneTransitionScreen.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f; // Ensure it starts fully opaque
        }

        // --- SINGLE SCENE ZONE SWAPPING ---
        // Enable the current zone and disable all others
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] != null)
                zones[i].SetActive(i == QuotaManager.currentZoneIndex);
        }

        // Teleport the player to the correct spawn point
        if (playerObject != null && playerSpawnPoints.Length > QuotaManager.currentZoneIndex && playerSpawnPoints[QuotaManager.currentZoneIndex] != null)
        {
            playerObject.transform.position = playerSpawnPoints[QuotaManager.currentZoneIndex].position;
        }
        
        QuotaManager qm = FindObjectOfType<QuotaManager>();
        if (qm != null) qm.StartNextZone();
        // ----------------------------------
        
        // --- ARENA CLEANUP ---
        // Despawn any surviving enemies or stray bullets from the previous zone
        foreach (StandardSwarmer swarmer in FindObjectsOfType<StandardSwarmer>())
            swarmer.gameObject.SetActive(false);
        foreach (AdvancedEnemy boss in FindObjectsOfType<AdvancedEnemy>())
            boss.gameObject.SetActive(false);
        foreach (Projectile proj in FindObjectsOfType<Projectile>())
            proj.gameObject.SetActive(false);
        foreach (EnemyProjectile eProj in FindObjectsOfType<EnemyProjectile>())
            eProj.gameObject.SetActive(false);
        // ---------------------

        // Lock the cursor so the player is ready to aim when it hits 0
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // +1 because zone index is 0-based, but we want to show "Zone 1", "Zone 2", etc.
        int displayZone = QuotaManager.currentZoneIndex + 1; 
        
        for (int i = 3; i > 0; i--)
        {
            if (zoneTransitionText != null)
            {
                zoneTransitionText.text = $"ZONE {displayZone}\nSTARTS IN {i}";
            }
            // We must use Realtime because Time.timeScale is currently 0!
            yield return new WaitForSecondsRealtime(1f); 
        }

        if (zoneTransitionText != null)
        {
            zoneTransitionText.text = "GO!";
        }

        if (inGameUI != null) inGameUI.SetActive(true);

        Time.timeScale = 1f; // Unfreeze the game!
        isTimerRunning = true; // Resume the background timer

        // Fade out the transition screen smoothly over 0.5 seconds
        if (zoneTransitionScreen != null)
        {
            CanvasGroup canvasGroup = zoneTransitionScreen.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = zoneTransitionScreen.AddComponent<CanvasGroup>();

            float fadeDuration = 0.5f;
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f; // Reset the alpha so it's opaque for the next zone's transition
            zoneTransitionScreen.SetActive(false);
        }
    }

    private void UpdateQuotaText(int currentKills, int targetQuota)
    {
        if (quotaText != null)
        {
            quotaText.text = $"KILLS: {currentKills} / {targetQuota}";
        }
    }

    private void ShowLevelCompletedMessage()
    {
        if (levelCompletedMessage != null)
        {
            StartCoroutine(LevelCompletedRoutine());
        }
    }

    private IEnumerator LevelCompletedRoutine()
    {
        levelCompletedMessage.SetActive(true);
        yield return new WaitForSeconds(2f);
        if (levelCompletedMessage != null) levelCompletedMessage.SetActive(false);
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }

    private void ShowDeathScreen()
    {
        isTimerRunning = false;
        Time.timeScale = 0f; // Freeze the game action
        
        if (inGameUI != null) inGameUI.SetActive(false);
        if (deathScreen != null) deathScreen.SetActive(true);
        
        if (AudioManager.Instance != null && AudioManager.Instance.gameOverSound != null)
        {
            AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.gameOverSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero, AudioManager.Instance.gameOverVolume, 1f);
        }

        if (deathTimeText != null) deathTimeText.text = $"TIME ALIVE: {FormatTime(currentRunTime)}";
        
        // Unlock the cursor so the player can click the Restart button
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ShowVictoryScreen()
    {
        isTimerRunning = false;
        Time.timeScale = 0f; // Freeze the game action
        
        if (inGameUI != null) inGameUI.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(true);

        if (victoryTimeText != null) victoryTimeText.text = $"FINAL TIME: {FormatTime(currentRunTime)}";
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // --- Button Methods ---
    
    public void RestartGame()
    {
        // Reset our runtime, time scale, and progress for the new run
        currentRunTime = 0f;
        Time.timeScale = 1f;
        QuotaManager.ResetProgression(); 
        
        // Send the player back to the interactive Main Menu Elevator hub
        SceneManager.LoadScene(0); 
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}