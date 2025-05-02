using UnityEngine;

namespace GrassInteraction
{
    [CreateAssetMenu(fileName = "TerrainInteractionSettings", menuName = "Settings/Terrain Interaction Settings")]
    public class TerrainInteractionSettings : ScriptableObject
    {
        [Header("テクスチャ設定")]
        public int textureResolution = 256;
        public string heatmapTexPropName = "_PosisionHeatmap";

        [Header("インタラクション設定")]
        public int interactionCount = 128;
        public float trailDuration = 0.2f;
        public float positionUpdateThreshold = 0.1f;
    }
}