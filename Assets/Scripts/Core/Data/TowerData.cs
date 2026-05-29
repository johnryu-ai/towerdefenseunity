using UnityEngine;
using System.Collections.Generic;

namespace TDF.Core.Data
{
    public enum AttackAttribute { Normal, Fire, Cold, Electric, Buff, Resource }
    public enum AttackType { Single, LinePiercing, AreaSelf, AreaProjectile, ContinuousHoming, Multi }
    public enum TargetType { Ground, Flying, Both, Special }

    [System.Serializable]
    public class TowerUpgradeTier
    {
        public int tierLevel; // 0 ~ 3
        public int buildOrUpgradeCost;
        public float range;
        public float damage;
        public float attackSpeed; // 1초당 공격 횟수 등
        public float projectileSpeed = 10f; // 발사체 이동 속도
        public int sellPrice;
        public float manaCost;

        [Header("Visuals")]
        public Sprite projectileSprite; // 업그레이드 상태별 발사체 이미지
        public AnimationClip projectileAnim; // 발사체 애니메이션 (단일 파일)
    }

    [System.Serializable]
    public class DirectionalSprites
    {
        [Tooltip("키패드 8")] public AnimationClip up;
        [Tooltip("키패드 2")] public AnimationClip down;
        [Tooltip("키패드 4")] public AnimationClip left;
        [Tooltip("키패드 6")] public AnimationClip right;
        [Tooltip("키패드 7")] public AnimationClip upLeft;
        [Tooltip("키패드 9")] public AnimationClip upRight;
        [Tooltip("키패드 1")] public AnimationClip downLeft;
        [Tooltip("키패드 3")] public AnimationClip downRight;
    }

    [System.Serializable]
    public class TowerAssets
    {
        public Sprite idleSprite;
        public Sprite attackSprite;
        public DirectionalSprites attackSprites8Dir; // 8방향 공격 모션용
        public AnimationClip idleAnim;
        public AnimationClip attackAnim;
        public RuntimeAnimatorController animatorController; // 애니메이션 컨트롤러 추가
        public GameObject prefab;
        public GameObject projectilePrefab; // 기본 발사체 프리팹 (스프라이트는 tier에서 오버라이드)
        public GameObject hitEffectPrefab;
        
        [Header("Scale")]
        public float visualScale = 1.0f; // 타일 기준 크기 조절 (1.0 = 타일 크기)
        public float projectileScale = 0.5f; // 발사체 크기 조절 (0.5 = 반 타일 크기)
    }

    [CreateAssetMenu(fileName = "NewTowerData", menuName = "TDF/Data/TowerData")]
    public class TowerData : ScriptableObject
    {
        [Header("Basic Info")]
        public string towerId;
        public string towerName;

        [Header("Properties")]
        public AttackAttribute attackAttribute = AttackAttribute.Normal;
        public AttackType attackType = AttackType.Single;
        public TargetType targetType = TargetType.Both;

        [Header("Upgrade Tiers (0~3)")]
        public List<TowerUpgradeTier> upgradeTiers = new List<TowerUpgradeTier>();

        [Header("Assets")]
        public TowerAssets assets;
    }
}
