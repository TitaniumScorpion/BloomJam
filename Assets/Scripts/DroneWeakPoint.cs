using UnityEngine;
using System.Collections;

public class DroneWeakPoint : MonoBehaviour, IDamageable
{
    [Tooltip("Drag the SpawnerDrone root object here")]
    public SpawnerDrone parentDrone;

    [Header("Health")]
    public int maxHealth = 8;
    private int currentHealth;

    [Header("Hit Flash")]
    public Renderer weakPointRenderer;
    public Renderer weakPointRenderer2;
    [ColorUsage(true, true)]
    public Color flashEmissionColor = new Color(8f, 8f, 8f, 1f);
    public float flashDuration = 0.1f;
    public string emissionPropertyName = "_EmissionColor";

    private Material instanceMaterial;
    private Material instanceMaterial2;
    private Color originalEmissionColor;
    private Color originalEmissionColor2;
    private int emissionColorID;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        emissionColorID = Shader.PropertyToID(emissionPropertyName);

        if (weakPointRenderer != null)
        {
            instanceMaterial = weakPointRenderer.material;
            originalEmissionColor = instanceMaterial.GetColor(emissionColorID);
            instanceMaterial.EnableKeyword("_EMISSION");
        }

        if (weakPointRenderer2 != null)
        {
            instanceMaterial2 = weakPointRenderer2.material;
            originalEmissionColor2 = instanceMaterial2.GetColor(emissionColorID);
            instanceMaterial2.EnableKeyword("_EMISSION");
        }

        currentHealth = maxHealth;
    }

    private void OnDestroy()
    {
        if (instanceMaterial != null) Destroy(instanceMaterial);
        if (instanceMaterial2 != null) Destroy(instanceMaterial2);
    }

    public void TakeDamage(int incomingDamage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= incomingDamage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Deactivate();
        }
        else if (gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    private void Deactivate()
    {
        // Stop any in-progress flash
        if (flashCoroutine != null) { StopCoroutine(flashCoroutine); flashCoroutine = null; }

        // Kill emission — go dark
        if (instanceMaterial != null)
        {
            instanceMaterial.SetColor(emissionColorID, Color.black);
            instanceMaterial.DisableKeyword("_EMISSION");
        }
        if (instanceMaterial2 != null)
        {
            instanceMaterial2.SetColor(emissionColorID, Color.black);
            instanceMaterial2.DisableKeyword("_EMISSION");
        }

        // Disable all colliders so this weak point can't be hit again
        foreach (Collider col in GetComponents<Collider>())
            col.enabled = false;

        if (parentDrone != null)
            parentDrone.OnWeakPointDestroyed();
    }

    private IEnumerator FlashRoutine()
    {
        if (instanceMaterial != null)
            instanceMaterial.SetColor(emissionColorID, flashEmissionColor);
        if (instanceMaterial2 != null)
            instanceMaterial2.SetColor(emissionColorID, flashEmissionColor);

        yield return new WaitForSeconds(flashDuration);

        if (instanceMaterial != null)
            instanceMaterial.SetColor(emissionColorID, originalEmissionColor);
        if (instanceMaterial2 != null)
            instanceMaterial2.SetColor(emissionColorID, originalEmissionColor2);
    }
}
