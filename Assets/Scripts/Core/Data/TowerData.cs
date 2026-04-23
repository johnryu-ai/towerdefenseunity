using UnityEngine;
using System.Collections.Generic;

namespace TDF.Core.Data
{
    public enum AttackAttribute { Normal, Fire, Cold, Electric, Buff, Resource }
    public enum AttackType { Single, LinePiercing, AreaSelf, AreaProjectile }
    public enum TargetType { Ground, Flying, Both, Special }

    [System.Serializable]
    public class TowerUpgradeTier
    {
        public int tierLevel; // 0 ~ 3
        public int buildOrUpgradeCost;
        public float range;
        public float damage;
        public float attackSpeed; // 1초당 공격 횟수 등
        public int sellPrice;
        public float manaCost;
    }

    [System.Serializable]
    public class TowerAssets
    {
        public Sprite idleSprite;
        public Sprite attackSprite;
        public AnimationClip idleAnim;
        public AnimationClip attackAnim;
        public GameObject projectilePrefab;
        public GameObject hitEffectPrefab;
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
