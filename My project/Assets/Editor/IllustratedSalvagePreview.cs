using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class IllustratedSalvagePreview
{
    static IllustratedSalvagePreview() { EditorApplication.update += BuildOnce; }
    private static void BuildOnce()
    {
        if (File.Exists("Library/IllustratedSalvagePreview.txt")) { EditorApplication.update -= BuildOnce; return; }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || PrefabStageUtility.GetCurrentPrefabStage() != null) return;
        Scene source = SceneManager.GetActiveScene();
        if (source.path != "Assets/Scenes/Second Growth_Area1 1.unity"
            && source.path != "Assets/Scenes/KH2_StylePreview.unity"
            && source.path != "Assets/Scenes/Fortnite_StylePreview.unity") return;
        Shader shader = Shader.Find("Roots/IllustratedSalvage");
        Shader outline = Shader.Find("Roots/IllustratedOutline");
        if (shader == null || outline == null || ShaderUtil.ShaderHasError(shader) || ShaderUtil.ShaderHasError(outline)) return;
        PaintedAdventurePreview.Build(source, shader, false, true);
        EditorApplication.update -= BuildOnce;
    }
}
