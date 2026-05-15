using UnityEngine;

namespace TDF.Core.Data
{
    [CreateAssetMenu(fileName = "NewObstacleData", menuName = "TDF/Data/ObstacleData")]
    public class ObstacleData : ScriptableObject
    {
        public string obstacleId;
        public string obstacleName;
        public float health = 50f;
        public int rewardGold = 20;
        
        public Sprite sprite;
        public GameObject prefab;
        public float visualScale = 1.0f;
    }
}
