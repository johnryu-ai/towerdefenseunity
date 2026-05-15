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

    [System.Serializable]
    public class WaveRound
    {
        public int roundNumber;
        public int clearReward;
        public float nextRoundDelay = 5f;
        public List<WaveSpawnInfo> spawnSequence = new List<WaveSpawnInfo>();
    }

    [CreateAssetMenu(fileName = "NewWaveData", menuName = "TDF/Data/WaveData")]
    public class WaveData : ScriptableObject
    {
        public MapData linkedMapData;
        
        [Header("Waves / Rounds")]
        public List<WaveRound> rounds = new List<WaveRound>();
    }
}
