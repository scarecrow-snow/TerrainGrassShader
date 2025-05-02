using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

namespace GrassInteraction
{


    // ヒートマップを描画するためのレンダーフィーチャー
    public class HeatmapRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] ComputeShader computeShader; // ヒートマップ計算用のコンピュートシェーダー
        [SerializeField] TerrainInteractionSettings settings; // 共通設定
        HeatmapPass pass;

        public static HeatmapRenderFeature Instance { get; private set; } // シングルトンインスタンス

        // ヒートマップの描画パス
        class HeatmapPass : ScriptableRenderPass
        {
            ComputeShader computeShader;
            int kernel;
            TerrainInteractionSettings settings;

            GraphicsBuffer interactionBuffer; // インタラクション位置を格納するバッファ
            int activeInteractionCount = 0; // アクティブなインタラクション数

            RTHandle heatmapHandle; // ヒートマップ用のレンダーテクスチャハンドル

            public RTHandle Heatmap => heatmapHandle;

            public ITerrainInteractionProvider terraInInteractionProvider; // 地形インタラクションプロバイダー

            // パスのセットアップ
            public void Setup(ComputeShader cs, TerrainInteractionSettings terrainSettings)
            {
                computeShader = cs;
                settings = terrainSettings;
                kernel = cs.FindKernel("CSMain");

                // ヒートマップのレンダーテクスチャを初期化
                if (heatmapHandle == null || heatmapHandle.rt.width != settings.textureResolution || heatmapHandle.rt.height != settings.textureResolution)
                {
                    heatmapHandle?.Release();

                    var desc = new RenderTextureDescriptor(settings.textureResolution, settings.textureResolution, GraphicsFormat.R32_SFloat, 0)
                    {
                        enableRandomWrite = true,
                        msaaSamples = 1,
                        sRGB = false,
                        useMipMap = false,
                    };

                    heatmapHandle = RTHandles.Alloc(desc, name: "_HeatmapRT");
                }

                // インタラクションバッファの初期化
                if (interactionBuffer == null || interactionBuffer.count != settings.interactionCount)
                {
                    interactionBuffer?.Release();
                    interactionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, settings.interactionCount, sizeof(float) * 2);
                }
            }

            // レンダーグラフのパスデータ
            class PassData
            {
                public ComputeShader compute;
                public int kernel;
                public TextureHandle output;
                public BufferHandle interactionHandle;
                public int interactionCount;
                public int activeInteractionCount;
            }

            // レンダーグラフに記録
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer context)
            {
                UpdateInteractionBuffer();

                TextureHandle texHandle = graph.ImportTexture(heatmapHandle);
                BufferHandle interactionHandle = graph.ImportBuffer(interactionBuffer);

                using IComputeRenderGraphBuilder builder = graph.AddComputePass("HeatmapPass", out PassData data);
                data.compute = computeShader;
                data.kernel = kernel;
                data.output = texHandle;
                data.interactionHandle = interactionHandle;
                data.interactionCount = settings.interactionCount;
                data.activeInteractionCount = activeInteractionCount;

                builder.UseTexture(texHandle, AccessFlags.Write);
                builder.UseBuffer(interactionHandle, AccessFlags.Read);

                // コンピュートシェーダーのディスパッチ設定
                builder.SetRenderFunc((PassData d, ComputeGraphContext ctx) =>
                {
                    ctx.cmd.SetComputeIntParam(d.compute, "interactionCount", d.interactionCount);
                    ctx.cmd.SetComputeBufferParam(d.compute, d.kernel, "interactionPositions", d.interactionHandle);
                    ctx.cmd.SetComputeIntParam(d.compute, "activeInteractionCount", d.activeInteractionCount);
                    ctx.cmd.SetComputeTextureParam(d.compute, d.kernel, "heatmapTexture", d.output);

                    ctx.cmd.DispatchCompute(d.compute, d.kernel, Mathf.CeilToInt(settings.textureResolution / 8f), Mathf.CeilToInt(settings.textureResolution / 8f), 1);
                });
            }

            // インタラクションバッファの更新
            void UpdateInteractionBuffer()
            {
                if (terraInInteractionProvider == null) return;

                Vector2[] positions = terraInInteractionProvider.GetInteractionPositionBuffer();
                activeInteractionCount = terraInInteractionProvider.GetActiveInteractionCount();

                if (activeInteractionCount > settings.interactionCount)
                {
                    activeInteractionCount = settings.interactionCount;
                }

                interactionBuffer.SetData(positions);
            }

            // リソースのクリーンアップ
            public void Cleanup()
            {
                heatmapHandle?.Release();
                heatmapHandle = null;

                interactionBuffer?.Release();
                interactionBuffer = null;
            }
        }

        // フィーチャーの作成
        public override void Create()
        {
            pass = new HeatmapPass()
            {
                renderPassEvent = RenderPassEvent.BeforeRendering
            };

            Instance = this;
        }

        // レンダーパスの追加
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!SystemInfo.supportsComputeShaders || computeShader == null || settings == null) return;

            pass.Setup(computeShader, settings);
            renderer.EnqueuePass(pass);
        }

        // ヒートマップテクスチャの取得メソッド
        public RTHandle GetHeatmapTexture() => pass?.Heatmap;
        // 地形インタラクションプロバイダーの設定メソッド
        public void SetTerrainInteractionProvider(ITerrainInteractionProvider provider) => pass.terraInInteractionProvider = provider;
        // リソースの破棄
        protected override void Dispose(bool disposing)
        {
            // GPU バッファのクリーンアップ
            pass?.Cleanup();
        }
    }
}