using UnityEngine;
using UnityEngine.SceneManagement;

public class Elevator : MonoBehaviour
{
    [Header("Elevator Settings")]
    public float liftSpeed = 5f;
    public float targetHeight = 20f;
    
    [Tooltip("Leave empty to just load the next scene sequentially in the Build Settings")]
    public string nextSceneName; 
    
    private bool isPlayerOnBoard = false;
    private bool isLifting = false;
    private Transform playerTransform;

    private void OnTriggerEnter(Collider other)
    {
        // Start lifting when the player steps into the trigger
        if (!isLifting && other.CompareTag("Player"))
        {
            isPlayerOnBoard = true;
            isLifting = true;
            playerTransform = other.transform;
            
            // Parent the player to the elevator so they move up together perfectly
            // (Ensure your elevator GameObject has a scale of 1,1,1 to avoid player scaling issues)
            playerTransform.SetParent(transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // If the player dashes out before it goes too high, stop lifting
        if (isLifting && other.CompareTag("Player") && transform.position.y < targetHeight * 0.5f)
        {
            isPlayerOnBoard = false;
            isLifting = false;
            playerTransform.SetParent(null);
            playerTransform = null;
        }
    }

    private void Update()
    {
        if (isLifting && isPlayerOnBoard)
        {
            // Move the elevator upwards
            transform.Translate(Vector3.up * liftSpeed * Time.deltaTime);

            // Check if we reached the top
            if (transform.position.y >= targetHeight)
            {
                LoadNextArena();
            }
        }
    }

    private void LoadNextArena()
    {
        // Detach player before loading scene
        if (playerTransform != null)
        {
            playerTransform.SetParent(null);
        }

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // Fallback: Load the next scene in the build settings
            int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextBuildIndex);
            }
            else
            {
                Debug.LogWarning("No more scenes in build index! Game Completed.");
            }
        }
    }
}