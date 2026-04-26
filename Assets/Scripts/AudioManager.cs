using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Clips")]
    public AudioClip pistolShootSound;
    public AudioClip bombExplosionSound;
    public AudioClip enemySpawnTelegraphSound;
    public AudioClip eliteShootSound;
    
    [Header("New Audio Clips")]
    public AudioClip backgroundSound;
    public AudioClip enemyMoveSound;
    public AudioClip eliteMoveSound;
    public AudioClip hitSound;
    public AudioClip gameOverSound;
    public AudioClip playerWalkSound;
    public AudioClip levelCompleteSound;
    public AudioClip playerDashSound;

    [Header("Volumes")]
    [Range(0f, 1f)] public float pistolShootVolume = 0.15f;
    [Range(0f, 3f)] public float bombExplosionVolume = 1f;
    [Range(0f, 1f)] public float enemySpawnTelegraphVolume = 1f;
    [Range(0f, 1f)] public float eliteShootVolume = 1f;
    [Range(0f, 1f)] public float backgroundVolume = 0.5f;
    [Range(0f, 1f)] public float enemyMoveVolume = 0.7f;
    [Range(0f, 1f)] public float eliteMoveVolume = 1f;
    [Range(0f, 3f)] public float hitVolume = 0.9f;
    [Range(0f, 1f)] public float gameOverVolume = 1f;
    [Range(0f, 1f)] public float playerWalkVolume = 0.3f;
    [Range(0f, 1f)] public float levelCompleteVolume = 1f;
    [Range(0f, 1f)] public float playerDashVolume = 0.6f;

    [Header("Pool Settings")]
    public int poolSize = 150; // Massively increased to prevent pool exhaustion during heavy swarms

    private Queue<AudioSource> audioPool;
    private AudioSource bgmSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Carry the Audio Manager across all zones!
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Setup and play the looping background music
        if (backgroundSound != null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.clip = backgroundSound;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f; // 0 = 2D sound (plays equally everywhere)
            bgmSource.volume = backgroundVolume; 
            bgmSource.priority = 0; // Highest priority (0) so background music never cuts out
            bgmSource.Play();
        }
    }

    private void InitializePool()
    {
        audioPool = new Queue<AudioSource>();
        for (int i = 0; i < poolSize; i++)
        {
            // Create an invisible child object to hold the AudioSource
            GameObject obj = new GameObject("PooledAudioSource_" + i);
            obj.transform.SetParent(transform);
            AudioSource source = obj.AddComponent<AudioSource>();
            
            // Optimize for 3D Spatial Audio
            source.playOnAwake = false;
            source.spatialBlend = 1f; // 1 = Fully 3D sound
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 10f; // Increased for better audibility
            source.maxDistance = 150f; // Greatly increased arena hearing range
            
            obj.SetActive(false);
            audioPool.Enqueue(source);
        }
    }

    // Added 'priority' and 'spatialBlend' parameters. spatialBlend: 0 = 2D (global), 1 = 3D
    public void PlaySoundAtLocation(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, int priority = 128, float spatialBlend = 1f)
    {
        if (clip == null) return;

        AudioSource source;
        if (audioPool.Count > 0)
        {
            source = audioPool.Dequeue();
        }
        else
        {
            // Pool is empty! Dynamically expand it to prevent sound starvation during massive waves.
            GameObject obj = new GameObject("PooledAudioSource_" + poolSize);
            obj.transform.SetParent(transform);
            source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 10f;
            source.maxDistance = 150f;
            poolSize++;
        }

        source.gameObject.SetActive(true);
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.priority = priority; 
        source.spatialBlend = spatialBlend;
        source.Play();

        // Start a timer to return this audio source to the pool exactly when the clip finishes
        // Divide by pitch because a lower pitch makes the sound play slower (requiring more time to finish)
        StartCoroutine(ReturnToPool(source, clip.length / pitch));
    }

    private IEnumerator ReturnToPool(AudioSource source, float delay)
    {
        // Use Realtime so the pool doesn't get permanently stuck if a sound plays while the game is paused
        yield return new WaitForSecondsRealtime(delay);
        source.Stop();
        source.gameObject.SetActive(false);
        audioPool.Enqueue(source);
    }
}