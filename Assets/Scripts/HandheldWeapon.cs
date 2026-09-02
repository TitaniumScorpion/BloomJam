using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shared behaviour for the player's held weapons: view-model sway/bob/tilt, show/hide
/// around elevator-hub and bullet-time transitions, and the "is the player actually in
/// control right now" gate.
///
/// Each weapon keeps its own combat logic and its own rotation-lerp speed (the pistol's
/// tiltSpeed, the katana's swingSpeed), which is why ApplySway takes that as a parameter
/// rather than owning the field.
///
/// Field names are unchanged from when they lived on AutomaticPistol/KatanaWeapon, so
/// existing Inspector values deserialize onto this class untouched.
/// </summary>
public abstract class HandheldWeapon : MonoBehaviour
{
    [Header("Display")]
    [Tooltip("The visible view-model. Hidden during hub browsing and (for the pistol) bullet time.")]
    public GameObject displayWeapon;

    [Header("Sway Settings")]
    public float swayAmount = 0.05f;
    public float swaySpeed = 8f;

    [Header("Bob Settings")]
    public float bobSpeed = 14f;
    public float bobAmount = 0.05f;

    [Header("Tilt Settings")]
    [Tooltip("Degrees the weapon banks when strafing or moving forward/back.")]
    public float tiltAmount = 5f;

    protected Quaternion initialDisplayRotation = Quaternion.identity;
    protected Vector3 initialDisplayPosition;
    private float bobTimer;

    protected virtual void Awake()
    {
        // Cached before anything can move the view-model, so sway always returns to rest
        if (displayWeapon != null)
        {
            initialDisplayRotation = displayWeapon.transform.localRotation;
            initialDisplayPosition = displayWeapon.transform.localPosition;
        }
    }

    /// <summary>
    /// Derived classes should call this LAST in their own Start — it applies the pre-game
    /// hide, and anything that needs the view-model active must run before it.
    /// </summary>
    protected virtual void Start()
    {
        // Stay hidden until the player actually presses Start — ElevatorHub.OnHubModeExit
        // (fired from GameManager.StartCurrentZone) shows it again from there on
        if (!GameManager.HasGameStarted) HideWeapon();
    }

    protected virtual void OnEnable()
    {
        ElevatorHub.OnHubModeEnter += HideWeapon;
        ElevatorHub.OnHubModeExit += ShowWeapon;
    }

    protected virtual void OnDisable()
    {
        ElevatorHub.OnHubModeEnter -= HideWeapon;
        ElevatorHub.OnHubModeExit -= ShowWeapon;
    }

    protected void HideWeapon() { if (displayWeapon != null) displayWeapon.SetActive(false); }
    protected void ShowWeapon() { if (displayWeapon != null) displayWeapon.SetActive(true); }

    /// <summary>
    /// False whenever the player has no control: the zone countdown (timeScale 0), before
    /// the run has started, or while browsing the elevator hub. Overridden by the pistol,
    /// which is also unusable during bullet time.
    /// </summary>
    protected virtual bool CanAct()
    {
        if (Time.timeScale == 0f) return false;
        if (!GameManager.HasGameStarted) return false;
        if (ElevatorHub.IsActive) return false;
        return true;
    }

    /// <summary>Raw WASD as a direction, read straight from the device for snappy, unsmoothed input.</summary>
    protected static Vector2 ReadMoveInput()
    {
        if (Keyboard.current == null) return Vector2.zero;

        Vector2 move = Vector2.zero;
        if (Keyboard.current.dKey.isPressed) move.x += 1f;
        if (Keyboard.current.aKey.isPressed) move.x -= 1f;
        if (Keyboard.current.wKey.isPressed) move.y += 1f;
        if (Keyboard.current.sKey.isPressed) move.y -= 1f;
        return move;
    }

    /// <summary>
    /// Drives the view-model's idle motion.
    /// </summary>
    /// <param name="baseRotation">Rest rotation to sway around — the katana passes its animated swing target.</param>
    /// <param name="rotationLerpSpeed">How fast rotation chases the target (pistol: tiltSpeed, katana: swingSpeed).</param>
    /// <param name="postRotation">Applied after tilt, before the lerp. The pistol's recoil kick.</param>
    /// <param name="positionOffset">Added to the target position. The pistol's recoil kickback.</param>
    /// <param name="positionShake">Added after the lerp, so it reads as jitter rather than a destination.</param>
    /// <param name="rotationShake">Multiplied in after the lerp, for the same reason.</param>
    protected void ApplySway(
        Quaternion baseRotation,
        float rotationLerpSpeed,
        Quaternion? postRotation = null,
        Vector3 positionOffset = default,
        Vector3 positionShake = default,
        Quaternion? rotationShake = null)
    {
        if (displayWeapon == null || !displayWeapon.activeSelf) return;

        // Nullable rather than `= default`: default(Quaternion) is all-zero, which would
        // collapse the rotation — and Unity's Quaternion == compares by dot product, so
        // `postRotation == default` is false even when it IS default. Nullable has no such trap.
        Quaternion post = postRotation ?? Quaternion.identity;
        Quaternion rotShake = rotationShake ?? Quaternion.identity;

        Vector2 move = ReadMoveInput();
        Transform view = displayWeapon.transform;

        // Bob only advances while actually moving, so the weapon settles when standing still
        float speedMagnitude = Mathf.Clamp01(Mathf.Abs(move.x) + Mathf.Abs(move.y));
        if (speedMagnitude > 0.1f) bobTimer += Time.deltaTime * bobSpeed;

        // Horizontal bob runs at half rate against the vertical, tracing a figure-eight
        Vector3 bobOffset = new Vector3(
            Mathf.Cos(bobTimer * 0.5f) * (bobAmount * 0.5f),
            Mathf.Sin(bobTimer) * bobAmount,
            0f) * speedMagnitude;

        Vector3 targetPosition = initialDisplayPosition
            + new Vector3(-move.x * swayAmount, -move.y * swayAmount, 0f)
            + bobOffset
            + positionOffset;

        view.localPosition = Vector3.Lerp(view.localPosition, targetPosition, Time.deltaTime * swaySpeed)
            + positionShake;

        Quaternion tilt = Quaternion.Euler(move.y * tiltAmount, 0f, -move.x * tiltAmount);
        view.localRotation = Quaternion.Lerp(view.localRotation, baseRotation * tilt * post,
            Time.deltaTime * rotationLerpSpeed) * rotShake;
    }
}
