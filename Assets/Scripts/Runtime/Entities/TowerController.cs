using UnityEngine;
using TDF.Core.Data;
using TDF.Runtime.Managers;

namespace TDF.Runtime.Entities
{
    public class TowerController : MonoBehaviour
    {
        private TowerData data;
        private int currentTierIndex = 0;
        
        private float attackTimer = 0f;
        private MonsterController currentTarget;

        // 성능 최적화: 매 프레임 OverlapCircleAll을 피하기 위해 NonAlloc 사용
        private Collider2D[] results = new Collider2D[20];
        private float searchTimer = 0f;
        private const float SEARCH_INTERVAL = 0.2f;

        private Transform cachedTransform;

        public void Initialize(TowerData towerData)
        {
            data = towerData;
            currentTierIndex = 0;
            cachedTransform = transform;
            
            if (data.assets != null && data.assets.idleSprite != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = data.assets.idleSprite;
            }
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (data == null || data.upgradeTiers.Count == 0) return;

            var currentTier = data.upgradeTiers[currentTierIndex];

            // 1. 타겟 탐색 (최적화: 0.2초마다 갱신 또는 타겟이 죽었을 때만)
            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f || currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                FindTarget(currentTier.range);
                searchTimer = SEARCH_INTERVAL;
            }

            // 2. 공격 쿨타임 계산 및 발사
            if (currentTarget != null)
            {
                // 타겟이 사거리를 벗어났는지 확인
                if (Vector2.SqrMagnitude(cachedTransform.position - currentTarget.transform.position) > currentTier.range * currentTier.range)
                {
                    currentTarget = null;
                    return;
                }

                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    FireProjectile();
                    // attackSpeed가 1초당 공격 횟수라면 쿨타임은 1 / attackSpeed
                    attackTimer = 1f / Mathf.Max(0.1f, currentTier.attackSpeed);
                }
            }
        }

        private void FindTarget(float range)
        {
            // Physics2D.OverlapCircleNonAlloc를 사용하여 가비지 컬렉션(GC) 방지
            int count = Physics2D.OverlapCircleNonAlloc(cachedTransform.position, range, results);
            
            float closestDistance = float.MaxValue;
            MonsterController closestMonster = null;

            for (int i = 0; i < count; i++)
            {
                MonsterController monster = results[i].GetComponent<MonsterController>();
                if (monster != null && monster.gameObject.activeInHierarchy)
                {
                    float dist = Vector2.SqrMagnitude(cachedTransform.position - monster.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestMonster = monster;
                    }
                }
            }

            currentTarget = closestMonster;
        }

        private void FireProjectile()
        {
            if (data.assets == null || data.assets.projectilePrefab == null || currentTarget == null) return;

            GameObject projObj = ObjectPoolManager.Instance.SpawnFromPool(
                data.assets.projectilePrefab,
                cachedTransform.position,
                Quaternion.identity
            );

            if (projObj != null)
            {
                var projectile = projObj.GetComponent<ProjectileController>();
                if (projectile != null)
                {
                    var tier = data.upgradeTiers[currentTierIndex];
                    projectile.Initialize(currentTarget, tier.damage, data.attackAttribute, data.assets.hitEffectPrefab);
                }
            }
        }

        public bool UpgradeTower()
        {
            if (currentTierIndex >= data.upgradeTiers.Count - 1) return false; // 최대 레벨

            int nextCost = data.upgradeTiers[currentTierIndex + 1].buildOrUpgradeCost;
            if (GameManager.Instance.UseGold(nextCost))
            {
                currentTierIndex++;
                // TODO: 외형 변경 (업그레이드된 스프라이트 등)
                return true;
            }
            return false;
        }
    }
}
