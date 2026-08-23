using UnityEngine;

public class MissingScriptFinder : MonoBehaviour
{
    [ContextMenu("Find Missing Scripts In Scene")]
    public void FindMissingScripts()
    {
        int found = 0;
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c == null)
                {
                    Debug.LogWarning($"Missing script on: '{go.name}' | Scene: {go.scene.name}", go);
                    found++;
                }
            }
        }
        Debug.Log($"Scan done. Found {found} missing script(s).");
    }
}
