using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System;

// Place this on a trigger collider positioned right around the elevator's front panel.
// Used for the BETWEEN-ZONES return only — the very first hub visit still uses ElevatorHub's
// full auto hub mode. Here the player keeps free movement, walks up, and presses F to unlock
// the cursor so they can click the panel's Start/Advance button; movement stays active the
// whole time, only mouse-look pauses while interacting.
[RequireComponent(typeof(Collider))]
public class PanelInteractable : MonoBehaviour
{
    [Tooltip("The PlayerController on the player GameObject, used to pause mouse-look while interacting")]
    public PlayerController playerController;
    [Tooltip("UI prompt shown while the player is in range. Leave unassigned to auto-create a 'PRESS F' label at runtime.")]
    public GameObject interactPrompt;

    // Static so weapons (AutomaticPistol, KatanaWeapon) can react without needing a reference
    public static bool IsInteracting { get; private set; }
    public static event Action OnInteractStart;
    public static event Action OnInteractEnd;

    private bool playerInRange;

    private void Start()
    {
        if (interactPrompt == null)
            CreatePrompt();
    }

    private void CreatePrompt()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        GameObject promptObj = new GameObject("PressFPrompt");
        promptObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = promptObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -100f); // Below crosshair
        rt.sizeDelta = new Vector2(300f, 40f);

        TMP_Text text = promptObj.AddComponent<TextMeshProUGUI>();
        text.text = "PRESS F";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 28f;
        text.color = Color.white;

        interactPrompt = promptObj;
        interactPrompt.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (IsInteracting) StopInteracting();
    }

    // Only interactable once the panel has actually reappeared (player walked back into the
    // elevator); while a zone is active and the panel is hidden, this returns false. If
    // GameManager.hubFrontPanel isn't assigned, gating is skipped so nothing regresses.
    private bool IsPanelAvailable()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.hubFrontPanel == null) return true;
        return gm.hubFrontPanel.activeInHierarchy;
    }

    private void Update()
    {
        // The very first hub visit is handled entirely by ElevatorHub's own auto hub mode
        if (ElevatorHub.IsActive) return;

        if (!IsPanelAvailable())
        {
            if (IsInteracting) StopInteracting();
            if (interactPrompt != null) interactPrompt.SetActive(false);
            return;
        }

        if (playerInRange && !IsInteracting && interactPrompt != null && !interactPrompt.activeSelf)
            interactPrompt.SetActive(true);

        if (!playerInRange) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (IsInteracting) StopInteracting();
            else StartInteracting();
        }
    }

    private void StartInteracting()
    {
        IsInteracting = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        playerController?.SetLookLocked(true);
        if (interactPrompt != null) interactPrompt.SetActive(false);
        OnInteractStart?.Invoke();
    }

    public void StopInteracting()
    {
        IsInteracting = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerController?.SetLookLocked(false);
        if (playerInRange && interactPrompt != null) interactPrompt.SetActive(true);
        OnInteractEnd?.Invoke();
    }
}
