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
