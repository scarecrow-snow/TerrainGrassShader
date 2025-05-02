using UnityEngine;

public class AutoMover : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float terrainCheckDistance = 10f;
    [SerializeField] private LayerMask terrainLayer;
    
    private Vector3 moveDirection;
    private float changeDirectionTimer;
    [SerializeField] private float directionChangeInterval = 3f;
    
    [Header("境界設定")]
    [SerializeField] private Transform terrainTransform;
    [SerializeField] private float terrainSizeX = 100f;
    [SerializeField] private float terrainSizeZ = 100f;
    [SerializeField] private float boundaryBuffer = 5f; // 境界からの余白

    private void Start()
    {
        // 初期移動方向をランダムに設定
        ChangeDirection();
        
        // テレイントランスフォームが設定されていない場合は警告
        if (terrainTransform == null)
        {
            Debug.LogWarning("テレイントランスフォームが設定されていません。境界チェックが正しく機能しない可能性があります。");
        }
    }

    private void Update()
    {
        // 定期的に方向を変更
        changeDirectionTimer -= Time.deltaTime;
        if (changeDirectionTimer <= 0)
        {
            ChangeDirection();
            changeDirectionTimer = directionChangeInterval;
        }

        // 前方に障害物があるか確認
        if (Physics.Raycast(transform.position, transform.forward, terrainCheckDistance, terrainLayer))
        {
            // 障害物があれば方向を変更
            ChangeDirection();
        }

        // テレインの境界をチェック
        if (terrainTransform != null)
        {
            CheckTerrainBoundaries();
        }

        // 移動と回転
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        transform.Rotate(Vector3.up, moveDirection.x * rotationSpeed * Time.deltaTime);

        // テレインの高さに合わせる
        AdjustToTerrainHeight();
    }

    private void ChangeDirection()
    {
        // ランダムな方向を設定（-1〜1の範囲）
        moveDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        
        // オブジェクトを移動方向に向ける
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
        
        changeDirectionTimer = directionChangeInterval;
    }

    private void CheckTerrainBoundaries()
    {
        // テレインの左手前を基準とした境界計算（Terrainのピボットは左下）
        Vector3 terrainOrigin = terrainTransform.position;
        Vector3 minBounds = terrainOrigin + new Vector3(boundaryBuffer, 0, boundaryBuffer);
        Vector3 maxBounds = terrainOrigin + new Vector3(terrainSizeX - boundaryBuffer, 0, terrainSizeZ - boundaryBuffer);
        Vector3 terrainCenter = terrainOrigin + new Vector3(terrainSizeX/2, 0, terrainSizeZ/2);
        
        // 次の位置を計算
        Vector3 nextPosition = transform.position + transform.forward * moveSpeed * Time.deltaTime;
        
        // 方向転換が必要かチェック
        bool needsDirectionChange = false;
        Vector3 newDirection = Vector3.zero;
        
        // 境界外に出ようとしている場合、中央に向かう方向を設定
        if (nextPosition.x < minBounds.x || nextPosition.x > maxBounds.x || 
            nextPosition.z < minBounds.z || nextPosition.z > maxBounds.z)
        {
            needsDirectionChange = true;
            
            // 現在位置からテレイン中央へのベクトルを計算
            Vector3 directionToCenter = (terrainCenter - transform.position).normalized;
            
            // 中央方向をベースに、少しランダム性を加える
            float randomAngle = Random.Range(-30f, 30f);
            newDirection = Quaternion.Euler(0, randomAngle, 0) * directionToCenter;
        }
        
        // 方向転換が必要な場合
        if (needsDirectionChange)
        {
            moveDirection = newDirection.normalized;
            transform.rotation = Quaternion.LookRotation(moveDirection);
            
            // デバッグ表示
            Debug.DrawRay(transform.position, newDirection * 5f, Color.yellow, 1.0f);
        }
    }

    private void AdjustToTerrainHeight()
    {
        RaycastHit hit;
        // 下方向にレイを飛ばして地面を検出
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 100f, terrainLayer))
        {
            // 地面の高さに位置を調整（少し浮かせる）
            Vector3 newPosition = transform.position;
            newPosition.y = hit.point.y + 0.5f; // 地面から少し浮かせる
            transform.position = newPosition;
        }
    }

    private void OnDrawGizmos()
    {
        // デバッグ用の視覚化
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * terrainCheckDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + Vector3.up, Vector3.down * 100f);
        
        // 境界の視覚化（テレイントランスフォームが設定されている場合）
        if (terrainTransform != null)
        {
            // テレインの左手前を基準とした境界表示
            Vector3 terrainOrigin = terrainTransform.position;
            Vector3 terrainSize = new Vector3(terrainSizeX, 2, terrainSizeZ);
            Vector3 terrainCenter = terrainOrigin + new Vector3(terrainSizeX/2, 1, terrainSizeZ/2);
            
            // テレイン全体の境界
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(terrainCenter, terrainSize);
            
            // 有効な移動範囲（バッファ適用後）
            Vector3 bufferedSize = new Vector3(terrainSizeX - boundaryBuffer * 2, 2, terrainSizeZ - boundaryBuffer * 2);
            Vector3 bufferedCenter = terrainOrigin + new Vector3(terrainSizeX/2, 1, terrainSizeZ/2);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(bufferedCenter, bufferedSize);
            
            // テレインの基準点（左手前）を表示
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(terrainOrigin, 1f);
        }
    }
} 