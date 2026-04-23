using UnityEngine;
using System.Collections.Generic;

namespace TDF.Core.Data
{
    [System.Serializable]
    public class WaveSpawnInfo
    {
        public MonsterData monsterToSpawn;
        public int spawnPointIndex; // MapData의 spawnPoints 인덱스
        public int spawnCount;
        public float spawnInterval;
        public float startDelay;
        
        // 추가: 에디터에서 수동으로 입력할 수 있는 웨이포인트(이동 경로)
        public List<Vector2> customWaypoints = new List<Vector2>();
    }

    [CreateAssetMenu(fileName = "NewWaveData", menuName = "TDF/Data/WaveData")]
    public class WaveData : ScriptableObject
    {
        [Header("Round Info")]
        public int roundNumber;
        public MapData linkedMapData;
        public int clearReward;
        public float nextRoundDelay = 5f;

        [Header("Spawn Sequence")]
        public List<WaveSpawnInfo> spawnSequence = new List<WaveSpawnInfo>();
    }
}
