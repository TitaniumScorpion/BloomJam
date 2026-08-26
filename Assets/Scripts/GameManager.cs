using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject inGameUI;
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

    [Header("Start Fade")]
    [Tooltip("Full-screen black overlay shown at boot, then faded out")]
    public GameObject startFadeScreen;
    [Tooltip("Seconds to hold pitch black before the fade-out begins")]
    public float startFadeDelay = 1f;
    public float startFadeDuration = 3f;

    [Header("Zone Cleared UI")]
    public GameObject levelCompletedMessage;

    [Header("Single Scene Progression")]
    public GameObject[] zones;
    public GameObject playerObject;

    [Header("Elevator Hub")]
    [Tooltip("The ElevatorHub component in the scene")]
    public ElevatorHub elevatorHub;
    [Tooltip("Where the player stands inside the elevator hub")]
    public Transform elevatorHubSpawnPoint;
    [Tooltip("The front wall that disappears when a zone starts")]
    public GameObject hubFrontWall;
    [Tooltip("The Start/Advance panel that disappears alongside the front wall when a zone starts")]
    public GameObject hubFrontPanel;
    [Tooltip("Seconds the player stands free in the elevator before hub browsing auto-engages on the front panel")]
    [SerializeField] private float firstVisitAutoEnterDelay = 5f;

    [SerializeField] private float skyboxRotationSpeed = 1f;

    public static GameManager Instance;
    // False from boot until the player presses Start for the very first time — used to keep
    // the player frozen, weapons hidden/unusable, and F unable to back out of hub browsing
    // during the initial elevator sequence.
    public static bool HasGameStarted { get; private set; }
    private Coroutine transitionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        QuotaManager.OnKillCountUpdated += UpdateQuotaText;
        QuotaManager.OnZoneCleared += ShowLevelCompletedMessage;
        QuotaManager.OnGameCompleted += ShowVictoryScreen;
        PlayerHealth.OnPlayerDied += ShowDeathScreen;
    }

    private void OnDisable()
    {
        QuotaManager.OnKillCountUpdated -= UpdateQuotaText;
        QuotaManager.OnZoneCleared -= ShowLevelCompletedMessage;
        QuotaManager.OnGameCompleted -= ShowVictoryScreen;
        PlayerHealth.OnPlayerDied -= ShowDeathScreen;
    }

    private void Start()
    {
        if (deathScreen != null) deathScreen.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (levelCompletedMessage != null) levelCompletedMessage.SetActive(false);
        if (inGameUI != null) inGameUI.SetActive(false);

        // Disable all zones — player starts in the elevator hub
        foreach (GameObject zone in zones)
            if (zone != null) zone.SetActive(false);

        // Hide the front panel entirely for the initial wait — deactivating it (rather than just
        // gating its button) means nothing on it can receive hover/click events at all, whatever
        // components it has. Reactivated the moment hub browsing actually focuses it below.
        if (hubFrontPanel != null) hubFrontPanel.SetActive(false);

        // Place player in the hub; hub browsing auto-engages after a short delay
        PlacePlayerInHub();
        StartCoroutine(AutoEnterHubModeAfterDelay());
        StartCoroutine(FadeInFromBlack());
    }

    private IEnumerator FadeInFromBlack()
    {
        if (startFadeScreen == null) yield break;

        startFadeScreen.SetActive(true);
        CanvasGroup canvasGroup = startFadeScreen.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = startFadeScreen.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        // Purely visual — never intercept clicks meant for the panel underneath it,
        // even while it's still visibly fading
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        yield return new WaitForSeconds(startFadeDelay);

        for (float t = 0; t < startFadeDuration; t += Time.deltaTime)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / startFadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        startFadeScreen.SetActive(false);
    }

    private IEnumerator AutoEnterHubModeAfterDelay()
    {
        yield return new WaitForSeconds(firstVisitAutoEnterDelay);
        if (hubFrontPanel != null) hubFrontPanel.SetActive(true);
        if (elevatorHub != null) elevatorHub.EnterHubMode(0);
    }

    private void Update()
    {
        if (isTimerRunning)
            currentRunTime += Time.deltaTime;

        if (RenderSettings.skybox != null)
            RenderSettings.skybox.SetFloat("_Rotation",
                RenderSettings.skybox.GetFloat("_Rotation") + skyboxRotationSpeed * Time.deltaTime);
    }

    // ── Called by ElevatorHub.OnStartPressed() ───────────────────────────────

    public void StartCurrentZone()
    {
        HasGameStarted = true;

        if (elevatorHub != null) elevatorHub.ForceExitHubMode();

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(ZoneTransitionRoutine());
    }

    // ── Called by ElevatorEntryTrigger when the player walks back into the elevator ──────
    // Closes the wall/panel and cleans up the finished zone. Doesn't engage hub browsing itself —
    // the player still walks up to a panel and presses F (via PanelInteractable) to do that.

    public void ReturnToElevatorHub()
    {
        if (ElevatorHub.IsActive) return; // Still in the initial hub mode, nothing to do

        isTimerRunning = false;

        // Disable all zones and clean up lingering enemies/projectiles
        foreach (GameObject zone in zones)
            if (zone != null) zone.SetActive(false);

        foreach (StandardSwarmer s in FindObjectsOfType<StandardSwarmer>()) s.gameObject.SetActive(false);
        foreach (AdvancedEnemy b in FindObjectsOfType<AdvancedEnemy>()) b.gameObject.SetActive(false);
        foreach (Projectile p in FindObjectsOfType<Projectile>()) p.gameObject.SetActive(false);
        foreach (EnemyProjectile ep in FindObjectsOfType<EnemyProjectile>()) ep.gameObject.SetActive(false);

        if (inGameUI != null) inGameUI.SetActive(false);

        if (hubFrontWall != null) hubFrontWall.SetActive(true);
        if (hubFrontPanel != null) hubFrontPanel.SetActive(true);
        if (elevatorHub != null) elevatorHub.RefreshButtonLabel();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void PlacePlayerInHub()
    {
        if (playerObject == null || elevatorHubSpawnPoint == null) return;

        Rigidbody rb = playerObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = elevatorHubSpawnPoint.position;
            rb.rotation = elevatorHubSpawnPoint.rotation;
        }
        playerObject.transform.position = elevatorHubSpawnPoint.position;
        playerObject.transform.rotation = elevatorHubSpawnPoint.rotation;
    }

    private IEnumerator ZoneTransitionRoutine()
    {
        isTimerRunning = false;
        Time.timeScale = 0f;

        if (inGameUI != null) inGameUI.SetActive(false);
        if (zoneTransitionScreen != null)
        {
            zoneTransitionScreen.SetActive(true);
            CanvasGroup cg = zoneTransitionScreen.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }

        // Enable current zone, disable others
        for (int i = 0; i < zones.Length; i++)
            if (zones[i] != null) zones[i].SetActive(i == QuotaManager.currentZoneIndex);

        // Open the hub front wall so the player can walk into the arena
        if (hubFrontWall != null) hubFrontWall.SetActive(false);
        if (hubFrontPanel != null) hubFrontPanel.SetActive(false);

        // Reset kills for this zone
        QuotaManager qm = FindObjectOfType<QuotaManager>();
        if (qm != null) qm.StartNextZone();

        // Clean up anything left from the hub or previous zone
        foreach (Projectile p in FindObjectsOfType<Projectile>()) p.gameObject.SetActive(false);
        foreach (EnemyProjectile ep in FindObjectsOfType<EnemyProjectile>()) ep.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        int displayZone = QuotaManager.currentZoneIndex + 1;
        for (int i = 3; i > 0; i--)
        {
            if (zoneTransitionText != null) zoneTransitionText.text = $"ZONE {displayZone}\nSTARTS IN {i}";
            yield return new WaitForSecondsRealtime(1f);
        }

        if (zoneTransitionText != null) zoneTransitionText.text = "GO!";
        if (inGameUI != null) inGameUI.SetActive(true);

        Time.timeScale = 1f;
        isTimerRunning = true;

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
            canvasGroup.alpha = 1f;
            zoneTransitionScreen.SetActive(false);
        }
    }

    private void UpdateQuotaText(int currentKills, int targetQuota)
    {
        if (quotaText != null) quotaText.text = $"KILLS: {currentKills} / {targetQuota}";
    }

    private void ShowLevelCompletedMessage()
    {
        if (levelCompletedMessage != null)
            StartCoroutine(LevelCompletedRoutine());
    }

    private IEnumerator LevelCompletedRoutine()
    {
        levelCompletedMessage.SetActive(true);
        yield return new WaitForSeconds(2f);
        if (levelCompletedMessage != null) levelCompletedMessage.SetActive(false);
    }

    private void ShowDeathScreen()
    {
        isTimerRunning = false;
        Time.timeScale = 0f;
        if (inGameUI != null) inGameUI.SetActive(false);
        if (deathScreen != null) deathScreen.SetActive(true);
        if (deathTimeText != null) deathTimeText.text = $"TIME ALIVE: {FormatTime(currentRunTime)}";
        if (AudioManager.Instance != null && AudioManager.Instance.gameOverSound != null)
            AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.gameOverSound,
                Camera.main != null ? Camera.main.transform.position : Vector3.zero,
                AudioManager.Instance.gameOverVolume, 1f);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ShowVictoryScreen()
    {
        isTimerRunning = false;
        Time.timeScale = 0f;
        if (inGameUI != null) inGameUI.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(true);
        if (victoryTimeText != null) victoryTimeText.text = $"FINAL TIME: {FormatTime(currentRunTime)}";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }

    // ── Button methods ────────────────────────────────────────────────────────

    public void RestartGame()
    {
        currentRunTime = 0f;
        Time.timeScale = 1f;
        QuotaManager.ResetProgression();
        UpgradeManager.ResetPendingUpgrade();
        HasGameStarted = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
