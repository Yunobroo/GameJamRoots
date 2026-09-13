using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PaintedAdventurePreview
{
    private const string Marker = "Library/PaintedAdventurePreview.txt";
    static PaintedAdventurePreview() { EditorApplication.update += BuildOnce; }
    private static void BuildOnce()
    {
        if (File.Exists(Marker)) { EditorApplication.update -= BuildOnce; return; }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || PrefabStageUtility.GetCurrentPrefabStage() != null) return;
        Scene source = SceneManager.GetActiveScene();
        if (source.path != "Assets/Scenes/Second Growth_Area1 1.unity") return;
        Shader shader = Shader.Find("Roots/PaintedAdventure");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) return;
        Build(source, shader);
        EditorApplication.update -= BuildOnce;
    }

    public static void Build(Scene source, Shader shader, bool bold = false, bool illustrated = false, bool ruined = false)
    {
        string label = ruined ? "RuinedSalvage" : illustrated ? "IllustratedSalvage" : bold ? "Fortnite" : "KH2";
        string folder = ruined ? "RuinedSalvageMaterials" : illustrated ? "IllustratedSalvageMaterials" : bold ? "BoldAdventureMaterials" : "PaintedAdventureMaterials";
        var renderers = new List<MeshRenderer>();
        var originals = new Dictionary<Material, Material>();
        foreach (GameObject root in source.GetRootGameObjects())
        {
            renderers.AddRange(root.GetComponentsInChildren<MeshRenderer>(true));
            foreach (CartoonLook look in root.GetComponentsInChildren<CartoonLook>(true))
                foreach (CartoonLook.Surface surface in look.surfaces)
                    for (int i = 0; i < surface.cartoon.Length; i++)
                        if (surface.cartoon[i] != null && surface.original[i] != null)
                            originals[surface.cartoon[i]] = surface.original[i];
        }
        MeshRenderer door = renderers.Find(r => r.name.Contains("Door_Left") && r.gameObject.activeInHierarchy);
        if (door == null) { Debug.LogWarning("Painted preview needs a Door_Left mesh in the source scene."); return; }
        Vector3 center = door.bounds.center;
        Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(preview);
        GameObject room = new GameObject(label + " Inspired - Doorway Study");
        var materials = new Dictionary<string, Material>();
        if (!AssetDatabase.IsValidFolder("Assets/" + folder)) AssetDatabase.CreateFolder("Assets", folder);
        Material outline = null;
        if (illustrated)
        {
            outline = new Material(Shader.Find("Roots/IllustratedOutline"));
            AssetDatabase.CreateAsset(outline, AssetDatabase.GenerateUniqueAssetPath("Assets/" + folder + "/Ink.mat"));
        }
        int count = 0;
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.name == "Ink contour") continue;
            if (!renderer.gameObject.activeInHierarchy || !renderer.enabled || Vector3.Distance(renderer.bounds.center, center) > 9f) continue;
            MeshFilter sourceMesh = renderer.GetComponent<MeshFilter>();
            if (sourceMesh == null || sourceMesh.sharedMesh == null) continue;
            GameObject piece = new GameObject(renderer.name);
            piece.layer = 31;
            piece.transform.SetParent(room.transform);
            piece.transform.SetPositionAndRotation(renderer.transform.position - center, renderer.transform.rotation);
            piece.transform.localScale = renderer.transform.lossyScale;
            piece.AddComponent<MeshFilter>().sharedMesh = sourceMesh.sharedMesh;
            MeshRenderer target = piece.AddComponent<MeshRenderer>();
            string partName = renderer.name.ToLowerInvariant();
            int paintRole = !illustrated ? 0 : partName.Contains("floor") || partName.Contains("ramp") ? 1
                : partName.Contains("door") || partName.Contains("column") ? 2 : 3;
            Material[] slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
            {
                Material original = slots[i];
                if (original == null) continue;
                if (originals.TryGetValue(original, out Material restored)) original = restored;
                slots[i] = original;
                if (original.renderQueue >= 2450 || !original.HasProperty("_BaseMap")
                    || (original.HasProperty("_Surface") && original.GetFloat("_Surface") > 0)) continue;
                string materialKey = original.GetEntityId() + ":" + paintRole;
                if (!materials.TryGetValue(materialKey, out Material painted))
                {
                    painted = new Material(shader) { name = original.name + " Painted" };
                    painted.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));
                    painted.SetTextureScale("_BaseMap", original.GetTextureScale("_BaseMap"));
                    painted.SetTextureOffset("_BaseMap", original.GetTextureOffset("_BaseMap"));
                    if (original.HasProperty("_BaseColor")) painted.SetColor("_BaseColor", original.GetColor("_BaseColor"));
                    if (illustrated)
                        painted.SetColor("_PaintColor", paintRole == 1 ? new Color(0.20f, 0.29f, 0.29f)
                            : paintRole == 2 ? new Color(0.28f, 0.52f, 0.49f) : new Color(0.86f, 0.84f, 0.73f));
                    if (original.IsKeywordEnabled("_EMISSION") && original.HasProperty("_EmissionColor"))
                    {
                        painted.SetColor("_EmissionColor", original.GetColor("_EmissionColor") * 0.25f);
                        if (original.HasProperty("_EmissionMap") && original.GetTexture("_EmissionMap") != null)
                            painted.SetTexture("_EmissionMap", original.GetTexture("_EmissionMap"));
                    }
                    AssetDatabase.CreateAsset(painted, AssetDatabase.GenerateUniqueAssetPath("Assets/" + folder + "/Painted.mat"));
                    materials.Add(materialKey, painted);
                }
                slots[i] = painted;
            }
            target.sharedMaterials = slots;
            if (illustrated && System.Array.Exists(slots, material => material != null && material.shader == shader))
            {
                GameObject ink = new GameObject("Ink contour");
                ink.layer = 31;
                ink.transform.SetParent(piece.transform, false);
                ink.AddComponent<MeshFilter>().sharedMesh = sourceMesh.sharedMesh;
                MeshRenderer inkRenderer = ink.AddComponent<MeshRenderer>();
                Material[] inkSlots = new Material[sourceMesh.sharedMesh.subMeshCount];
                for (int j = 0; j < inkSlots.Length; j++) inkSlots[j] = outline;
                inkRenderer.sharedMaterials = inkSlots;
                inkRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                inkRenderer.receiveShadows = false;
            }
            count++;
        }
        if (ruined) RuinedSalvagePreview.AddDamage(room, door.transform.forward, folder, shader);
        GameObject keyObject = new GameObject("Soft Ivory Key");
        Light key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.cullingMask = 1 << 31;
        key.color = new Color(1f, 0.96f, 0.89f);
        key.intensity = 0.85f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.4f;
        key.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
        GameObject cameraObject = new GameObject("Preview Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = ruined ? new Color(0.12f, 0.17f, 0.15f) : illustrated ? new Color(0.64f, 0.70f, 0.64f) : new Color(0.40f, 0.53f, 0.70f);
        camera.fieldOfView = 60f;
        camera.cullingMask = 1 << 31;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 60f;
        camera.transform.position = door.transform.forward * 5f + Vector3.up * 0.35f;
        camera.transform.LookAt(Vector3.zero);
        RenderTexture targetTexture = new RenderTexture(1280, 720, 24);
        RenderTexture previousTarget = RenderTexture.active;
        bool previousAsync = ShaderUtil.allowAsyncCompilation;
        Texture2D capture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var sourceVisibility = new Dictionary<MeshRenderer, bool>();
        foreach (MeshRenderer renderer in renderers)
        {
            sourceVisibility[renderer] = renderer.enabled;
            renderer.enabled = false;
        }
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            camera.targetTexture = targetTexture;
            camera.Render();
            RenderTexture.active = targetTexture;
            capture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            capture.Apply();
            File.WriteAllBytes("CampaignBackups/" + label + "-preview.png", capture.EncodeToPNG());
        }
        finally
        {
            foreach (var entry in sourceVisibility)
                if (entry.Key != null) entry.Key.enabled = entry.Value;
            camera.targetTexture = null;
            RenderTexture.active = previousTarget;
            ShaderUtil.allowAsyncCompilation = previousAsync;
            Object.DestroyImmediate(capture);
            targetTexture.Release();
            Object.DestroyImmediate(targetTexture);
        }
        AssetDatabase.SaveAssets();
        string path = ruined ? "Assets/Scenes/RuinedSalvage_StylePreview.unity" : illustrated ? "Assets/Scenes/IllustratedSalvage_StylePreview.unity" : bold ? "Assets/Scenes/Fortnite_StylePreview.unity" : AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/" + label + "_StylePreview.unity");
        EditorSceneManager.SaveScene(preview, path);
        SceneManager.SetActiveScene(source);
        // Keep this isolated study out of the user's running level.
        EditorSceneManager.CloseScene(preview, true);
        File.WriteAllText(ruined ? "Library/RuinedSalvagePreview.txt" : illustrated ? "Library/IllustratedSalvagePreview.txt" : bold ? "Library/BoldAdventurePreview.txt" : Marker, path + "\n" + count + " meshes; " + materials.Count + " materials.\n");
        if (bold) File.WriteAllText("Library/BoldAdventurePreview2.txt", path);
        Debug.Log("Painted adventure preview saved: " + path + " (" + count + " meshes). Open it to compare; source scene unchanged.");
    }
}
