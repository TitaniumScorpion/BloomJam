using UnityEngine;
using System.Collections;

public class DroneWeakPoint : MonoBehaviour
{
    [Tooltip("Drag the SpawnerDrone root object here")]
    public SpawnerDrone parentDrone;
    public int damage = 1;

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
    }

    private void OnDestroy()
    {
        if (instanceMaterial != null) Destroy(instanceMaterial);
        if (instanceMaterial2 != null) Destroy(instanceMaterial2);
    }

    public void TakeDamage(int incomingDamage)
    {
        if (parentDrone != null)
            parentDrone.TakeDamage(damage);

        if (gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }
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
