using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// One-shot generator for the sword combo animation template:
/// four placeholder clips plus an Animator Controller whose state names match the
/// defaults in <see cref="KatanaWeapon.comboSwings"/>.
///
/// The clips are deliberately crude — they exist so the whole path (controller →
/// state → clip → Animation Event → damage) can be tested before any real animation
/// work happens. Replace them by re-keying the clips in the Animation window, or by
/// dropping your own clip into the state's Motion slot.
///
/// Re-running this NEVER overwrites an asset that already exists, so it is safe to
/// invoke again after you have replaced a clip with the real thing.
/// </summary>
public static class SwordAnimationSetup
{
    private const string FolderPath = "Assets/Animation/Sword";
    private const string ControllerPath = FolderPath + "/SwordCombo.controller";

    private const string IdleState = "Idle";
    private const float FrameRate = 60f;

    /// <summary>A placeholder swing, described the way the Animation window would show it.</summary>
    private struct SwingSpec
    {
        public string Name;
        public float Length;
        public float AnticipationTime;
        public Vector3 AnticipationPose;
        public float ContactTime;
        public Vector3 ContactPose;
        public float EventTime;
    }

    // Poses mirror KatanaWeapon's procedural fallback offsets, so the placeholder clips
    // read the same as what the sword already does without an Animator assigned.
    private static readonly SwingSpec[] Swings =
    {
        new SwingSpec
        {
            Name = "Swing_Left", Length = 0.35f,
            AnticipationTime = 0.06f, AnticipationPose = new Vector3(-20f, -6f, 6f),
            ContactTime = 0.16f, ContactPose = new Vector3(85f, 20f, -20f),
            EventTime = 0.13f,
        },
        new SwingSpec
        {
            Name = "Swing_Right", Length = 0.35f,
            AnticipationTime = 0.06f, AnticipationPose = new Vector3(-20f, 9f, -6f),
            ContactTime = 0.16f, ContactPose = new Vector3(85f, -35f, 25f),
            EventTime = 0.13f,
        },
        new SwingSpec
        {
            Name = "Swing_Cross", Length = 0.5f,
            AnticipationTime = 0.08f, AnticipationPose = new Vector3(-25f, 0f, 0f),
            ContactTime = 0.2f, ContactPose = new Vector3(110f, 0f, 0f),
            EventTime = 0.17f,
        },
    };

    [MenuItem("Tools/Devil Engine/Generate Sword Animation Template")]
    public static void Generate()
    {
        EnsureFolder();

        AnimationClip idleClip = GetOrCreateIdleClip();

        var swingClips = new List<AnimationClip>();
        foreach (SwingSpec spec in Swings)
            swingClips.Add(GetOrCreateSwingClip(spec));

        BuildController(idleClip, swingClips);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[SwordAnimationSetup] Template ready in " + FolderPath + ".\n" +
            "Next, in the Hierarchy:\n" +
            "1. Under Player > ... > SwordParent, create an empty child named SwordPivot (reset its Transform).\n" +
            "2. Drag Sword Base and SwordLv1-4 into SwordPivot.\n" +
            "3. Add an Animator to SwordPivot; set its Controller to SwordCombo.\n" +
            "4. Add a SwordAnimationEvents component to SwordPivot.\n" +
            "5. On KatanaWeapon, set Sword Animator to that Animator.\n" +
            "Then press Play and right-click three times.");

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
    }

    // ── Clips ────────────────────────────────────────────────────────────────

    private static AnimationClip GetOrCreateIdleClip()
    {
        string path = FolderPath + "/" + IdleState + ".anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) return existing;

        var clip = new AnimationClip { frameRate = FrameRate };

        // A single key at rest. The swing states all end at rest too, so idle is really
        // just a clean state for the machine to sit in between attacks.
        SetEulerCurves(clip,
            new[] { 0f, 1f },
            new[] { Vector3.zero, Vector3.zero });

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip GetOrCreateSwingClip(SwingSpec spec)
    {
        string path = FolderPath + "/" + spec.Name + ".anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) return existing;

        var clip = new AnimationClip { frameRate = FrameRate };

        // Anticipation → contact → recovery. The recovery leg is the long one, which is
        // most of what makes a swing read as a swing.
        SetEulerCurves(clip,
            new[] { 0f, spec.AnticipationTime, spec.ContactTime, spec.Length },
            new[] { Vector3.zero, spec.AnticipationPose, spec.ContactPose, Vector3.zero });

        // Fires SwordHit on the contact frame. Harmless while KatanaWeapon's
        // Use Animation Event For Hit is off — the hit has already resolved by then,
        // and the second call is ignored.
        AnimationUtility.SetAnimationEvents(clip, new[]
        {
            new AnimationEvent { time = spec.EventTime, functionName = "SwordHit" },
        });

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    /// <summary>
    /// Writes one localEulerAnglesRaw curve per axis onto the animator's own transform
    /// (empty path), which is the SwordPivot the Animator will sit on.
    /// </summary>
    private static void SetEulerCurves(AnimationClip clip, float[] times, Vector3[] poses)
    {
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.x", BuildCurve(times, poses, 0));
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.y", BuildCurve(times, poses, 1));
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", BuildCurve(times, poses, 2));
    }

    private static AnimationCurve BuildCurve(float[] times, Vector3[] poses, int axis)
    {
        var keys = new Keyframe[times.Length];
        for (int i = 0; i < times.Length; i++)
            keys[i] = new Keyframe(times[i], poses[i][axis]);

        var curve = new AnimationCurve(keys);

        // Smooth the interior keys so the blade arcs through contact instead of easing
        // to a stop on it. First and last keep flat tangents so the clip starts and
        // settles cleanly.
        for (int i = 1; i < keys.Length - 1; i++)
            curve.SmoothTangents(i, 0f);

        return curve;
    }

    // ── Controller ───────────────────────────────────────────────────────────

    private static void BuildController(AnimationClip idleClip, List<AnimationClip> swingClips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            Debug.Log("[SwordAnimationSetup] " + ControllerPath + " already exists — left untouched.");
            return;
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimatorState idle = stateMachine.AddState(IdleState);
        idle.motion = idleClip;
        stateMachine.defaultState = idle;

        // No parameters and no transitions INTO the swings on purpose: KatanaWeapon plays
        // each state by name with CrossFadeInFixedTime, which needs neither.
        for (int i = 0; i < Swings.Length; i++)
        {
            AnimatorState state = stateMachine.AddState(Swings[i].Name);
            state.motion = swingClips[i];

            AnimatorStateTransition toIdle = state.AddTransition(idle);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 1f;
            toIdle.duration = 0.1f;
        }

        EditorUtility.SetDirty(controller);
    }

    // ── Folder ───────────────────────────────────────────────────────────────

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animation"))
            AssetDatabase.CreateFolder("Assets", "Animation");
        if (!AssetDatabase.IsValidFolder(FolderPath))
            AssetDatabase.CreateFolder("Assets/Animation", "Sword");
    }
}
