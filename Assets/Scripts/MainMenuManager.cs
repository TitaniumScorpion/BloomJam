using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private void Start()
    {
        // Ensure cursor is visible and unlocked on the Main Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Ensure time scale is normal when entering the menu
        Time.timeScale = 1f;
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
