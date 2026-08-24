using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System;

// Place this component on the elevator hub GameObject in your main scene.
// Assign the front-panel Start/Advance button's OnClick to ElevatorHub.OnStartPressed().
// This GameObject (and the elevator structure around it) should stay active at all times —
// hub mode is toggled via EnterHubMode()/ExitHubMode(), not by enabling/disabling the object.
public class ElevatorHub : MonoBehaviour
{
    public static bool IsActive { get; private set; }
    public static event Action OnHubModeEnter;
    public static event Action OnHubModeExit;

    [Header("References")]
    [Tooltip("The PlayerController on the player GameObject")]
    public PlayerController playerController;
    [Tooltip("The TMP_Text label on the front panel's Start/Advance button")]
    public TMP_Text startButtonLabel;

    [Header("Panel Rotation")]
    [Tooltip("Starting Y angle that faces the front (Start) panel in world space")]
    public float frontPanelAngle = 0f;
    public float rotationSpeed = 10f;

    private float targetAngle;
    private float smoothAngle;

    // Call when the player should regain control of the panel — initial game start,
    // or after walking back into the elevator following a zone clear.
    public void EnterHubMode()
    {
        IsActive = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Snap to front panel immediately
        targetAngle = frontPanelAngle;
        smoothAngle = frontPanelAngle;
        playerController?.SetYRotation(frontPanelAngle);

        RefreshButtonLabel();
        OnHubModeEnter?.Invoke();
    }

    // Call when the player leaves hub mode to go play the zone
    public void ExitHubMode()
    {
        IsActive = false;
        OnHubModeExit?.Invoke();
    }

    // Updates the Start/Advance label without engaging full hub mode —
    // used for the between-zones return, where the player keeps free movement/look
    // and interacts with the panel via PanelInteractable (walk close, press E) instead.
    public void RefreshButtonLabel()
    {
        if (startButtonLabel != null)
            startButtonLabel.text = QuotaManager.currentZoneIndex > 0 ? "ADVANCE" : "START";
    }

    private void Update()
    {
        if (!IsActive) return;

        HandlePanelInput();
        SmoothCamera();
    }

    private void HandlePanelInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            targetAngle -= 90f;
        if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            targetAngle += 90f;
    }

    private void SmoothCamera()
    {
        if (playerController == null) return;
        smoothAngle = Mathf.LerpAngle(smoothAngle, targetAngle, Time.deltaTime * rotationSpeed);
        playerController.SetYRotation(smoothAngle);
    }

    // Wire this to the Start/Advance button's OnClick event in the Inspector
    public void OnStartPressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartCurrentZone();
    }
}
