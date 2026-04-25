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

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Ensure the bomb is visible at start
        if (displayBomb != null)
            displayBomb.SetActive(true);
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