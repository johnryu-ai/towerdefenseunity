using System.Collections.Generic;
using UnityEngine;
using TDF.Core.Data;
using TDF.Runtime.Managers;

namespace TDF.Runtime.Map
{
    public class MapController : MonoBehaviour
    {
        public static MapController Instance { get; private set; }

        [Header("References")]
        public GameObject tilePrefab; // SpriteRenderer가 있는 기본 빈 타일 프리팹
        public Transform tileContainer; // 생성된 타일들을 묶어둘 부모 Transform

        private MapData currentMapData;
        private GameObject[,] spawnedTiles;

        public const float TILE_SIZE = 1f; // 타일 한 칸의 월드 사이즈 (유닛)
        public float offsetX { get; private set; }
        public float offsetY { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // 모바일 16:9 FHD(1920x1080) 대응: 1타일 135px -> 세로 8칸
            if (Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 4f; 
                Camera.main.backgroundColor = new Color(0.2f, 0.2f, 0.2f); // 남는 영역 회색
            }
        }

        public void GenerateMap(MapData mapData)
        {
            if (mapData == null || tilePrefab == null)
            {
                Debug.LogError("MapController: MapData 또는 TilePrefab이 없습니다.");
                return;
            }

            currentMapData = mapData;
            spawnedTiles = new GameObject[mapData.gridWidth, mapData.gridHeight];

            // 기존 타일 초기화
            if (tileContainer == null)
            {
                tileContainer = new GameObject("TileContainer").transform;
                tileContainer.SetParent(this.transform);
            }
            foreach (Transform child in tileContainer)
            {
                Destroy(child.gameObject);
            }

            // 중심을 맞추기 위한 오프셋 계산 (화면 중앙에 맵 배치)
            offsetX = -mapData.gridWidth * TILE_SIZE / 2f + (TILE_SIZE / 2f);
            offsetY = mapData.gridHeight * TILE_SIZE / 2f - (TILE_SIZE / 2f);

            for (int y = 0; y < mapData.gridHeight; y++)
            {
                for (int x = 0; x < mapData.gridWidth; x++)
                {
                    Vector3 position = new Vector3(offsetX + (x * TILE_SIZE), offsetY - (y * TILE_SIZE), 0);
                    GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, tileContainer);
                    tileObj.name = $"Tile_{x}_{y}";
                    
                    TileType type = mapData.GetTileAt(x, y);
                    SpriteRenderer sr = tileObj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        // TODO: 실제 프로젝트에서는 type에 따라 다른 Sprite 할당 로직이 들어갑니다.
                        // mapData.tileSprite 등을 사용할 수 있습니다.
                        sr.color = GetColorForTile(type); // 임시 시각화용 색상
                    }

                    spawnedTiles[x, y] = tileObj;
                }
            }

            // 배경 이미지 세팅
            if (mapData.backgroundSprite != null)
            {
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(this.transform);
                // 맵 크기에 맞게 조절
                bgObj.transform.position = Vector3.zero;
                SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
                bgSr.sprite = mapData.backgroundSprite;
                bgSr.sortingOrder = -10; // 타일보다 뒤에 그려지도록
            }
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            if (spawnedTiles != null && x >= 0 && x < currentMapData.gridWidth && y >= 0 && y < currentMapData.gridHeight)
            {
                return spawnedTiles[x, y].transform.position;
            }
            return Vector3.zero;
        }

        public void UpdateTileColor(int x, int y)
        {
            if (spawnedTiles != null && x >= 0 && x < currentMapData.gridWidth && y >= 0 && y < currentMapData.gridHeight)
            {
                var sr = spawnedTiles[x, y].GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = GetColorForTile(currentMapData.GetTileAt(x, y));
            }
        }

        public List<Vector2> GetPath()
        {
            List<Vector2> path = new List<Vector2>();
            if (currentMapData == null) return path;

            int gridW = currentMapData.gridWidth;
            int gridH = currentMapData.gridHeight;

            int spawnX = -1, spawnY = -1;
            int baseX = -1, baseY = -1;

            for (int y = 0; y < gridH; y++)
            {
                for (int x = 0; x < gridW; x++)
                {
                    TileType t = currentMapData.GetTileAt(x, y);
                    if (t == TileType.Spawn) { spawnX = x; spawnY = y; }
                    else if (t == TileType.Base) { baseX = x; baseY = y; }
                }
            }

            if (spawnX == -1 || baseX == -1) return path;

            // 1. 기지(Base)로부터 각 타일까지의 최단 거리 계산 (BFS)
            int[,] distances = new int[gridW, gridH];
            for (int x = 0; x < gridW; x++)
                for (int y = 0; y < gridH; y++)
                    distances[x, y] = int.MaxValue;

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(baseX, baseY));
            distances[baseX, baseY] = 0;

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                Vector2Int curr = queue.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    int nx = curr.x + dx[i];
                    int ny = curr.y + dy[i];
                    if (nx >= 0 && nx < gridW && ny >= 0 && ny < gridH)
                    {
                        TileType type = currentMapData.GetTileAt(nx, ny);
                        if ((type == TileType.Path || type == TileType.Spawn) && distances[nx, ny] == int.MaxValue)
                        {
                            distances[nx, ny] = distances[curr.x, curr.y] + 1;
                            queue.Enqueue(new Vector2Int(nx, ny));
                        }
                    }
                }
            }

            // 2. 몬스터 이동 시뮬레이션
            Vector2Int currentPos = new Vector2Int(spawnX, spawnY);
            path.Add(GetWorldPosition(currentPos.x, currentPos.y));

            // 시작 방향 결정 (가장 거리가 짧은 유효 타일 방향)
            Vector2Int currentDir = Vector2Int.zero;
            int minDist = int.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                int nx = currentPos.x + dx[i];
                int ny = currentPos.y + dy[i];
                if (IsValidPathTile(nx, ny, gridW, gridH))
                {
                    if (distances[nx, ny] < minDist)
                    {
                        minDist = distances[nx, ny];
                        currentDir = new Vector2Int(dx[i], dy[i]);
                    }
                }
            }

            if (currentDir == Vector2Int.zero) return path; // 길 없음

            int safeguard = 0;
            while (currentPos.x != baseX || currentPos.y != baseY)
            {
                safeguard++;
                if (safeguard > gridW * gridH * 2) break; // 무한 루프 방지

                Vector2Int forward = currentPos + currentDir;
                bool canGoForward = IsValidPathTile(forward.x, forward.y, gridW, gridH);

                if (canGoForward)
                {
                    // 직진
                    currentPos = forward;
                    path.Add(GetWorldPosition(currentPos.x, currentPos.y));
                }
                else
                {
                    // 막힘: 90도 회전
                    Vector2Int left = new Vector2Int(-currentDir.y, currentDir.x);
                    Vector2Int right = new Vector2Int(currentDir.y, -currentDir.x);

                    Vector2Int posLeft = currentPos + left;
                    Vector2Int posRight = currentPos + right;

                    bool canGoLeft = IsValidPathTile(posLeft.x, posLeft.y, gridW, gridH);
                    bool canGoRight = IsValidPathTile(posRight.x, posRight.y, gridW, gridH);

                    if (canGoLeft && canGoRight)
                    {
                        // 양쪽 다 길이 있는 T자형 막다른 길이면 기지에 가까운 쪽 선택
                        int distLeft = distances[posLeft.x, posLeft.y];
                        int distRight = distances[posRight.x, posRight.y];
                        
                        if (distLeft <= distRight) currentDir = left;
                        else currentDir = right;
                    }
                    else if (canGoLeft) currentDir = left;
                    else if (canGoRight) currentDir = right;
                    else break; // 막다른 길
                }
            }

            return path;
        }

        private bool IsValidPathTile(int x, int y, int gridW, int gridH)
        {
            if (x < 0 || x >= gridW || y < 0 || y >= gridH) return false;
            TileType type = currentMapData.GetTileAt(x, y);
            return type == TileType.Path || type == TileType.Base;
        }

        private Color GetColorForTile(TileType type)
        {
            switch (type)
            {
                case TileType.Path: return new Color(0.6f, 0.4f, 0.2f);
                case TileType.Buildable: return Color.green;
                case TileType.NonBuildable: return Color.gray;
                case TileType.Spawn: return new Color(0.5f, 0f, 0.5f);
                case TileType.Base: return Color.blue;
                default: return Color.white;
            }
        }
    }
}
