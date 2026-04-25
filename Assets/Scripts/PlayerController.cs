using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 12f;
    public float groundDrag = 6f;
    public float airMultiplier = 0.4f;

    [Header("Jumping")]
    public float jumpForce = 8f;
    public float extraGravity = 30f;
    public float jumpCooldown = 0.25f;
    private bool readyToJump = true;

    [Header("Dashing")]
    public float dashForce = 35f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.5f;
    private bool isDashing;
    private float dashCooldownTimer;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    private bool grounded;

    [Header("Camera Look")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    private float xRotation = 0f;

    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private Rigidbody rb;

    [Header("Camera Effects")]
    public Camera playerCamera;
    public float normalFOV = 90f;
    public float dashFOV = 110f;
    public float fovChangeSpeed = 10f;
    public float bobSpeed = 14f;
    public float bobAmount = 0.05f;
    public float tiltAngle = 3f;
    public float tiltSpeed = 6f;
    private float zRotation = 0f;
    private float defaultCameraY;
    private float timer = 0;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Prevent the physics engine from tipping the player over

        // Lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null && cameraTransform != null)
            playerCamera = cameraTransform.GetComponent<Camera>();
            
        if (playerCamera != null)
            normalFOV = playerCamera.fieldOfView;

        if (cameraTransform != null)
            defaultCameraY = cameraTransform.localPosition.y;
    }

    private void Update()
    {
        // Completely freeze the player's inputs and camera movement during the countdown
        if (Time.timeScale == 0f) return;

        // Ground check using a simple Raycast downward
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        HandleLook();
        HandleHeadBob();
        HandleDashFOV();
        SpeedControl();

        // Handle physics drag when grounded so movement feels snappy and doesn't slide like ice
        if (grounded && !isDashing)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;

        // Manage the dash cooldown timer
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        // We only move normally if we aren't currently locked into a dash animation/movement
        if (!isDashing)
        {
            MovePlayer();
        }

        // Apply extra gravity to make jump speed (time in air) shorter
        if (!grounded)
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }

    private void MyInput()
    {
        horizontalInput = 0f;
        verticalInput = 0f;
        bool jumpPressed = false;
        bool dashPressed = false;

        if (Keyboard.current != null)
        {
            // Gather snappy, non-smoothed inputs manually from the new Input System
            if (Keyboard.current.dKey.isPressed) horizontalInput += 1f;
            if (Keyboard.current.aKey.isPressed) horizontalInput -= 1f;
            if (Keyboard.current.wKey.isPressed) verticalInput += 1f;
            if (Keyboard.current.sKey.isPressed) verticalInput -= 1f;

            jumpPressed = Keyboard.current.spaceKey.isPressed;
            dashPressed = Keyboard.current.leftShiftKey.wasPressedThisFrame;
        }

        // Jump
        if (jumpPressed && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown); // Prevent spamming jump every frame
        }

        // Dash
        if (dashPressed && dashCooldownTimer <= 0f && !isDashing)
        {
            Dash();
        }
    }

    private void HandleLook()
    {
        if (cameraTransform == null) return;

        float mouseX = 0f;
        float mouseY = 0f;

        if (Mouse.current != null)
        {
            // The new Input System returns raw pixel delta for the mouse. 
            // We scale it down slightly so your current mouseSensitivity value still feels normal.
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            mouseX = mouseDelta.x * mouseSensitivity * 0.05f;
            mouseY = mouseDelta.y * mouseSensitivity * 0.05f;
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Prevent looking past straight up or down

        // Calculate target Z rotation (camera tilt) based on horizontal movement
        float targetZ = -horizontalInput * tiltAngle;
        zRotation = Mathf.Lerp(zRotation, targetZ, Time.deltaTime * tiltSpeed);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, zRotation);
        transform.rotation *= Quaternion.Euler(0f, mouseX, 0f);
    }

    private void MovePlayer()
    {
        // Calculate movement direction based on where the player is currently looking
        moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        // Apply force on the ground
        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        // Apply reduced force in the air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
    }

    private void SpeedControl()
    {
        if (isDashing) return;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Limit the velocity if it exceeds the maximum move speed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // Reset the Y velocity entirely before jumping so we always jump the exact same height
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Dash()
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;

        // Determine which direction we are pressing keys. If standing still, default to forward dash.
        Vector3 dashDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward;

        // Temporarily reset velocity so the dash applies purely and consistently
        rb.linearVelocity = new Vector3(0f, 0f, 0f);
        rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);

        // Stop the dash after 'dashDuration' seconds
        Invoke(nameof(EndDash), dashDuration);
    }

    private void EndDash()
    {
        isDashing = false;
        // Significantly reduce velocity immediately after a dash so the player doesn't slide uncontrollably
        rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y, rb.linearVelocity.z * 0.3f);
    }

    private void HandleHeadBob()
    {
        if (cameraTransform == null) return;

        // Only bob if moving and on the ground
        if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
        {
            if (grounded)
            {
                timer += Time.deltaTime * bobSpeed;
                cameraTransform.localPosition = new Vector3(
                    cameraTransform.localPosition.x,
                    defaultCameraY + Mathf.Sin(timer) * bobAmount,
                    cameraTransform.localPosition.z);
            }
        }
        else
        {
            // Reset to default smoothly
            timer = 0;
            cameraTransform.localPosition = new Vector3(
                cameraTransform.localPosition.x,
                Mathf.Lerp(cameraTransform.localPosition.y, defaultCameraY, Time.deltaTime * bobSpeed),
                cameraTransform.localPosition.z);
        }
    }

    private void HandleDashFOV()
    {
        if (playerCamera == null) return;

        float targetFOV = isDashing ? dashFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovChangeSpeed);
    }
}