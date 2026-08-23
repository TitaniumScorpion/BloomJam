using UnityEngine;
using UnityEngine.InputSystem;

// Place this component on the elevator hub GameObject in your main scene.
// Assign the front-panel Start button's OnClick to ElevatorHub.OnStartPressed().
public class ElevatorHub : MonoBehaviour
{
    public static bool IsActive { get; private set; }

    [Header("References")]
    [Tooltip("The PlayerController on the player GameObject")]
    public PlayerController playerController;

    [Header("Panel Rotation")]
    [Tooltip("Starting Y angle that faces the front (Start) panel in world space")]
    public float frontPanelAngle = 0f;
    public float rotationSpeed = 10f;

    private float targetAngle;
    private float smoothAngle;

    private void OnEnable()
    {
        IsActive = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Snap to front panel immediately on (re)activation
        targetAngle = frontPanelAngle;
        smoothAngle = frontPanelAngle;
        playerController?.SetYRotation(frontPanelAngle);
    }

    private void OnDisable()
    {
        IsActive = false;
    }

    private void Update()
    {
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

    // Wire this to the Start button's OnClick event in the Inspector
    public void OnStartPressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartCurrentZone();
    }
}
