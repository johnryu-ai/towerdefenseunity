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
            // GameManager에서 초기화가 끝난 후 맵 데이터 연동
            if (GameManager.Instance != null && GameManager.Instance.currentMapData != null)
            {
                GenerateMap(GameManager.Instance.currentMapData);
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

            int currX = -1, currY = -1;
            for (int y = 0; y < currentMapData.gridHeight; y++)
            {
                for (int x = 0; x < currentMapData.gridWidth; x++)
                {
                    if (currentMapData.GetTileAt(x, y) == TileType.Spawn)
                    {
                        currX = x; currY = y; break;
                    }
                }
                if (currX != -1) break;
            }

            if (currX == -1) return path;

            bool[,] visited = new bool[currentMapData.gridWidth, currentMapData.gridHeight];
            
            while (true)
            {
                path.Add(GetWorldPosition(currX, currY));
                visited[currX, currY] = true;

                if (currentMapData.GetTileAt(currX, currY) == TileType.Base) break;

                int nextX = -1, nextY = -1;
                int[] dx = { 1, -1, 0, 0 };
                int[] dy = { 0, 0, 1, -1 };
                
                for (int i = 0; i < 4; i++)
                {
                    int nx = currX + dx[i];
                    int ny = currY + dy[i];
                    if (nx >= 0 && nx < currentMapData.gridWidth && ny >= 0 && ny < currentMapData.gridHeight)
                    {
                        if (!visited[nx, ny])
                        {
                            TileType type = currentMapData.GetTileAt(nx, ny);
                            if (type == TileType.Path || type == TileType.Base)
                            {
                                nextX = nx; nextY = ny; break;
                            }
                        }
                    }
                }

                if (nextX == -1) break;
                currX = nextX;
                currY = nextY;
            }
            return path;
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
