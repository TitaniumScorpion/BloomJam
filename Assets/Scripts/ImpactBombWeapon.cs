using UnityEngine;
using UnityEngine.InputSystem;

public class ImpactBombWeapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float cooldownTime = 2f; // Time before you can throw another bomb
    private float cooldownTimer;

    [Header("References")]
    public string bombPoolTag = "ImpactBomb";
    public Transform bombPoint;
    public GameObject displayBomb; // The visual bomb in the player's hand
    public Camera playerCamera;

    [Header("Tilt Settings")]
    public float tiltAmount = 5f;
    public float tiltSpeed = 8f;
    private Quaternion initialDisplayRotation;

    [Header("Sway Settings")]
    public float swayAmount = 0.05f;
    public float swaySpeed = 8f;
    private Vector3 initialDisplayPosition;

    [Header("Bob Settings")]
    public float bobSpeed = 14f;
    public float bobAmount = 0.05f;
    private float bobTimer;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Ensure the bomb is visible at start
        if (displayBomb != null)
        {
            displayBomb.SetActive(true);
            initialDisplayRotation = displayBomb.transform.localRotation;
            initialDisplayPosition = displayBomb.transform.localPosition;
        }
    }

    private void Update()
    {
        // Don't process input or cooldowns if the game is paused
        if (Time.timeScale == 0f) return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;

            // Reactivate the display bomb once the cooldown finishes
            if (cooldownTimer <= 0f && displayBomb != null)
            {
                displayBomb.SetActive(true);
            }
        }

        // Listen for Right Mouse Button to throw
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (cooldownTimer <= 0f)
            {
                ThrowBomb();
            }
        }

        HandleWeaponTilt();
    }

    private void HandleWeaponTilt()
    {
        if (displayBomb != null && displayBomb.activeSelf)
        {
            float moveX = 0f;
            float moveY = 0f;

            if (Keyboard.current != null)
            {
                // Read raw keyboard input for movement
                if (Keyboard.current.dKey.isPressed) moveX += 1f;
                if (Keyboard.current.aKey.isPressed) moveX -= 1f;
                if (Keyboard.current.wKey.isPressed) moveY += 1f;
                if (Keyboard.current.sKey.isPressed) moveY -= 1f;
            }

            // Tilt on the Z axis (roll) when strafing left/right
            // Tilt on the X axis (pitch) when moving forward/backward
            Quaternion targetRotation = initialDisplayRotation * Quaternion.Euler(moveY * tiltAmount, 0f, -moveX * tiltAmount);
            displayBomb.transform.localRotation = Quaternion.Lerp(displayBomb.transform.localRotation, targetRotation, Time.deltaTime * tiltSpeed);

            // Calculate continuous bobbing (Figure-8 pattern)
            float speedMagnitude = Mathf.Clamp01(Mathf.Abs(moveX) + Mathf.Abs(moveY));
            if (speedMagnitude > 0.1f)
            {
                bobTimer += Time.deltaTime * bobSpeed;
            }

            // Sin handles the Y (up/down), Cos handles the X (left/right at half speed for the figure-8 shape)
            Vector3 bobOffset = new Vector3(
                Mathf.Cos(bobTimer * 0.5f) * (bobAmount * 0.5f), 
                Mathf.Sin(bobTimer) * bobAmount, 
                0f
            ) * speedMagnitude;

            // Positional sway: lags behind movement to create a sense of weight
            // -moveX makes it sway opposite to strafing, -moveY pushes it down when moving forward
            Vector3 targetPosition = initialDisplayPosition + new Vector3(-moveX * swayAmount, -moveY * swayAmount, 0f) + bobOffset;
            displayBomb.transform.localPosition = Vector3.Lerp(displayBomb.transform.localPosition, targetPosition, Time.deltaTime * swaySpeed);
        }
    }

    private void ThrowBomb()
    {
        cooldownTimer = cooldownTime;

        // Hide the bomb in the player's hand
        if (displayBomb != null)
        {
            displayBomb.SetActive(false);
        }

        if (bombPoint != null && playerCamera != null)
        {
            // Aim exactly where the center of the screen is looking
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 targetPoint;

            if (Physics.Raycast(ray, out RaycastHit hit))
                targetPoint = hit.point;
            else
                targetPoint = ray.GetPoint(100f);

            // Calculate the rotation from the fire point to the target
            Vector3 direction = targetPoint - bombPoint.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            ObjectPooler.Instance.SpawnFromPool(bombPoolTag, bombPoint.position, targetRotation);
        }
    }
}