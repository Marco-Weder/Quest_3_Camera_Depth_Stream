using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ClearNullEditorPrefs
{
   
    static ClearNullEditorPrefs()
    {
        if (EditorPrefs.HasKey(null))
        {
            EditorPrefs.DeleteKey(null);
            Debug.Log("[Meta] Cleared null EditorPrefs key → Building Blocks should now appear.");
        }
    }
    
    [MenuItem("Meta/Clear Null EditorPrefs Key")]
    private static void ManualClear()
    {
        if (EditorPrefs.HasKey(null))
        {
            EditorPrefs.DeleteKey(null);
            Debug.Log("[Meta] Manually cleared null EditorPrefs key.");
        }
        else
        {
            Debug.Log("[Meta] No null key found in EditorPrefs.");
        }
    }
}
