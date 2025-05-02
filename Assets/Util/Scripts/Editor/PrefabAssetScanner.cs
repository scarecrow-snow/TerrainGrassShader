using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UnityUtils
{
    public class PrefabAssetScanner : EditorWindow
    {
        private GameObject prefab;
        private Vector2 scroll;

        private List<Mesh> meshes = new List<Mesh>();
        private List<Material> materials = new List<Material>();
        private List<Texture> textures = new List<Texture>();
        private List<Shader> shaders = new List<Shader>();

        [MenuItem("Tools/Prefab Asset Scanner")]
        static void ShowWindow()
        {
            GetWindow<PrefabAssetScanner>("Prefab Asset Scanner");
        }

        private void OnGUI()
        {
            prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

            if (GUILayout.Button("Scan Prefab"))
            {
                ScanPrefab();
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (meshes.Count > 0)
            {
                EditorGUILayout.LabelField("🧱 Meshes:");
                foreach (var mesh in meshes)
                    EditorGUILayout.ObjectField(mesh, typeof(Mesh), false);
            }

            if (materials.Count > 0)
            {
                EditorGUILayout.LabelField("🎨 Materials:");
                foreach (var mat in materials)
                    EditorGUILayout.ObjectField(mat, typeof(Material), false);
            }

            if (textures.Count > 0)
            {
                EditorGUILayout.LabelField("🖼 Textures:");
                foreach (var tex in textures)
                    EditorGUILayout.ObjectField(tex, typeof(Texture), false);
            }

            if (shaders.Count > 0)
            {
                EditorGUILayout.LabelField("🧪 Shaders:");
                foreach (var shader in shaders)
                    EditorGUILayout.ObjectField(shader, typeof(Shader), false);
            }

            EditorGUILayout.EndScrollView();
        }

        private void ScanPrefab()
        {
            meshes.Clear();
            materials.Clear();
            textures.Clear();
            shaders.Clear();

            if (prefab == null) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            var meshFilters = instance.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh != null && !meshes.Contains(mf.sharedMesh))
                    meshes.Add(mf.sharedMesh);
            }

            var skinnedRenderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in skinnedRenderers)
            {
                if (smr.sharedMesh != null && !meshes.Contains(smr.sharedMesh))
                    meshes.Add(smr.sharedMesh);
            }

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null) continue;

                    if (!materials.Contains(mat))
                        materials.Add(mat);

                    if (mat.shader != null && !shaders.Contains(mat.shader))
                        shaders.Add(mat.shader);

                    Shader shader = mat.shader;
                    int count = ShaderUtil.GetPropertyCount(shader);
                    for (int i = 0; i < count; i++)
                    {
                        if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                        {
                            string propName = ShaderUtil.GetPropertyName(shader, i);
                            Texture tex = mat.GetTexture(propName);
                            if (tex != null && !textures.Contains(tex))
                                textures.Add(tex);
                        }
                    }
                }
            }

            DestroyImmediate(instance);
        }
    }
}