using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;

// Place this component on the elevator hub GameObject in your main scene.
// Assign the front-panel Start/Advance button's OnClick to ElevatorHub.OnStartPressed().
// This GameObject (and the elevator structure around it) should stay active at all times —
// hub mode is toggled via EnterHubMode()/ExitHubMode(), not by enabling/disabling the object.
//
// While active, the player is frozen (PlayerController skips Update/FixedUpdate whenever
// ElevatorHub.IsActive) and the camera is driven directly by this script: it dollies/rotates
// toward whichever panel's focusPoint is currently selected. A/D (or clicking the left/right
// half of the screen, outside any UI) cycles the focused panel with the same transition.
public class ElevatorHub : MonoBehaviour
{
    public static bool IsActive { get; private set; }
    public static event Action OnHubModeEnter;
    public static event Action OnHubModeExit;
    // Fired whenever the focused panel index changes (including the initial focus on entry)
    public static event Action<int> OnPanelFocusChanged;

    [System.Serializable]
    public class HubPanelSlot
    {
        public string label;
        [Tooltip("World-space position+rotation the camera dollies/rotates to when this panel is focused")]
        public Transform focusPoint;
    }

    [Header("References")]
    [Tooltip("The PlayerController on the player GameObject")]
    public PlayerController playerController;
    [Tooltip("The TMP_Text label on the front panel's Start/Advance button")]
    public TMP_Text startButtonLabel;

    [Header("Panels")]
    [Tooltip("Order defines A/D cycling. Index 0 is used as the default focus on the very first hub visit.")]
    public HubPanelSlot[] panels;

    [Header("Transition")]
    public float transitionSpeed = 4f;

    private int currentPanelIndex;
    private Vector3 restLocalPosition;
    private Quaternion restLocalRotation;
    private Coroutine exitRoutine;
    // Guards against the same F-press that triggered EnterHubMode() (from PanelInteractable)
    // also being read as an exit signal later in the same frame — Unity doesn't guarantee
    // Update() order between different components.
    private int enterFrame = -1;

    // Call when the player should enter hub browsing — initial game start (after its delay),
    // or walking up to any panel and pressing F after a zone clear.
    public void EnterHubMode(int startPanelIndex = 0)
    {
        if (panels == null || panels.Length == 0) return;

        if (exitRoutine != null)
        {
            StopCoroutine(exitRoutine);
            exitRoutine = null;
        }

        IsActive = true;
        enterFrame = Time.frameCount;
        currentPanelIndex = Mathf.Clamp(startPanelIndex, 0, panels.Length - 1);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerController != null && playerController.cameraTransform != null)
        {
            restLocalPosition = playerController.cameraTransform.localPosition;
            restLocalRotation = playerController.cameraTransform.localRotation;
        }

        RefreshButtonLabel();
        OnHubModeEnter?.Invoke();
        OnPanelFocusChanged?.Invoke(currentPanelIndex);
    }

    // Call when the player backs out of hub browsing (F pressed again). Runs a smooth
    // return-to-eye-position transition before actually handing control back, so there's no snap.
    public void ExitHubMode()
    {
        if (!IsActive || exitRoutine != null) return;
        exitRoutine = StartCoroutine(ExitRoutine());
    }

    private IEnumerator ExitRoutine()
    {
        Transform cam = playerController != null ? playerController.cameraTransform : null;

        if (cam != null)
        {
            while (Vector3.Distance(cam.localPosition, restLocalPosition) > 0.01f ||
                   Quaternion.Angle(cam.localRotation, restLocalRotation) > 0.5f)
            {
                cam.localPosition = Vector3.Lerp(cam.localPosition, restLocalPosition, Time.deltaTime * transitionSpeed);
                cam.localRotation = Quaternion.Slerp(cam.localRotation, restLocalRotation, Time.deltaTime * transitionSpeed);
                yield return null;
            }
            cam.localPosition = restLocalPosition;
            cam.localRotation = restLocalRotation;

            Vector3 euler = restLocalRotation.eulerAngles;
            float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            playerController.SyncRotation(pitch, euler.y);
        }

        IsActive = false;
        exitRoutine = null;
        OnHubModeExit?.Invoke();
    }

    // Immediately exits hub mode with no transition — used when actually starting a zone via
    // StartCurrentZone(), since the zone-transition screen (and Time.timeScale = 0) covers the
    // snap anyway. ExitHubMode()'s smooth coroutine would otherwise stall forever once timeScale
    // hits 0, because it relies on Time.deltaTime to animate.
    public void ForceExitHubMode()
    {
        if (exitRoutine != null)
        {
            StopCoroutine(exitRoutine);
            exitRoutine = null;
        }

        if (playerController != null && playerController.cameraTransform != null)
        {
            Transform cam = playerController.cameraTransform;
            cam.localPosition = restLocalPosition;
            cam.localRotation = restLocalRotation;

            Vector3 euler = restLocalRotation.eulerAngles;
            float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            playerController.SyncRotation(pitch, euler.y);
        }

        IsActive = false;
        OnHubModeExit?.Invoke();
    }

    // Updates the Start/Advance label based on progression — safe to call whether or not hub mode is active
    public void RefreshButtonLabel()
    {
        if (startButtonLabel != null)
            startButtonLabel.text = QuotaManager.currentZoneIndex > 0 ? "ADVANCE" : "START";
    }

    private void Update()
    {
        if (!IsActive || exitRoutine != null) return;

        HandlePanelCycling();
        UpdateCameraTransition();

        // Before the game has ever started, F can't back the player out of hub browsing —
        // there's nothing to back out to yet (weapons hidden, movement frozen either way)
        if (GameManager.HasGameStarted && Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame && Time.frameCount != enterFrame)
            ExitHubMode();
    }

    private void HandlePanelCycling()
    {
        if (panels == null || panels.Length == 0) return;

        int newIndex = currentPanelIndex;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                newIndex = (currentPanelIndex + 1) % panels.Length;
            if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                newIndex = (currentPanelIndex - 1 + panels.Length) % panels.Length;
        }

        if (newIndex == currentPanelIndex && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Ignore clicks that landed on a UI element (button) — let that button's own OnClick handle it
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!overUI)
            {
                float x = Mouse.current.position.ReadValue().x;
                newIndex = x < Screen.width * 0.5f
                    ? (currentPanelIndex + 1) % panels.Length
                    : (currentPanelIndex - 1 + panels.Length) % panels.Length;
            }
        }

        if (newIndex != currentPanelIndex)
        {
            currentPanelIndex = newIndex;
            OnPanelFocusChanged?.Invoke(currentPanelIndex);
        }
    }

    private void UpdateCameraTransition()
    {
        if (playerController == null || playerController.cameraTransform == null) return;
        if (panels == null || currentPanelIndex >= panels.Length) return;

        Transform target = panels[currentPanelIndex].focusPoint;
        if (target == null) return;

        Transform cam = playerController.cameraTransform;
        cam.position = Vector3.Lerp(cam.position, target.position, Time.deltaTime * transitionSpeed);
        cam.rotation = Quaternion.Slerp(cam.rotation, target.rotation, Time.deltaTime * transitionSpeed);
    }

    // Wire this to the Start/Advance button's OnClick event in the Inspector
    public void OnStartPressed()
    {
        // Guards against clicking the button before hub mode has actually engaged (e.g. during
        // the initial 5-second wait, if the panel happens to be visible/reachable on screen already)
        if (!IsActive) return;

        // Can't advance with an upgrade choice still sitting unspent on the left panel
        if (UpgradeManager.HasPendingUpgrade) return;

        if (GameManager.Instance != null)
            GameManager.Instance.StartCurrentZone();
    }
}
