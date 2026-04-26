using UnityEngine;
using UnityEngine.SceneManagement;

public class Elevator : MonoBehaviour
{
    [Header("Elevator Settings")]
    public float ascensionTime = 6.5f; // Time in seconds to reach the target height
    public float targetHeight = 20f;
    
    [Tooltip("Leave empty to just load the next scene sequentially in the Build Settings")]
    public string nextSceneName; 
    
    private bool isPlayerOnBoard = false;
    private bool isLifting = false;
    private Transform playerTransform;
    private AudioSource liftAudioSource;
    private float currentLiftSpeed;

    private void Awake()
    {
        liftAudioSource = gameObject.AddComponent<AudioSource>();
        liftAudioSource.spatialBlend = 1f; // Make it a 3D sound attached to the elevator
        liftAudioSource.playOnAwake = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Start lifting when the player steps into the trigger
        if (!isLifting && other.CompareTag("Player"))
        {
            isPlayerOnBoard = true;
            isLifting = true;
            playerTransform = other.transform;
            
            // Calculate exactly how fast it needs to move to arrive in the set time
            float distanceToTravel = targetHeight - transform.position.y;
            currentLiftSpeed = distanceToTravel / ascensionTime;
            
            if (AudioManager.Instance != null && AudioManager.Instance.elevatorAscendSound != null)
            {
                liftAudioSource.clip = AudioManager.Instance.elevatorAscendSound;
                liftAudioSource.volume = AudioManager.Instance.elevatorAscendVolume;
                liftAudioSource.Play();
            }
            
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
            liftAudioSource.Stop(); // Stop the sound if the player bails out early
        }
    }

    private void Update()
    {
        if (isLifting && isPlayerOnBoard)
        {
            // Move the elevator upwards
            transform.Translate(Vector3.up * currentLiftSpeed * Time.deltaTime);

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