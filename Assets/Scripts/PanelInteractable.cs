using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Place one of these on a trigger collider positioned right around each of the elevator's
// 4 panels. Walking into range and pressing F tells ElevatorHub to enter hub browsing mode
// focused on this specific panel (index must match its slot in ElevatorHub.panels).
[RequireComponent(typeof(Collider))]
public class PanelInteractable : MonoBehaviour
{
    [Tooltip("The ElevatorHub in the scene")]
    public ElevatorHub elevatorHub;
    [Tooltip("This panel's index in ElevatorHub.panels")]
    public int panelIndex;
    [Tooltip("UI prompt shown while the player is in range. Leave unassigned to auto-create a 'PRESS F' label at runtime.")]
    public GameObject interactPrompt;

    private bool playerInRange;

    private void Start()
    {
        if (interactPrompt == null)
            CreatePrompt();
    }

    private void CreatePrompt()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
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
        // Already browsing (manually entered, or the game-start auto-intro) — this trigger has nothing to do
        if (ElevatorHub.IsActive) return;

        if (!IsPanelAvailable())
        {
            if (interactPrompt != null) interactPrompt.SetActive(false);
            return;
        }

        if (playerInRange && interactPrompt != null && !interactPrompt.activeSelf)
            interactPrompt.SetActive(true);

        if (!playerInRange) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (interactPrompt != null) interactPrompt.SetActive(false);
            elevatorHub?.EnterHubMode(panelIndex);
        }
    }
}
