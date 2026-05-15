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
        public GameObject tilePrefab; 
        public Transform tileContainer; 

        private MapData currentMapData;
        private GameObject[,] spawnedTiles;

        public const float TILE_SIZE = 1f; 
        public float offsetX { get; private set; }
        public float offsetY { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            if (Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            }
        }

        public void GenerateMap(MapData mapData)
        {
            if (mapData == null) return;
            currentMapData = mapData;

            if (tilePrefab == null)
            {
                tilePrefab = new GameObject("DefaultTilePrefab");
                var sr = tilePrefab.AddComponent<SpriteRenderer>();
                Texture2D tex = new Texture2D(100, 100);
                Color[] colors = new Color[100 * 100];
                for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
                tex.SetPixels(colors);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f), 100f);
                tilePrefab.SetActive(false);
            }

            spawnedTiles = new GameObject[mapData.gridWidth, mapData.gridHeight];
            float mapWorldWidth = mapData.gridWidth * TILE_SIZE;
            float mapWorldHeight = mapData.gridHeight * TILE_SIZE;
            if (Camera.main != null)
            {
                Camera.main.orthographicSize = mapWorldHeight / 2f;
                // 카메라를 맵 중앙으로 이동 (0,0이 최좌측 하단이므로 중앙은 너비/높이의 절반)
                Camera.main.transform.position = new Vector3(mapWorldWidth / 2f - (TILE_SIZE / 2f), mapWorldHeight / 2f - (TILE_SIZE / 2f), Camera.main.transform.position.z);
            }

            if (tileContainer == null)
            {
                tileContainer = new GameObject("TileContainer").transform;
                tileContainer.SetParent(this.transform);
            }
            foreach (Transform child in tileContainer) Destroy(child.gameObject);

            // [수정] 0,0이 항상 화면 최하단 좌측 타일이 되도록 오프셋을 0으로 설정
            offsetX = 0f;
            offsetY = 0f;

            for (int y = 0; y < mapData.gridHeight; y++)
            {
                for (int x = 0; x < mapData.gridWidth; x++)
                {
                    Vector3 position = new Vector3(offsetX + (x * TILE_SIZE), offsetY + (y * TILE_SIZE), 0);
                    GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, tileContainer);
                    tileObj.SetActive(true);
                    tileObj.name = $"Tile_{x}_{y}";
                    TileType type = mapData.GetTileAt(x, y);
                    SpriteRenderer sr = tileObj.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = GetColorForTile(type);
                    spawnedTiles[x, y] = tileObj;
                }
            }

            SpawnObstacles(mapData);

            if (mapData.backgroundSprite != null)
            {
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(this.transform);
                // 맵의 (0,0)이 좌하단이므로, 백그라운드 이미지는 맵의 중앙(카메라 위치)으로 이동시켜야 함
                bgObj.transform.position = new Vector3(mapWorldWidth / 2f - (TILE_SIZE / 2f), mapWorldHeight / 2f - (TILE_SIZE / 2f), 0);
                
                SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
                bgSr.sprite = mapData.backgroundSprite;
                bgSr.sortingOrder = 5;
                if (Camera.main != null)
                {
                    float screenHeightUnits = Camera.main.orthographicSize * 2f;
                    float screenWidthUnits = screenHeightUnits * (1920f / 1080f);
                    bgObj.transform.localScale = new Vector3(screenWidthUnits / bgSr.sprite.bounds.size.x, screenHeightUnits / bgSr.sprite.bounds.size.y, 1f);
                }
            }
        }

        private void SpawnObstacles(MapData mapData)
        {
            foreach(var obs in Entities.ObstacleController.ActiveObstacles)
            {
                if (obs != null) Destroy(obs.gameObject);
            }
            Entities.ObstacleController.ActiveObstacles.Clear();

            foreach (var po in mapData.obstacles)
            {
                if (po.data == null) continue;
                Vector3 pos = GetWorldPosition(po.coordinate.x, po.coordinate.y);
                GameObject obsObj = new GameObject($"Obstacle_{po.data.obstacleName}");
                obsObj.transform.position = pos;
                var controller = obsObj.AddComponent<Entities.ObstacleController>();
                controller.Initialize(po.data, po.coordinate);
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

        public List<Vector2> GetPath(int spawnPointIndex = -1, Vector2Int? startCoord = null, Vector2Int? directionHint = null, bool isAir = false)
        {
            List<Vector2> path = new List<Vector2>();
            if (currentMapData == null) return path;

            int gridW = currentMapData.gridWidth;
            int gridH = currentMapData.gridHeight;

            int spawnX = -1, spawnY = -1;
            int baseX = -1, baseY = -1;

            if (startCoord.HasValue)
            {
                spawnX = startCoord.Value.x;
                spawnY = startCoord.Value.y;
            }
            else
            {
                if (spawnPointIndex < 0 || spawnPointIndex >= currentMapData.spawnPoints.Count)
                {
                    if (currentMapData.spawnPoints.Count > 0) spawnPointIndex = 0;
                }

                if (spawnPointIndex >= 0 && spawnPointIndex < currentMapData.spawnPoints.Count)
                {
                    var sp = currentMapData.spawnPoints[spawnPointIndex];
                    
                    // [수정] Map에 수동 경로(manual path waypoint)가 있다면 그것을 즉시 반환
                    if (sp.pathWaypoints != null && sp.pathWaypoints.Count > 0)
                    {
                        return new List<Vector2>(sp.pathWaypoints);
                    }

                    spawnX = sp.coordinate.x;
                    spawnY = sp.coordinate.y;
                }
            }

            for (int y = 0; y < gridH; y++)
            {
                for (int x = 0; x < gridW; x++)
                {
                    if (currentMapData.GetTileAt(x, y) == TileType.Base) { baseX = x; baseY = y; break; }
                }
                if (baseX != -1) break;
            }

            if (spawnX == -1 || baseX == -1) return path;

            int[,] distances = new int[gridW, gridH];
            for (int x = 0; x < gridW; x++) for (int y = 0; y < gridH; y++) distances[x, y] = int.MaxValue;

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
                        
                        // 공중 유닛은 타일 종류와 장애물을 무시하고 이동 가능
                        bool canPass = isAir || ((type == TileType.Path || type == TileType.Spawn || type == TileType.Base) && !HasObstacleAt(nx, ny));

                        if (canPass && distances[nx, ny] == int.MaxValue)
                        {
                            distances[nx, ny] = distances[curr.x, curr.y] + 1;
                            queue.Enqueue(new Vector2Int(nx, ny));
                        }
                    }
                }
            }

            Vector2Int currentPos = new Vector2Int(spawnX, spawnY);
            Vector3 worldSpawnPos = GetWorldPosition(currentPos.x, currentPos.y);
            if (worldSpawnPos == Vector3.zero) worldSpawnPos = new Vector3(offsetX + (currentPos.x * TILE_SIZE), offsetY + (currentPos.y * TILE_SIZE), 0);
            
            path.Add(worldSpawnPos);

            if (distances[spawnX, spawnY] == int.MaxValue)
            {
                Debug.LogWarning($"[MapController] No path found from ({spawnX}, {spawnY}) to base!");
                return path;
            }

            Vector2Int lastDir = directionHint ?? Vector2Int.zero;
            int safeguard = 0;
            
            // BFS distances are still needed to know IF a path exists and to prevent infinite loops/dead ends,
            // but the priority logic will now favor straight lines even if a turn has the same or slightly better distance.
            
            while (currentPos.x != baseX || currentPos.y != baseY)
            {
                safeguard++;
                if (safeguard > gridW * gridH) break;

                Vector2Int bestNext = currentPos;
                int currentDist = distances[currentPos.x, currentPos.y];

                // 1. [최우선] 직진 확인: 진행 방향에 길이 있고, 그 길을 통해 기지에 도달 가능하다면 무조건 선택
                if (lastDir != Vector2Int.zero)
                {
                    int nx = currentPos.x + lastDir.x;
                    int ny = currentPos.y + lastDir.y;
                    if (nx >= 0 && nx < gridW && ny >= 0 && ny < gridH)
                    {
                        // 해당 칸이 도달 가능한 칸(distances < max)이라면 직진
                        if (distances[nx, ny] != int.MaxValue)
                        {
                            bestNext = new Vector2Int(nx, ny);
                            currentPos = bestNext;
                            path.Add(GetWorldPosition(currentPos.x, currentPos.y));
                            continue; 
                        }
                    }
                }

                // 2. [차선] 90도 회전: 직진이 막힌 경우에만 기지와 가장 가까운 90도 방향 선택
                List<Vector2Int> turnDirs = new List<Vector2Int>();
                if (lastDir.x != 0) { turnDirs.Add(new Vector2Int(0, 1)); turnDirs.Add(new Vector2Int(0, -1)); }
                else if (lastDir.y != 0) { turnDirs.Add(new Vector2Int(1, 0)); turnDirs.Add(new Vector2Int(-1, 0)); }
                else turnDirs.AddRange(new Vector2Int[] { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1) });

                int minDist = int.MaxValue;
                bool foundTurn = false;
                foreach (var dir in turnDirs)
                {
                    int nx = currentPos.x + dir.x;
                    int ny = currentPos.y + dir.y;
                    if (nx >= 0 && nx < gridW && ny >= 0 && ny < gridH)
                    {
                        if (distances[nx, ny] < minDist)
                        {
                            minDist = distances[nx, ny];
                            bestNext = new Vector2Int(nx, ny);
                            lastDir = dir;
                            foundTurn = true;
                        }
                    }
                }

                if (!foundTurn) 
                {
                    // 3. [최후] 역주행: 모든 길이 막혔을 때만 고려
                    Vector2Int reverse = -lastDir;
                    int nx = currentPos.x + reverse.x;
                    int ny = currentPos.y + reverse.y;
                    if (nx >= 0 && nx < gridW && ny >= 0 && ny < gridH && distances[nx, ny] != int.MaxValue)
                    {
                        bestNext = new Vector2Int(nx, ny);
                        lastDir = reverse;
                    }
                    else break; 
                }

                currentPos = bestNext;
                path.Add(GetWorldPosition(currentPos.x, currentPos.y));
            }
            
            return path;
        }

        private bool HasObstacleAt(int x, int y)
        {
            foreach (var obs in Entities.ObstacleController.ActiveObstacles)
            {
                if (obs.GridPos.x == x && obs.GridPos.y == y) return true;
            }
            return false;
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
