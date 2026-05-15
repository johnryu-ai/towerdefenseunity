using UnityEngine;
using System.Collections.Generic;

namespace TDF.Core.Data
{
    public enum MonsterFlyType { Ground, Air }
    public enum MonsterMovementType { GroundPath, FlyingStraight, FlyingCurve }

    [System.Serializable]
    public class MonsterStats
    {
        public float moveSpeed = 1f;
        public float health = 100f;
        public int killReward = 10;
        public int baseDamage = 1; // 기지에 가하는 데미지
        public AttackAttribute immuneAttribute = AttackAttribute.Normal; // 면역 속성 (Normal = None)
    }

    [System.Serializable]
    public class MonsterAssets
    {
        public Sprite moveSprite;
        public Sprite hitSprite;
        public Sprite dieSprite;
        public AnimationClip moveAnim;
        public RuntimeAnimatorController animatorController; // 애니메이션 컨트롤러 추가
        public GameObject prefab;

        [Header("Scale")]
        public float visualScale = 1.0f; // 타일 기준 크기 조절 (1.0 = 타일 크기)
    }

    [System.Serializable]
    public class SplitLogic
    {
        public bool splitOnDeath = false;
        public MonsterData splitMonsterType;
        public int splitCount = 2;
    }

    [CreateAssetMenu(fileName = "NewMonsterData", menuName = "TDF/Data/MonsterData")]
    public class MonsterData : ScriptableObject
    {
        [Header("Basic")]
        public string monsterId;
        public string monsterName;

        [Header("Stats")]
        public MonsterStats stats;
        
        [Header("Movement")]
        public MonsterFlyType flyType = MonsterFlyType.Ground;
        public MonsterMovementType movementType = MonsterMovementType.GroundPath;

        [Header("Special")]
        public SplitLogic splitLogic;

        [Header("Assets")]
        public MonsterAssets assets;
    }
}
