using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CartoonLookSetup
{
    private const string Marker = "Library/RootsCartoonApplied.txt";
    static CartoonLookSetup() { EditorApplication.update += FirstApply; }

    private static void FirstApply()
    {
        if (File.Exists(Marker)) { EditorApplication.update -= FirstApply; return; }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (PrefabStageUtility.GetCurrentPrefabStage() != null) return;
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/Second Growth_Area1 1.unity") return;
        Shader cartoonShader = Shader.Find("Roots/Cartoon");
        if (cartoonShader == null || ShaderUtil.ShaderHasError(cartoonShader)) return;
        Apply();
        File.WriteAllText(Marker, "Cartoon look applied to the open scene; disable Cartoon Art Direction to restore.\n");
        EditorApplication.update -= FirstApply;
    }

    [MenuItem("Tools/Roots/Apply Cartoon Look")]
    public static void Apply()
    {
        if (PrefabStageUtility.GetCurrentPrefabStage() != null || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CartoonLook existing = root.GetComponentInChildren<CartoonLook>(true);
            if (existing == null) continue;
            Undo.RecordObject(existing, "Enable cartoon look");
            existing.enabled = true;
            existing.Apply(true);
            Selection.activeGameObject = existing.gameObject;
            return;
        }
        Shader shader = Shader.Find("Roots/Cartoon");
        if (shader == null || ShaderUtil.ShaderHasError(shader))
        { Debug.LogError("Cartoon shader has not compiled successfully."); return; }
        if (!AssetDatabase.IsValidFolder("Assets/CartoonMaterials")) AssetDatabase.CreateFolder("Assets", "CartoonMaterials");
        GameObject go = new GameObject("Cartoon Art Direction");
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Apply cartoon art direction");
        CartoonLook look = go.AddComponent<CartoonLook>();
        look.originalAmbient = RenderSettings.ambientLight;
        look.originalAmbientMode = RenderSettings.ambientMode;
        look.originalAmbientIntensity = RenderSettings.ambientIntensity;
        var surfaces = new List<CartoonLook.Surface>();
        var lamps = new List<CartoonLook.Lamp>();
        var materials = new Dictionary<Material, Material>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                Material[] original = renderer.sharedMaterials;
                Material[] cartoon = (Material[])original.Clone();
                bool changed = false;
                for (int i = 0; i < original.Length; i++)
                {
                    Material source = original[i];
                    if (source == null || source.renderQueue >= 2450 || !source.HasProperty("_BaseMap")
                        || (source.HasProperty("_Surface") && source.GetFloat("_Surface") > 0)) continue;
                    if (!materials.TryGetValue(source, out Material replacement))
                    {
                        replacement = new Material(shader) { name = source.name + " Cartoon" };
                        replacement.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                        replacement.SetTextureScale("_BaseMap", source.GetTextureScale("_BaseMap"));
                        replacement.SetTextureOffset("_BaseMap", source.GetTextureOffset("_BaseMap"));
                        if (source.HasProperty("_BaseColor")) replacement.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                        if (source.IsKeywordEnabled("_EMISSION") && source.HasProperty("_EmissionColor"))
                        {
                            replacement.SetColor("_EmissionColor", source.GetColor("_EmissionColor") * 0.65f);
                            if (source.HasProperty("_EmissionMap") && source.GetTexture("_EmissionMap") != null)
                                replacement.SetTexture("_EmissionMap", source.GetTexture("_EmissionMap"));
                        }
                        string assetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/CartoonMaterials/CartoonMaterial.mat");
                        AssetDatabase.CreateAsset(replacement, assetPath);
                        materials.Add(source, replacement);
                    }
                    cartoon[i] = replacement;
                    changed = true;
                }
                if (changed) surfaces.Add(new CartoonLook.Surface { renderer = renderer, original = original, cartoon = cartoon });
            }
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
                lamps.Add(new CartoonLook.Lamp { light = light, color = light.color, intensity = light.intensity,
                    shadowStrength = light.shadowStrength, shadows = light.shadows });
        }
        look.surfaces = surfaces.ToArray();
        look.lamps = lamps.ToArray();
        look.initialized = true;
        look.Apply(true);
        EditorUtility.SetDirty(look);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = go;
        SceneView.RepaintAll();
        Debug.Log($"Cartoon look applied: {surfaces.Count} renderers, {materials.Count} materials, {lamps.Count} lights. Disable Cartoon Art Direction to restore.");
    }
}
