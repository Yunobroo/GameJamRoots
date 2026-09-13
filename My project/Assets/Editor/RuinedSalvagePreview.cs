using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RuinedSalvagePreview
{
    static RuinedSalvagePreview() { EditorApplication.update += BuildOnce; }
    private static void BuildOnce()
    {
        if (File.Exists("Library/RuinedSalvagePreview2.txt")) { EditorApplication.update -= BuildOnce; return; }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || PrefabStageUtility.GetCurrentPrefabStage() != null) return;
        Scene source = SceneManager.GetActiveScene();
        if (source.path != "Assets/Scenes/Second Growth_Area1 1.unity" && source.path != "Assets/Scenes/IllustratedSalvage_StylePreview.unity") return;
        Shader shader = Shader.Find("Roots/RuinedSalvage");
        if (shader == null || ShaderUtil.ShaderHasError(shader) || Shader.Find("Roots/IllustratedOutline") == null) return;
        PaintedAdventurePreview.Build(source, shader, false, true, true);
        File.WriteAllText("Library/RuinedSalvagePreview2.txt", "Rendered corrected fallen panel.");
        EditorApplication.update -= BuildOnce;
    }

    public static void AddDamage(GameObject room, Vector3 forward, string folder, Shader shader)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float floor = -1.7f;
        MeshRenderer[] pieces = room.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer piece in pieces)
            if (piece.name.Contains("Door_Left")) { floor = piece.bounds.min.y; break; }
        foreach (MeshRenderer piece in pieces)
        {
            if (piece.name.Contains("Door_Left"))
            {
                Vector3 pivot = piece.bounds.center;
                piece.transform.RotateAround(pivot, right, 85f);
                piece.transform.RotateAround(pivot, forward, -16f);
                piece.transform.localScale *= 0.75f;
                piece.transform.position += forward * 1.2f - right * 0.8f;
                float bottom = float.PositiveInfinity;
                foreach (Vector3 vertex in piece.GetComponent<MeshFilter>().sharedMesh.vertices)
                    bottom = Mathf.Min(bottom, piece.transform.TransformPoint(vertex).y);
                piece.transform.position += Vector3.up * (floor + 0.12f - bottom);
            }
            else if (piece.name.Contains("Door_Right"))
            {
                piece.transform.RotateAround(piece.bounds.center, forward, 9f);
                piece.transform.position += right * 0.4f;
            }
        }
        Material rubble = new Material(shader) { name = "Broken concrete and paint" };
        rubble.SetColor("_PaintColor", new Color(0.43f, 0.41f, 0.32f));
        AssetDatabase.CreateAsset(rubble, AssetDatabase.GenerateUniqueAssetPath("Assets/" + folder + "/Rubble.mat"));
        Mesh chunk = new Mesh { name = "Jagged salvage chunk" };
        chunk.vertices = new[] { new Vector3(-.5f,-.4f,-.4f),new Vector3(.4f,-.5f,-.5f),new Vector3(.5f,-.4f,.3f),new Vector3(-.4f,-.5f,.5f),new Vector3(-.3f,.4f,-.5f),new Vector3(.5f,.25f,-.3f),new Vector3(.3f,.5f,.5f),new Vector3(-.5f,.3f,.3f) };
        chunk.triangles = new[] {0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};
        chunk.RecalculateNormals(); chunk.RecalculateBounds();
        AssetDatabase.CreateAsset(chunk, AssetDatabase.GenerateUniqueAssetPath("Assets/" + folder + "/Debris.asset"));
        System.Random random = new System.Random(81);
        for (int i = 0; i < 27; i++)
        {
            GameObject debris = new GameObject("Fallen fragment " + i); debris.layer = 31;
            debris.transform.SetParent(room.transform);
            float x = (float)random.NextDouble()*4.2f-2.1f;
            float z = (float)random.NextDouble()*2.7f+0.7f;
            float size = 0.12f+(float)random.NextDouble()*0.45f;
            debris.transform.position = right*x + forward*z + Vector3.up*(floor+size*0.25f);
            debris.transform.rotation = Quaternion.Euler((float)random.NextDouble()*30,(float)random.NextDouble()*360,12);
            debris.transform.localScale = new Vector3(size*1.6f,size*0.6f,size);
            debris.AddComponent<MeshFilter>().sharedMesh = chunk;
            debris.AddComponent<MeshRenderer>().sharedMaterial = rubble;
        }
        Material cable = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        cable.SetColor("_BaseColor", new Color(0.035f,0.045f,0.04f));
        AssetDatabase.CreateAsset(cable, AssetDatabase.GenerateUniqueAssetPath("Assets/" + folder + "/Cable.mat"));
        for (int j=0; j<3; j++)
        {
            GameObject hanging = new GameObject("Exposed hanging cable " + j); hanging.layer=31;
            hanging.transform.SetParent(room.transform);
            LineRenderer line = hanging.AddComponent<LineRenderer>();
            line.sharedMaterial=cable; line.widthMultiplier=0.025f+j*0.009f; line.positionCount=12;
            for (int k=0;k<12;k++)
            {
                float t=k/11f;
                line.SetPosition(k,right*(-1.8f+j*0.25f+Mathf.Sin(t*4)*0.22f)+forward*(0.4f+t*0.25f)+Vector3.up*(floor+3.5f-t*(1.5f+j*.22f)));
            }
        }
    }
}
