using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RestoreOriginalLook
{
    private const string Marker = "Library/RootsOriginalLookRestored.txt";
    static RestoreOriginalLook() { EditorApplication.update += Restore; }
    private static void Restore()
    {
        if (File.Exists(Marker)) { EditorApplication.update -= Restore; return; }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || PrefabStageUtility.GetCurrentPrefabStage() != null) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Second Growth_Area1 1.unity") return;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (CartoonLook look in root.GetComponentsInChildren<CartoonLook>(true))
            {
                Undo.RecordObject(look, "Restore original art style");
                look.Apply(false);
                look.enabled = false;
                EditorUtility.SetDirty(look);
            }
        EditorSceneManager.MarkSceneDirty(scene);
        SceneView.RepaintAll();
        File.WriteAllText(Marker, "Original materials and lighting restored.\n");
        Debug.Log("Original materials and lighting restored.");
        EditorApplication.update -= Restore;
    }
}
