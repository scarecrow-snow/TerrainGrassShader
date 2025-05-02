using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace UnityUtils
{
    public class PrefabExporter : EditorWindow
    {
        private List<Object> selectedPrefabs = new List<Object>();
        private string exportPath = "ExportedPrefabPackage.unitypackage";

        [MenuItem("Tools/Prefab Exporter with Dependencies")]
        static void Init()
        {
            GetWindow<PrefabExporter>("Prefab Exporter");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("🎯 選択中のプレファブをエクスポート");

            if (GUILayout.Button("選択中のプレファブを追加"))
            {
                AddSelectedPrefabs();
            }

            if (selectedPrefabs.Count > 0)
            {
                EditorGUILayout.LabelField("📋 選択されたプレファブ一覧:");

                for (int i = 0; i < selectedPrefabs.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    selectedPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                        selectedPrefabs[i],
                        typeof(GameObject),
                        false
                    );

                    if (GUILayout.Button("❌", GUILayout.Width(30)))
                    {
                        selectedPrefabs.RemoveAt(i);
                        i--; // インデックスを戻して安全にループ継続
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();
            exportPath = EditorGUILayout.TextField("📁 出力ファイル名", exportPath);

            if (GUILayout.Button("📦 エクスポート実行"))
            {
                ExportWithDependencies();
            }
        }

        private void AddSelectedPrefabs()
        {
            foreach (var obj in Selection.objects)
            {
                if (PrefabUtility.IsPartOfPrefabAsset(obj) && !selectedPrefabs.Contains(obj))
                {
                    selectedPrefabs.Add(obj);
                }
            }
        }

        private void ExportWithDependencies()
        {
            if (selectedPrefabs.Count == 0)
            {
                Debug.LogWarning("❗プレファブが選択されていません。");
                return;
            }

            List<string> allAssetPaths = new List<string>();

            foreach (var prefab in selectedPrefabs)
            {
                string path = AssetDatabase.GetAssetPath(prefab);
                if (!string.IsNullOrEmpty(path))
                {
                    string[] dependencies = AssetDatabase.GetDependencies(path, true);
                    allAssetPaths.AddRange(dependencies);
                }
            }

            // 重複削除
            HashSet<string> uniquePaths = new HashSet<string>(allAssetPaths);

            // パス選択
            string finalPath = EditorUtility.SaveFilePanel("エクスポート先を指定", "", exportPath, "unitypackage");
            if (!string.IsNullOrEmpty(finalPath))
            {
                AssetDatabase.ExportPackage(
                    new List<string>(uniquePaths).ToArray(),
                    finalPath,
                    ExportPackageOptions.Interactive
                );

                Debug.Log($"✅ プレファブとその依存アセットをエクスポートしました！\nパス: {finalPath}");
            }
        }
    }
}