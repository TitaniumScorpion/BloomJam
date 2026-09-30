using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only: keeps Play mode running while the Unity window is unfocused.
///
/// Player Settings > Run In Background is off, which is right for the build - a 1 HP
/// speedrun should not keep playing while alt-tabbed - but in the editor it freezes Play
/// mode the moment focus moves to another window. That stalls anything driving the game
/// from outside Unity, such as MCP-driven play tests.
///
/// In the editor, Application.runInBackground writes straight through to the Player Setting,
/// so the original is remembered on entering Play mode and put back on leaving it. It lives in
/// SessionState because a static field would not survive the domain reload in between.
/// </summary>
[InitializeOnLoad]
internal static class PlayModeRunInBackground
{
    private const string OriginalKey = "PlayModeRunInBackground.Original";
    private const int NothingStored = -1;

    static PlayModeRunInBackground()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.EnteredPlayMode:
                SessionState.SetInt(OriginalKey, PlayerSettings.runInBackground ? 1 : 0);
                Application.runInBackground = true;
                break;

            case PlayModeStateChange.EnteredEditMode:
                int original = SessionState.GetInt(OriginalKey, NothingStored);
                if (original == NothingStored) break;
                PlayerSettings.runInBackground = original == 1;
                SessionState.EraseInt(OriginalKey);
                break;
        }
    }
}
