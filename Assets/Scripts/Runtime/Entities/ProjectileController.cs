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
        private float maxVisualScale;

        private MonsterController target;
        private float damage;
        private AttackAttribute attribute;
        private GameObject hitEffectPrefab;
        private float targetScale = 1f;
        private float originalTargetScale = 1f;

        private Transform cachedTransform;
        
        private Animator animator;
        private float animTime = 0f;
        private AnimationClip currentPlayingClip;

        private void Awake()
        {
            cachedTransform = transform;
            animator = GetComponent<Animator>();
        }

        private TargetType targetType;

        public void Initialize(AttackType type, TargetType tType, float range, Vector3 towerGroundPos, MonsterController targetMonster, float dmg, AttackAttribute attr, GameObject hitEffect, Sprite projSprite = null, AnimationClip projAnim = null, float projScale = 1f)
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
            originalTargetScale = projScale;
            
            currentPlayingClip = projAnim;
            animTime = 0f;
            isExploding = false;
            piercedTargets.Clear();

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.material = new Material(Shader.Find("Sprites/Default"));
                sr.color = Color.white; // 오브젝트 풀에서 꺼냈을 때 투명도 복구
                sr.sortingOrder = 30000;

                if (projSprite != null) {
                    sr.sprite = projSprite;
                } else if (projAnim != null) {
                    projAnim.SampleAnimation(gameObject, 0f); // 첫 프레임 로드
                }
            }

            if (projAnim != null)
            {
                if (animator == null) animator = gameObject.AddComponent<Animator>();
            }

            // 타입별 초기화 로직
            if (attackType == AttackType.LinePiercing)
            {
                if (target != null)
                {
                    // 발사 순간의 타겟 '바닥' 방향으로 직선 발사
                    Vector3 targetGroundPos = target.transform.position;
                    if (target.GetFlyType() == MonsterFlyType.Air) targetGroundPos -= Vector3.up;

                    moveDirection = (targetGroundPos - transform.position).normalized;
                    float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }
            else if (attackType == AttackType.AreaSelf)
            {
                // 타워 위치에서 즉시 폭발
                isExploding = true;
                ExplodeArea(startPos);
            }
            else
            {
                // Single, AreaProjectile, Multi: 타겟을 향해 유도
                if (target != null)
                {
                    Vector3 dir = (target.transform.position - transform.position).normalized;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }
        }

        private void Update()
        {
            // 애니메이션 재생
            if (currentPlayingClip != null)
            {
                animTime += Time.deltaTime;
                if (currentPlayingClip.isLooping) animTime %= currentPlayingClip.length;
                else animTime = Mathf.Clamp(animTime, 0f, currentPlayingClip.length);
                currentPlayingClip.SampleAnimation(gameObject, animTime);
            }

            if (isExploding)
            {
                // 폭발 이펙트 연출: 폭발 반경의 2배(지름)만큼 커짐
                targetScale += (explosionRange * 2f - targetScale) * Time.deltaTime * 15f;
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a -= Time.deltaTime * 3f; // 투명해지면서 사라짐
                    sr.color = c;
                    if (c.a <= 0f)
                    {
                        ReturnToPool();
                    }
                }
                return;
            }

            if (attackType == AttackType.LinePiercing)
            {
                cachedTransform.position += moveDirection * speed * Time.deltaTime;

                // 관통 충돌 체크
                for (int i = 0; i < MonsterController.ActiveMonsters.Count; i++)
                {
                    var monster = MonsterController.ActiveMonsters[i];
                    if (monster != null && monster.gameObject.activeInHierarchy && !piercedTargets.Contains(monster))
                    {
                        Vector3 monsterPos = monster.transform.position;
                        if (Vector2.SqrMagnitude(cachedTransform.position - monsterPos) < 0.25f) // 충돌 반경 0.5
                        {
                            piercedTargets.Add(monster);
                            ApplyDamageAndEffect(monster);
                        }
                    }
                }

                // 사거리를 벗어나면 파괴
                if (Vector2.Distance(startPos, cachedTransform.position) > explosionRange)
                {
                    ReturnToPool();
                }
                return;
            }

            // 타겟이 이미 죽었거나 사라졌을 때의 처리
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                if (attackType == AttackType.AreaProjectile)
                {
                    // 공중에서 타겟이 죽었다면 그 자리에서 바로 폭발
                    isExploding = true;
                    ExplodeArea(cachedTransform.position);
                }
                else if (attackType == AttackType.Multi)
                {
                    // Multi 타입 역시 죽은 자리에서 겹친 적들 타격
                    float multiRangeSqr = 0.5f * 0.5f;
                    Vector3 impactCenter = cachedTransform.position;
                    for (int i = MonsterController.ActiveMonsters.Count - 1; i >= 0; i--)
                    {
                        var monster = MonsterController.ActiveMonsters[i];
                        if (monster != null && monster.gameObject.activeInHierarchy)
                        {
                            Vector3 mGround = monster.transform.position;
                            if (monster.GetFlyType() == MonsterFlyType.Air) mGround -= Vector3.up;

                            if (IsValidTargetType(monster.GetFlyType()))
                            {
                                if (Vector2.SqrMagnitude(impactCenter - mGround) <= multiRangeSqr)
                                {
                                    ApplyDamageAndEffect(monster);
                                }
                            }
                        }
                    }
                    ReturnToPool();
                }
                else
                {
                    ReturnToPool();
                }
                return;
            }

            // 타겟을 향해 이동
            Vector3 dir = (target.transform.position - cachedTransform.position).normalized;
            cachedTransform.position += dir * speed * Time.deltaTime;
            float currentAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            cachedTransform.rotation = Quaternion.Euler(0, 0, currentAngle);

            // 타겟 명중 체크
            if (Vector2.SqrMagnitude(cachedTransform.position - target.transform.position) < 0.1f)
            {
                if (attackType == AttackType.AreaProjectile)
                {
                    isExploding = true;
                    // 폭발 기준점은 타겟의 바닥 위치로 함
                    Vector3 explodeCenter = target.transform.position;
                    if (target.GetFlyType() == MonsterFlyType.Air) explodeCenter -= Vector3.up;

                    ExplodeArea(explodeCenter);
                }
                else if (attackType == AttackType.Multi)
                {
                    // Multi: 타겟 위치 반경 0.5f(조금이라도 겹친) 내의 모든 적 타격
                    Vector3 impactCenter = target.transform.position;
                    if (target.GetFlyType() == MonsterFlyType.Air) impactCenter -= Vector3.up;
                    
                    float multiRangeSqr = 0.5f * 0.5f;
                    // 리스트 원소가 제거될 수 있으므로 역순 순회
                    for (int i = MonsterController.ActiveMonsters.Count - 1; i >= 0; i--)
                    {
                        var monster = MonsterController.ActiveMonsters[i];
                        if (monster != null && monster.gameObject.activeInHierarchy)
                        {
                            Vector3 mGround = monster.transform.position;
                            if (monster.GetFlyType() == MonsterFlyType.Air) mGround -= Vector3.up;

                            if (IsValidTargetType(monster.GetFlyType()))
                            {
                                if (Vector2.SqrMagnitude(impactCenter - mGround) <= multiRangeSqr)
                                {
                                    ApplyDamageAndEffect(monster);
                                }
                            }
                        }
                    }
                    ReturnToPool();
                }
                else // Single
                {
                    ApplyDamageAndEffect(target);
                    ReturnToPool();
                }
            }
        }

        private void LateUpdate()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float maxBound = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                if (maxBound > 0.001f)
                {
                    float currentScale = targetScale / maxBound;
                    transform.localScale = Vector3.one * currentScale;
                }
            }
        }

        private void ExplodeArea(Vector3 center)
        {
            float sqrRange = explosionRange * explosionRange;
            // 리스트 원소가 제거되더라도 스킵되지 않도록 역순 순회
            for (int i = MonsterController.ActiveMonsters.Count - 1; i >= 0; i--)
            {
                var monster = MonsterController.ActiveMonsters[i];
                if (monster != null && monster.gameObject.activeInHierarchy)
                {
                    // 몬스터 바닥 좌표 기준 검사
                    Vector3 monsterGroundPos = monster.transform.position;
                    if (monster.GetFlyType() == MonsterFlyType.Air) monsterGroundPos -= Vector3.up;

                    if (IsValidTargetType(monster.GetFlyType()))
                    {
                        if (Vector2.SqrMagnitude(center - monsterGroundPos) <= sqrRange)
                        {
                            ApplyDamageAndEffect(monster);
                        }
                    }
                }
            }
        }

        private void ApplyDamageAndEffect(MonsterController targetMonster)
        {
            targetMonster.TakeDamage(damage);
            
            var status = targetMonster.GetComponent<StatusEffectManager>();
            if (status != null)
            {
                switch (attribute)
                {
                    case AttackAttribute.Cold:
                        status.ApplySlow(2f, 0.4f);
                        break;
                    case AttackAttribute.Fire:
                        status.ApplyBurn(3f, damage * 0.2f);
                        break;
                    case AttackAttribute.Electric:
                        status.ApplyStun(0.5f);
                        break;
                }
            }

            if (hitEffectPrefab != null)
            {
                ObjectPoolManager.Instance.SpawnFromPool(hitEffectPrefab, targetMonster.transform.position, Quaternion.identity);
            }
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
