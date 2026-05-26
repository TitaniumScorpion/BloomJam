using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenuManager : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform menuCamera;
    public float rotationSpeed = 10f;
    
    private int currentPanelIndex = 0;
    private float[] panelAngles = { 0f, 90f, 180f, 270f };
    private float targetAngle = 0f;

    private void Start()
    {
        // Ensure cursor is visible and unlocked on the Main Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Ensure time scale is normal when entering the menu
        Time.timeScale = 1f;

        // Automatically grab the main camera if one isn't assigned
        if (menuCamera == null && Camera.main != null)
        {
            menuCamera = Camera.main.transform;
        }
        targetAngle = panelAngles[currentPanelIndex];
    }

    private void Update()
    {
        HandleInput();
        SmoothRotateCamera();
    }

    private void HandleInput()
    {
        bool moveLeft = false;
        bool moveRight = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                moveLeft = true;
            if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                moveRight = true;
        }

        // We add/subtract continuously by 90 degrees instead of snapping to array values 
        // to ensure the Slerp always rotates in the correct, shortest direction without unwinding.
        if (moveRight)
        {
            currentPanelIndex = (currentPanelIndex + 1) % panelAngles.Length;
            targetAngle += 90f; 
        }
        else if (moveLeft)
        {
            currentPanelIndex--;
            if (currentPanelIndex < 0) currentPanelIndex = panelAngles.Length - 1;
            targetAngle -= 90f;
        }
    }

    private void SmoothRotateCamera()
    {
        if (menuCamera == null) return;
        
        Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
        menuCamera.rotation = Quaternion.Slerp(menuCamera.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    public void PlayGame()
    {
        // Ensure progression stats are fully reset before starting
        QuotaManager.ResetProgression();
        
        // Load Zone 1. We will set the Main Menu to be index 0, and Zone 1 to be index 1 in the Build Settings.
        SceneManager.LoadScene(1);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game from Main Menu...");
        Application.Quit();
    }
}
