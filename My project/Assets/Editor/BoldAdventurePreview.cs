using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class BoldAdventurePreview
{
    static BoldAdventurePreview() { EditorApplication.update += BuildOnce; }
    private static void BuildOnce()
    {
        if (File.Exists("Library/BoldAdventurePreview2.txt")) { EditorApplication.update -= BuildOnce; return; }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || PrefabStageUtility.GetCurrentPrefabStage() != null) return;
        Scene source = SceneManager.GetActiveScene();
        if (source.path != "Assets/Scenes/Second Growth_Area1 1.unity"
            && source.path != "Assets/Scenes/KH2_StylePreview.unity") return;
        Shader shader = Shader.Find("Roots/BoldAdventure");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) return;
        PaintedAdventurePreview.Build(source, shader, true);
        EditorApplication.update -= BuildOnce;
    }
}
