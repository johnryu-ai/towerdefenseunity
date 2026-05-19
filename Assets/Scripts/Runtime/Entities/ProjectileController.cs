using UnityEngine;
using TDF.Core.Data;
using TDF.Runtime.Managers;

namespace TDF.Runtime.Entities
{
    public class ProjectileController : MonoBehaviour
    {
        public float speed = 10f;
        
        private AttackType attackType;
        private float explosionRange;
        private Vector3 startPos;
        private Vector3 moveDirection;
        private System.Collections.Generic.List<MonsterController> piercedTargets = new System.Collections.Generic.List<MonsterController>();
        private bool isExploding = false;
        private float targetScale = 1f;

        private MonsterController target;
        private ObstacleController obstacleTarget;
        private float damage;
        private AttackAttribute attribute;
        private GameObject hitEffectPrefab;
        private TargetType targetType;

        private Transform cachedTransform;
        private AnimationClip currentPlayingClip;
        private float animTime = 0f;

        private void Awake()
        {
            cachedTransform = transform;
        }

        public void Initialize(AttackType type, TargetType tType, float range, Vector3 towerGroundPos, MonsterController targetMonster, float dmg, AttackAttribute attr, GameObject hitEffect, Sprite projSprite = null, AnimationClip projAnim = null, float projScale = 1f, float projSpeed = 10f)
        {
            attackType = type;
            targetType = tType;
            explosionRange = range;
            startPos = towerGroundPos;

            target = targetMonster;
            damage = dmg;
            attribute = attr;
            hitEffectPrefab = hitEffect;
            targetScale = projScale;
            speed = projSpeed;
            
            currentPlayingClip = projAnim;
            animTime = 0f;
            isExploding = false;
            piercedTargets.Clear();

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.material = new Material(Shader.Find("Sprites/Default"));
                sr.color = Color.white;
                sr.sortingOrder = 30000;

                if (projSprite != null) sr.sprite = projSprite;
                else if (projAnim != null) projAnim.SampleAnimation(gameObject, 0f);
            }

            if (attackType == AttackType.LinePiercing && target != null)
            {
                Vector3 targetGroundPos = target.transform.position;
                if (target.GetFlyType() == MonsterFlyType.Air) targetGroundPos -= Vector3.up;
                moveDirection = (targetGroundPos - transform.position).normalized;
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else if (attackType == AttackType.AreaSelf)
            {
                isExploding = true;
                ExplodeArea(startPos);
            }
            else if (target != null)
            {
                Vector3 dir = (target.transform.position - transform.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void Update()
        {
            if (currentPlayingClip != null)
            {
                animTime += Time.deltaTime;
                if (currentPlayingClip.isLooping) animTime %= currentPlayingClip.length;
                else animTime = Mathf.Clamp(animTime, 0f, currentPlayingClip.length);
                currentPlayingClip.SampleAnimation(gameObject, animTime);
            }

            if (isExploding)
            {
                targetScale += (explosionRange * 2f - targetScale) * Time.deltaTime * 15f;
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a -= Time.deltaTime * 3f;
                    sr.color = c;
                    if (c.a <= 0f) ReturnToPool();
                }
                return;
            }

            if (attackType == AttackType.LinePiercing)
            {
                cachedTransform.position += moveDirection * speed * Time.deltaTime;
                for (int i = 0; i < MonsterController.ActiveMonsters.Count; i++)
                {
                    var monster = MonsterController.ActiveMonsters[i];
                    if (monster != null && monster.gameObject.activeInHierarchy && !piercedTargets.Contains(monster))
                    {
                        if (!IsValidTargetType(monster.GetFlyType())) continue;

                        if (Vector2.SqrMagnitude(cachedTransform.position - monster.transform.position) < 0.25f)
                        {
                            piercedTargets.Add(monster);
                            ApplyDamageAndEffect(monster);
                        }
                    }
                }
                if (Vector2.Distance(startPos, cachedTransform.position) > explosionRange) ReturnToPool();
                return;
            }

            if ((target == null || !target.gameObject.activeInHierarchy) && 
                (obstacleTarget == null || !obstacleTarget.gameObject.activeInHierarchy))
            {
                if (attackType == AttackType.AreaProjectile)
                {
                    isExploding = true;
                    ExplodeArea(cachedTransform.position);
                }
                else if (attackType == AttackType.Multi)
                {
                    ExplodeAreaAt(cachedTransform.position, 0.7f);
                    ReturnToPool();
                }
                else ReturnToPool();
                return;
            }

            Vector3 targetWorldPos = target != null ? target.transform.position : obstacleTarget.transform.position;
            Vector3 dir = (targetWorldPos - cachedTransform.position).normalized;
            cachedTransform.position += dir * speed * Time.deltaTime;
            float currentAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            cachedTransform.rotation = Quaternion.Euler(0, 0, currentAngle);

            if (Vector2.SqrMagnitude(cachedTransform.position - targetWorldPos) < 0.1f)
            {
                if (attackType == AttackType.AreaProjectile)
                {
                    isExploding = true;
                    Vector3 explodeCenter = targetWorldPos;
                    if (target != null && target.GetFlyType() == MonsterFlyType.Air) explodeCenter -= Vector3.up;
                    ExplodeArea(explodeCenter);
                }
                else if (attackType == AttackType.Multi)
                {
                    Vector3 impactCenter = targetWorldPos;
                    if (target != null && target.GetFlyType() == MonsterFlyType.Air) impactCenter -= Vector3.up;
                    ExplodeAreaAt(impactCenter, 0.7f);
                    ReturnToPool();
                }
                else
                {
                    if (target != null) ApplyDamageAndEffect(target);
                    else if (obstacleTarget != null) obstacleTarget.TakeDamage(damage);
                    ReturnToPool();
                }
            }
        }

        public void SetObstacleTarget(ObstacleController obs)
        {
            obstacleTarget = obs;
        }

        private void LateUpdate()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) { transform.localScale = Vector3.one * targetScale; return; }
            if (sr.sprite != null)
            {
                float maxBound = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                if (maxBound > 0.001f) transform.localScale = Vector3.one * (targetScale / maxBound);
                else transform.localScale = Vector3.one * targetScale;
            }
            else transform.localScale = Vector3.one * targetScale;
        }

        private void ExplodeArea(Vector3 center)
        {
            ExplodeAreaAt(center, explosionRange);
        }

        private void ExplodeAreaAt(Vector3 center, float range)
        {
            float sqrRange = range * range;
            for (int i = MonsterController.ActiveMonsters.Count - 1; i >= 0; i--)
            {
                var monster = MonsterController.ActiveMonsters[i];
                if (monster != null && monster.gameObject.activeInHierarchy)
                {
                    if (!IsValidTargetType(monster.GetFlyType())) continue;
                    Vector3 mGround = monster.transform.position;
                    if (monster.GetFlyType() == MonsterFlyType.Air) mGround -= Vector3.up;
                    if (Vector2.SqrMagnitude(center - mGround) <= sqrRange) ApplyDamageAndEffect(monster);
                }
            }

            // 장애물 범위 타격 추가
            foreach (var obs in ObstacleController.ActiveObstacles)
            {
                if (obs != null && obs.gameObject.activeInHierarchy)
                {
                    if (Vector2.SqrMagnitude(center - obs.transform.position) <= sqrRange)
                    {
                        obs.TakeDamage(damage);
                    }
                }
            }
        }

        private void ApplyDamageAndEffect(MonsterController targetMonster)
        {
            targetMonster.TakeDamage(damage);
            
            // 면역 상태 확인: 면역이 아닐 때만 효과(Slow, Burn, Stun) 적용
            if (!targetMonster.IsImmuneTo(attribute))
            {
                var status = targetMonster.GetComponent<StatusEffectManager>();
                if (status != null)
                {
                    var settings = AttackAttributeSettings.Instance;
                    switch (attribute)
                    {
                        case AttackAttribute.Cold:
                            status.ApplySlow(settings.iceDuration, settings.iceSlowMultiplier);
                            break;
                        case AttackAttribute.Fire:
                            status.ApplyBurn(settings.fireDuration, damage * settings.fireBurnDamageRatio);
                            break;
                        case AttackAttribute.Electric:
                            status.ApplyStun(settings.lightningStunDuration);
                            break;
                    }
                }
            }
            if (hitEffectPrefab != null) ObjectPoolManager.Instance.SpawnFromPool(hitEffectPrefab, targetMonster.transform.position, Quaternion.identity);
        }

        private bool IsValidTargetType(MonsterFlyType flyType)
        {
            if (targetType == TargetType.Both || targetType == TargetType.Special) return true;
            if (targetType == TargetType.Ground && flyType == MonsterFlyType.Ground) return true;
            if (targetType == TargetType.Flying && flyType == MonsterFlyType.Air) return true;
            return false;
        }

        private void ReturnToPool()
        {
            gameObject.SetActive(false);
        }
    }
}
