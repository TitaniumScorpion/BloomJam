using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class KatanaWeapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float attackRange = 3.5f;
    public float attackRadius = 2f; // Creates a thick cylinder cast for area-of-effect
    public int damage = 5; // Enough to kill several standard swarmers or heavily damage a boss
    public float cooldownTime = 0.8f;
    private float cooldownTimer;

    [Header("References")]
    public GameObject displayWeapon;
    public Transform cameraTransform;

    [Header("Tilt Settings")]
    public float tiltAmount = 5f;

    [Header("Sway Settings")]
    public float swayAmount = 0.05f;
    public float swaySpeed = 8f;
    private Vector3 initialDisplayPosition;

    [Header("Bob Settings")]
    public float bobSpeed = 14f;
    public float bobAmount = 0.05f;
    private float bobTimer;

    [Header("Swing Animation")]
    public float swingSpeed = 15f;
    public float swingDuration = 0.35f;
    public Vector3 swingRotationOffset = new Vector3(10f, 100f, -40f); 
    private Quaternion initialDisplayRotation;
    private Quaternion targetSwingRotation;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (displayWeapon != null)
        {
            initialDisplayRotation = displayWeapon.transform.localRotation;
            initialDisplayPosition = displayWeapon.transform.localPosition;
            targetSwingRotation = initialDisplayRotation;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Right click to swing the Katana
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (cooldownTimer <= 0f)
            {
                Attack();
            }
        }

        HandleWeaponSwayAndSwing();
    }

    private void Attack()
    {
        cooldownTimer = cooldownTime;

        // Trigger Visual Swing
        if (displayWeapon != null)
        {
            StartCoroutine(SwingRoutine());
        }

        // Hit Detection using a thick SphereCast to simulate a wide horizontal slash
        if (cameraTransform != null)
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit[] hits = Physics.SphereCastAll(ray, attackRadius, attackRange);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.TryGetComponent(out StandardSwarmer swarmer))
                {
                    swarmer.TakeDamage(damage);
                }
                else if (hit.collider.TryGetComponent(out EnemyWeakPoint weakPoint))
                {
                    weakPoint.TakeDamage(damage);
                }
            }
        }
    }

    private IEnumerator SwingRoutine()
    {
        // Swing the blade out fast
        targetSwingRotation = initialDisplayRotation * Quaternion.Euler(swingRotationOffset);
        yield return new WaitForSeconds(swingDuration);
        // Smoothly retract back to idle position
        targetSwingRotation = initialDisplayRotation;
    }

    private void HandleWeaponSwayAndSwing()
    {
        if (displayWeapon != null && displayWeapon.activeSelf)
        {
            float moveX = 0f;
            float moveY = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.dKey.isPressed) moveX += 1f;
                if (Keyboard.current.aKey.isPressed) moveX -= 1f;
                if (Keyboard.current.wKey.isPressed) moveY += 1f;
                if (Keyboard.current.sKey.isPressed) moveY -= 1f;
            }

            // Calculate bobbing
            float speedMagnitude = Mathf.Clamp01(Mathf.Abs(moveX) + Mathf.Abs(moveY));
            if (speedMagnitude > 0.1f) bobTimer += Time.deltaTime * bobSpeed;

            Vector3 bobOffset = new Vector3(Mathf.Cos(bobTimer * 0.5f) * (bobAmount * 0.5f), Mathf.Sin(bobTimer) * bobAmount, 0f) * speedMagnitude;

            // Apply positional sway and bob
            Vector3 targetPosition = initialDisplayPosition + new Vector3(-moveX * swayAmount, -moveY * swayAmount, 0f) + bobOffset;
            displayWeapon.transform.localPosition = Vector3.Lerp(displayWeapon.transform.localPosition, targetPosition, Time.deltaTime * swaySpeed);

            // Combine movement tilt with the swinging rotation
            Quaternion tiltRotation = Quaternion.Euler(moveY * tiltAmount, 0f, -moveX * tiltAmount);
            displayWeapon.transform.localRotation = Quaternion.Lerp(displayWeapon.transform.localRotation, targetSwingRotation * tiltRotation, Time.deltaTime * swingSpeed);
        }
    }
}