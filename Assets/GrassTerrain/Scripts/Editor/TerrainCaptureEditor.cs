using UnityEngine;
using UnityEditor;
using System.IO;

public class TerrainCaptureEditor : EditorWindow
{
    private Terrain terrain;
    private int resolution = 1024;


    [MenuItem("Tools/Terrain Topdown Capture")]
    public static void ShowWindow()
    {
        GetWindow<TerrainCaptureEditor>("Terrain Capture Tool");
    }

    void OnGUI()
    {
        GUILayout.Label("📷 Terrain Topdown Capture", EditorStyles.boldLabel);

        terrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", terrain, typeof(Terrain), true);
        resolution = EditorGUILayout.IntPopup("Resolution", resolution, new[] { "512", "1024", "2048", "4096" }, new[] { 512, 1024, 2048, 4096 });

        if (GUILayout.Button("🖼️ Bake and Save Terrain Image"))
        {
            if (terrain == null)
            {
                Debug.LogError("❌ Please assign a Terrain.");
                return;
            }

            CaptureTerrainImage(terrain, resolution);
        }
    }

    private void CaptureTerrainImage(Terrain targetTerrain, int res)
    {
        // 📐 テレイン情報取得
        var data = targetTerrain.terrainData;
        Vector3 terrainSize = data.size;
        Vector3 terrainCenter = targetTerrain.transform.position + terrainSize / 2f;

        // 🎥 一時カメラ作成
        GameObject camGO = new GameObject("TempTopdownCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(terrainSize.x, terrainSize.z) / 2f;
        cam.transform.position = new Vector3(terrainCenter.x, 200f, terrainCenter.z);
        cam.transform.rotation = Quaternion.Euler(90f, 0, 0);
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.backgroundColor = Color.gray;
        cam.cullingMask = LayerMask.GetMask("Terrain");

        // 🖼️ RenderTexture 設定
        RenderTexture rt = new RenderTexture(res, res, 24);
        cam.targetTexture = rt;

        // 📷 描画
        cam.Render();

        // 🧱 ベイク処理
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, res, res), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        // 💾 保存
        string path = "Assets/TerrainCapture.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();
        Debug.Log("✅ Terrain image saved to: " + path);

        // 🧹 後始末
        Object.DestroyImmediate(camGO);
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}
