using UnityEngine;
using System.Collections.Generic;

namespace TDF.Core.Data
{
    public enum TileType
    {
        Path = 0,       // Brown
        Buildable = 1,  // Green
        NonBuildable = 2, // Gray
        Spawn = 3,      // Purple
        Base = 4        // Blue
    }

    [System.Serializable]
    public class MapConfig
    {
        public int initialGold = 100;
        public int initialLives = 20;
        public List<TowerData> availableTowers = new List<TowerData>();
        public int maxUpgradeLevel = 3;
    }

    [System.Serializable]
    public class SpawnPointData
    {
        public int spawnIndex;
        public Vector2Int coordinate;
        public List<Vector2> pathWaypoints = new List<Vector2>();
    }

    [CreateAssetMenu(fileName = "NewMapData", menuName = "TDF/Data/MapData")]
    public class MapData : ScriptableObject
    {
        [Header("Grid Size")]
        public int gridWidth = 12;
        public int gridHeight = 8;

        [Header("Grid System")]
        [SerializeField] private TileType[] gridLayout = new TileType[12 * 8];

        [Header("Spawn Points & Paths")]
        public List<SpawnPointData> spawnPoints = new List<SpawnPointData>();

        [Header("Config")]
        public MapConfig config;

        [Header("Visuals")]
        public Sprite backgroundSprite;
        public Sprite tileSprite;

        public TileType[] GridLayout => gridLayout;

        public TileType GetTileAt(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
                return TileType.NonBuildable;
            return gridLayout[y * gridWidth + x];
        }

        public void SetTileAt(int x, int y, TileType type)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
            gridLayout[y * gridWidth + x] = type;
        }

        public void ResizeGrid(int newWidth, int newHeight)
        {
            if (newWidth == gridWidth && newHeight == gridHeight) return;

            TileType[] newLayout = new TileType[newWidth * newHeight];
            
            // 기존 데이터 복사 (복사 가능한 영역까지만)
            int minWidth = Mathf.Min(gridWidth, newWidth);
            int minHeight = Mathf.Min(gridHeight, newHeight);

            for (int y = 0; y < minHeight; y++)
            {
                for (int x = 0; x < minWidth; x++)
                {
                    newLayout[y * newWidth + x] = gridLayout[y * gridWidth + x];
                }
            }

            gridLayout = newLayout;
            gridWidth = newWidth;
            gridHeight = newHeight;
        }
    }
}
