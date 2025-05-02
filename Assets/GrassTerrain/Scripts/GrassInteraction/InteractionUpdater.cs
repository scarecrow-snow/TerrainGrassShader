using System;
using System.Collections.Generic;
using UnityEngine;
namespace GrassInteraction
{
    // インターフェースを定義
    public interface ITerrainInteractionProvider
    {
        Vector2[] GetInteractionPositionBuffer();
        int GetActiveInteractionCount();
    }

    public class InteractionUpdater : MonoBehaviour, ITerrainInteractionProvider
    {
        [SerializeField] Material material;
        [SerializeField] TerrainInteractionSettings settings;
        [SerializeField] List<Transform> interactionObjects = new List<Transform>();
        [SerializeField] Vector2[] interactionPositionBuffer;


        int activeInteractionCount = 0;
        Terrain terrain;

        // 位置と時間を保持する構造体
        private struct PositionData
        {
            public Vector2 position;
            public float timestamp;
            public int objectID;  // オブジェクトを識別するためのID

            public PositionData(Vector2 pos, float time, int id)
            {
                position = pos;
                timestamp = time;
                objectID = id;
            }
        }

        // すべての位置履歴を保持するリスト（キューではなくリストに変更）
        private List<PositionData> positionHistory = new List<PositionData>();
        // オブジェクトIDを管理するための辞書
        private Dictionary<Transform, int> objectIDMap = new Dictionary<Transform, int>();


        // 事前に確保する作業用バッファ
        private List<PositionData> currentPositionsBuffer = new List<PositionData>();
        private List<PositionData> trailPositionsBuffer = new List<PositionData>();
        private HashSet<int> currentObjectIDsBuffer = new HashSet<int>();
        private int nextID = 0;

        void Start()
        {
            interactionPositionBuffer = new Vector2[settings.interactionCount];
            terrain = GetComponent<Terrain>();
        }


        void Update()
        {
            var feature = HeatmapRenderFeature.Instance;
            if (feature == null) return;

            var texture = feature.GetHeatmapTexture();
            if (texture == null) return;

            if (terrain == null) return;

            feature.SetTerrainInteractionProvider(this);

            float currentTime = Time.time;

            UpdatePositionHistory(currentTime);
            CleanupOldPositions(currentTime);
            UpdateBuffers();

            material.SetTexture(settings.heatmapTexPropName, texture);
        }

        private void UpdatePositionHistory(float currentTime)
        {
            // 現在のオブジェクトの位置を更新または追加
            foreach (var obj in interactionObjects)
            {
                // オブジェクトのIDを取得または作成
                if (!objectIDMap.TryGetValue(obj, out int id))
                {
                    id = nextID++;
                    objectIDMap[obj] = id;
                }

                Vector2 texCoord = WorldPosToTextureCoord(obj.position, settings.textureResolution);

                // このオブジェクトの最新の位置を探す
                int existingIndex = -1;
                for (int i = 0; i < positionHistory.Count; i++)
                {
                    if (positionHistory[i].objectID == id)
                    {
                        existingIndex = i;
                        break;
                    }
                }

                // 既存のデータがあり、かつ位置が大きく変わっていない場合は、タイムスタンプだけ更新
                if (existingIndex >= 0)
                {
                    var existing = positionHistory[existingIndex];
                    if (Vector2.Distance(existing.position, texCoord) < settings.positionUpdateThreshold)
                    {
                        positionHistory[existingIndex] = new PositionData(existing.position, currentTime, id);
                    }
                    else
                    {
                        // 古い位置を残しつつ、新しい位置も追加
                        positionHistory.Add(new PositionData(texCoord, currentTime, id));
                    }
                }
                else
                {
                    // 新規追加
                    positionHistory.Add(new PositionData(texCoord, currentTime, id));
                }
            }
        }

        private void CleanupOldPositions(float currentTime)
        {
            // 古いデータを削除
            positionHistory.RemoveAll(data => currentTime - data.timestamp > settings.trailDuration);
        }

        private void UpdateBuffers()
        {
            // バッファをクリア
            currentPositionsBuffer.Clear();
            trailPositionsBuffer.Clear();
            currentObjectIDsBuffer.Clear();

            // 現在のオブジェクトのIDリストを作成
            CollectCurrentObjectIDs();

            // 現在のオブジェクトの最新位置と軌跡を分ける
            SeparateCurrentPositionsAndTrails();

            // バッファに現在の位置を優先して配置
            FillInteractionBuffer();
        }

        private void CollectCurrentObjectIDs()
        {
            foreach (var obj in interactionObjects)
            {
                if (objectIDMap.TryGetValue(obj, out int id))
                {
                    currentObjectIDsBuffer.Add(id);
                }
            }
        }

        private void SeparateCurrentPositionsAndTrails()
        {
            foreach (var data in positionHistory)
            {
                if (currentObjectIDsBuffer.Contains(data.objectID))
                {
                    // 各オブジェクトの最新の位置を見つける
                    bool isLatest = true;
                    foreach (var other in positionHistory)
                    {
                        if (other.objectID == data.objectID && other.timestamp > data.timestamp)
                        {
                            isLatest = false;
                            break;
                        }
                    }

                    if (isLatest)
                    {
                        currentPositionsBuffer.Add(data);
                    }
                    else
                    {
                        trailPositionsBuffer.Add(data);
                    }
                }
                else
                {
                    // 現在のオブジェクトではないものは全て軌跡として扱う
                    trailPositionsBuffer.Add(data);
                }
            }
        }

        private void FillInteractionBuffer()
        {
            int index = 0;

            // まず現在の位置を配置
            foreach (var data in currentPositionsBuffer)
            {
                if (index < settings.interactionCount)
                {
                    interactionPositionBuffer[index] = data.position;
                    index++;
                }
            }

            // 次に軌跡を配置
            foreach (var data in trailPositionsBuffer)
            {
                if (index < settings.interactionCount)
                {
                    interactionPositionBuffer[index] = data.position;
                    index++;
                }
            }

            activeInteractionCount = index;
        }

        void OnCollisionEnter(Collision other)
        {
            if (interactionObjects.Contains(other.transform)) return;

            interactionObjects.Add(other.transform);
        }

        void OnCollisionExit(Collision other)
        {
            if (!interactionObjects.Contains(other.transform)) return;

            interactionObjects.Remove(other.transform);
        }

        Vector2 WorldPosToTextureCoord(Vector3 worldPos, int textureResolution)
        {
            // Terrainの原点（左手前下）を取得
            Vector3 terrainOrigin = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;

            // オブジェクトの座標をTerrainのローカル空間に変換
            Vector3 localPos = worldPos - terrainOrigin;

            // [0,1] に正規化されたUV座標を求める
            float u = Mathf.Clamp01(localPos.x / terrainSize.x);
            float v = Mathf.Clamp01(localPos.z / terrainSize.z); // Z = 奥行き方向

            // テクスチャ解像度に変換（テクスチャの上下反転を考慮するなら 1 - v）
            int texX = Mathf.FloorToInt(u * (textureResolution - 1));
            int texY = Mathf.FloorToInt(v * (textureResolution - 1));

            return new Vector2(texX, texY);
        }

        public Vector2[] GetInteractionPositionBuffer()
        {
            return interactionPositionBuffer;
        }

        public int GetInteractionCount()
        {
            return settings.interactionCount;
        }

        public int GetActiveInteractionCount()
        {
            return activeInteractionCount;
        }
    }
}