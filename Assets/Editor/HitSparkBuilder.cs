using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the enemy-hit spark effect from scratch - texture, material and prefab - with no
/// asset-store content involved. Editor-only; lives in Assets/Editor so it is excluded from
/// builds.
///
/// The values below are the effect's specification. Re-running the menu item rebuilds the
/// prefab from them, so tune here when a change should be permanent, and in the Inspector
/// while you are still deciding. Rebuilding overwrites Inspector tweaks by design.
/// </summary>
public static class HitSparkBuilder
{
    private const string VfxFolder = "Assets/VFX";
    private const string PrefabFolder = "Assets/Prefabs/VFX";
    private const string TexturePath = VfxFolder + "/SparkDot.png";
    private const string MaterialPath = VfxFolder + "/HitSparkAdditive.mat";
    private const string PrefabPath = PrefabFolder + "/HitSpark.prefab";

    // HDR start colours. Values above 1 are what the bloom threshold (1.0 in SampleSceneProfile)
    // tests against, so these drive the glow. Kept moderate on the sparks because ACES
    // tonemapping desaturates very bright colour toward white - push these too far and the
    // orange washes out to a uniform white.
    private static readonly Color FlashColor = new Color(9f, 7f, 4.5f);
    private static readonly Color SparkColor = new Color(3.2f, 1.5f, 0.45f);
    private static readonly Color EmberColor = new Color(2.2f, 0.85f, 0.25f);

    [MenuItem("Tools/Devil Engine/Build Hit Spark VFX")]
    public static void Build()
    {
        EnsureFolder(VfxFolder);
        EnsureFolder(PrefabFolder);

        Texture2D texture = BuildSparkTexture();
        Material material = BuildMaterial(texture);

        GameObject root = new GameObject("HitSpark");
        try
        {
            BuildFlash(root, material);
            BuildSparks(root, material);
            BuildEmbers(root, material);
            root.AddComponent<PooledParticleEffect>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Debug.Log("Built hit spark VFX at " + PrefabPath);
    }

    // -- Assets ---------------------------------------------------------------

    /// <summary>
    /// A soft round dot with a hot centre. Stretched Billboard smears this along the particle's
    /// velocity, so one dot is all the sparks need - the streak is the render mode, not the art.
    /// </summary>
    private static Texture2D BuildSparkTexture()
    {
        const int size = 64;
        const float radius = size * 0.5f;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) - radius;
                float dy = (y + 0.5f) - radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) / radius;

                // pow(1-d, 2.2) gives a tight core with a long soft skirt, which reads as a
                // glow rather than a disc once it is stretched and blooming
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), TexturePath), texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);

        if (AssetImporter.GetAtPath(TexturePath) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
    }

    /// <summary>
    /// URP Particles/Unlit set to additive. Additive is what makes overlapping sparks sum past
    /// the bloom threshold, and it means the texture's black areas cost nothing visually.
    /// </summary>
    private static Material BuildMaterial(Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            Debug.LogError("URP Particles/Unlit shader not found - is the project still on URP?");
            shader = Shader.Find("Sprites/Default");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.shader = shader;

        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white); // tint comes from the particles, not here

        // Transparent + additive. The blend factors have to be set alongside the _Surface and
        // _Blend enums - URP's material inspector writes both, and only the factors affect
        // rendering, so setting the enums alone looks correct in the UI but renders as alpha.
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        EditorUtility.SetDirty(material);
        return material;
    }

    // -- Systems --------------------------------------------------------------

    /// <summary>
    /// The impact pop: one bright quad, gone in under a tenth of a second. This is what sells
    /// the moment of contact - the sparks are the aftermath and read too slowly on their own.
    /// </summary>
    private static void BuildFlash(GameObject parent, Material material)
    {
        ParticleSystem system = CreateSystem("Flash", parent, material, 0.2f);

        ParticleSystem.MainModule main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = FlashColor;
        main.maxParticles = 2;

        Burst(system, 1);

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = false; // a single particle at the exact contact point

        // Shrinking away is what makes it read as a flash rather than a sprite blinking off
        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve(1f, 0.45f));

        Fade(system, 0.5f);

        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    /// <summary>
    /// The sparks themselves. Tuned to fall rather than explode: a low launch speed that is
    /// damped away almost immediately, then gravity takes over for the rest of a long lifetime.
    /// Because Stretched Billboard orients each particle along its own velocity, that hand-off
    /// turns the streaks from outward to vertical on their own - which is the downward trail.
    /// </summary>
    private static void BuildSparks(GameObject parent, Material material)
    {
        ParticleSystem system = CreateSystem("Sparks", parent, material, 0.25f);

        ParticleSystem.MainModule main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(7f, 18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startColor = SparkColor;
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.7f);
        main.maxParticles = 24;

        Burst(system, 12);

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 55f;
        shape.radius = 0.04f;
        // Scatter within the cone. Without it every spark leaves on a clean radial line and
        // the burst reads as a fan rather than a spray
        shape.randomDirectionAmount = 0.12f;

        // Deliberately no Limit Velocity here. Damping the launch speed collapses every
        // spark onto the same velocity within a few frames, and particles moving at identical
        // speeds in a narrow cone never separate - the burst stays as one clump for its whole
        // life. The spread comes from the width of the startSpeed range, so it has to survive.

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve(1f, 0.25f));

        // Cooling metal: hot white core, through orange, to a dark ember before it fades.
        // Gradients are 8-bit and clamp to 1, so this only shapes the colour - the HDR
        // brightness that bloom reacts to comes from main.startColor and is multiplied through.
        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(BuildGradient(
            new[] { (0f, Color.white), (0.25f, new Color(1f, 0.75f, 0.35f)), (1f, new Color(0.9f, 0.25f, 0.06f)) },
            new[] { (0f, 1f), (0.55f, 1f), (1f, 0f) }));

        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.6f;
        renderer.velocityScale = 0.09f;
    }

    /// <summary>
    /// A handful of slower, longer-lived bits that keep falling after the main burst is gone.
    /// They stop the effect ending all at once, which is most of what separates a hit that
    /// feels physical from one that feels like a sprite.
    /// </summary>
    private static void BuildEmbers(GameObject parent, Material material)
    {
        ParticleSystem system = CreateSystem("Embers", parent, material, 0.25f);

        ParticleSystem.MainModule main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.11f);
        main.startColor = EmberColor;
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.6f);
        main.maxParticles = 12;

        Burst(system, 3);

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 55f;
        shape.radius = 0.03f;

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(BuildGradient(
            new[] { (0f, new Color(1f, 0.8f, 0.45f)), (1f, new Color(0.8f, 0.2f, 0.05f)) },
            new[] { (0f, 1f), (0.4f, 0.9f), (1f, 0f) }));

        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2f;
        renderer.velocityScale = 0.08f;
    }

    // -- Helpers --------------------------------------------------------------

    private static ParticleSystem CreateSystem(string name, GameObject parent, Material material, float duration)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        ParticleSystem system = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = system.main;
        main.duration = duration;
        main.loop = false;
        main.playOnAwake = false; // PooledParticleEffect drives playback explicitly
        // World space, so particles already in flight are not dragged along when the pool
        // repositions this instance for its next hit
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        // Hierarchy, so the prefab root's transform scale acts as one global size dial -
        // the fastest way to re-judge the whole effect without touching three systems
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);

        system.GetComponent<ParticleSystemRenderer>().material = material;
        return system;
    }

    private static void Burst(ParticleSystem system, short count)
    {
        ParticleSystem.EmissionModule emission = system.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
    }

    /// <summary>Alpha-only fade, for systems whose colour should not shift.</summary>
    private static void Fade(ParticleSystem system, float holdUntil)
    {
        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(BuildGradient(
            new[] { (0f, Color.white), (1f, Color.white) },
            new[] { (0f, 1f), (holdUntil, 1f), (1f, 0f) }));
    }

    private static AnimationCurve Curve(float from, float to)
    {
        return AnimationCurve.EaseInOut(0f, from, 1f, to);
    }

    private static Gradient BuildGradient((float time, Color color)[] colors, (float time, float alpha)[] alphas)
    {
        GradientColorKey[] colorKeys = new GradientColorKey[colors.Length];
        for (int i = 0; i < colors.Length; i++)
            colorKeys[i] = new GradientColorKey(colors[i].color, colors[i].time);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[alphas.Length];
        for (int i = 0; i < alphas.Length; i++)
            alphaKeys[i] = new GradientAlphaKey(alphas[i].alpha, alphas[i].time);

        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
