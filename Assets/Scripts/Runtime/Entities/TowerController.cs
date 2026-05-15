using UnityEngine;
using TDF.Core.Data;
using TDF.Runtime.Managers;
using System.Collections.Generic;

namespace TDF.Runtime.Entities
{
    public class TowerController : MonoBehaviour
    {
        private TowerData data;
        private int currentTierIndex = 0;
        
        private float attackTimer = 0f;
        private MonsterController currentTarget;
        private ObstacleController currentObstacleTarget;
        
        private MonsterController priorityTarget;
        private ObstacleController priorityObstacleTarget;

        private float searchTimer = 0f;
        private const float SEARCH_INTERVAL = 0.05f; // 더욱 빈번하게 검색하여 완벽한 타겟팅 보장

        private Transform cachedTransform;

        private LineRenderer currentRangeLine;
        private LineRenderer nextRangeLine;
        private SpriteRenderer currentRangeFill;
        private SpriteRenderer nextRangeFill;
        private LineRenderer laserBeam;
        
        private static Sprite cachedRangeFillSprite;
        
        private Animator animator;
        private float animTime = 0f;
        private AnimationClip currentPlayingClip;

        private void PlayClip(AnimationClip clip)
        {
            if (clip == null) return;
            if (currentPlayingClip != clip)
            {
                currentPlayingClip = clip;
                animTime = 0f;
            }
            animTime += Time.deltaTime;
            if (clip.isLooping) animTime %= clip.length;
            else animTime = Mathf.Clamp(animTime, 0f, clip.length);
            clip.SampleAnimation(gameObject, animTime);
        }

        private void Awake()
        {
            cachedTransform = transform;
            currentRangeLine = CreateRangeCircle("CurrentRange", Color.green);
            nextRangeLine = CreateRangeCircle("NextRange", Color.yellow);
            
            currentRangeFill = CreateRangeFill("CurrentRangeFill", new Color(0, 1, 0, 0.15f));
            nextRangeFill = CreateRangeFill("NextRangeFill", new Color(1, 1, 0, 0.1f));

            GameObject laserObj = new GameObject("LaserBeam");
            laserObj.transform.SetParent(transform);
            laserBeam = laserObj.AddComponent<LineRenderer>();
            laserBeam.useWorldSpace = true;
            laserBeam.startWidth = 0.1f;
            laserBeam.endWidth = 0.1f;
            laserBeam.material = new Material(Shader.Find("Sprites/Default"));
            laserBeam.startColor = Color.cyan;
            laserBeam.endColor = Color.blue;
            laserBeam.positionCount = 2;
            laserBeam.sortingOrder = 3000;
            laserBeam.gameObject.SetActive(false);
        }

        private SpriteRenderer CreateRangeFill(string name, Color color)
        {
            if (cachedRangeFillSprite == null)
            {
                Texture2D tex = new Texture2D(128, 128);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(64, 64));
                        if (dist <= 64f) tex.SetPixel(x, y, Color.white);
                        else tex.SetPixel(x, y, Color.clear);
                    }
                }
                tex.Apply();
                cachedRangeFillSprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            }

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = cachedRangeFillSprite;
            sr.color = color;
            sr.sortingOrder = 4900; // Outline보다는 아래
            obj.SetActive(false);
            return sr;
        }

        private LineRenderer CreateRangeCircle(string name, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            lr.useWorldSpace = true;
            lr.positionCount = 51;
            lr.sortingOrder = 5000;
            lr.gameObject.SetActive(false);
            return lr;
        }

        public TowerData GetData() => data;
        public int GetCurrentTierIndex() => currentTierIndex;

        public void Deselect()
        {
            currentRangeLine.gameObject.SetActive(false);
            nextRangeLine.gameObject.SetActive(false);
            currentRangeFill.gameObject.SetActive(false);
            nextRangeFill.gameObject.SetActive(false);
        }

        public void Select()
        {
            if (data != null && data.upgradeTiers != null)
            {
                float currentRange = data.upgradeTiers[currentTierIndex].range;
                UpdateCircle(currentRangeLine, currentRange);
                currentRangeLine.gameObject.SetActive(true);
                
                // 채우기 영역 활성화 및 스케일 조정 (Sprite 기본 크기 1x1 가정)
                currentRangeFill.transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f), transform.position.z);
                currentRangeFill.transform.localScale = Vector3.one * (currentRange * 2f);
                currentRangeFill.gameObject.SetActive(true);

                if (currentTierIndex < data.upgradeTiers.Count - 1)
                {
                    float nextRange = data.upgradeTiers[currentTierIndex + 1].range;
                    UpdateCircle(nextRangeLine, nextRange);
                    nextRangeLine.gameObject.SetActive(true);
                    
                    nextRangeFill.transform.position = currentRangeFill.transform.position;
                    nextRangeFill.transform.localScale = Vector3.one * (nextRange * 2f);
                    nextRangeFill.gameObject.SetActive(true);
                }
            }
        }

        private void UpdateCircle(LineRenderer lr, float radius)
        {
            float x, y;
            float angle = 0f;
            Vector3 center = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f), transform.position.z);
            for (int i = 0; i < 51; i++)
            {
                x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
                lr.SetPosition(i, new Vector3(center.x + x, center.y + y, 0));
                angle += (360f / 50f);
            }
        }

        public int GridX { get; private set; }
        public int GridY { get; private set; }

        public void Initialize(TowerData towerData, int gridX, int gridY)
        {
            data = towerData;
            currentTierIndex = 0;
            GridX = gridX;
            GridY = gridY;
            cachedTransform = transform;
            if (laserBeam != null) laserBeam.gameObject.SetActive(false);
            
            if (data.assets != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.material = new Material(Shader.Find("Sprites/Default"));
                    sr.color = Color.white;
                    if (data.assets.idleSprite != null) sr.sprite = data.assets.idleSprite;
                    else if (data.assets.idleAnim != null) data.assets.idleAnim.SampleAnimation(gameObject, 0f);
                    UpdateSortingOrder();
                }
                Vector3 centerPos = transform.position;
                transform.position = new Vector3(centerPos.x, centerPos.y - 0.5f + (data.assets.visualScale * 0.5f), centerPos.z);
                if (data.assets.animatorController != null)
                {
                    if (animator == null) animator = gameObject.AddComponent<Animator>();
                    animator.runtimeAnimatorController = data.assets.animatorController;
                }
            }
        }

        private void UpdateSortingOrder()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 100f) - Mathf.RoundToInt(transform.position.x * 10f);
                if (laserBeam != null)
                {
                    laserBeam.sortingOrder = sr.sortingOrder - 1;
                }
            }
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (data == null || data.upgradeTiers.Count == 0) return;

            var currentTier = data.upgradeTiers[currentTierIndex];

            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f || (currentTarget == null && currentObstacleTarget == null))
            {
                FindTarget(currentTier.range);
                searchTimer = SEARCH_INTERVAL;
            }

            if (currentTarget != null || currentObstacleTarget != null)
            {
                Vector3 targetPos = Vector3.zero;
                bool isTargetActive = false;

                if (currentTarget != null)
                {
                    isTargetActive = currentTarget.gameObject.activeInHierarchy;
                    if (isTargetActive)
                    {
                        targetPos = currentTarget.transform.position;
                        if (currentTarget.GetFlyType() == MonsterFlyType.Air) {
                            // 공중 유닛의 이미지를 타격하도록 그림자 위치(아래)로 보정하지 않음
                        }
                    }
                    else currentTarget = null;
                }
                else if (currentObstacleTarget != null)
                {
                    isTargetActive = currentObstacleTarget.gameObject.activeInHierarchy;
                    if (isTargetActive) targetPos = currentObstacleTarget.transform.position;
                    else currentObstacleTarget = null;
                }

                if (!isTargetActive)
                {
                    RevertToIdle();
                    if (laserBeam != null) laserBeam.gameObject.SetActive(false);
                    return;
                }

                Vector3 towerGroundPos = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f), transform.position.z);
                if (Vector2.SqrMagnitude(towerGroundPos - targetPos) > currentTier.range * currentTier.range)
                {
                    currentTarget = null;
                    currentObstacleTarget = null;
                    RevertToIdle();
                    if (laserBeam != null) laserBeam.gameObject.SetActive(false);
                    return;
                }

                UpdateAttackSprite(currentTarget != null ? currentTarget.transform.position : currentObstacleTarget.transform.position);

                if (data.attackType == AttackType.ContinuousHoming)
                {
                    if (laserBeam != null)
                    {
                        laserBeam.gameObject.SetActive(true);
                        // 발사체는 타워의 중심(transform.position)에서 발사되도록 설정
                        laserBeam.SetPosition(0, transform.position);
                        laserBeam.SetPosition(1, targetPos);
                    }

                    // 데미지는 매 프레임 연속적으로 적용 (초당 데미지 = 기본 데미지 * 공격 속도)
                    float dps = currentTier.damage * currentTier.attackSpeed;
                    float damageThisFrame = dps * Time.deltaTime;
                    
                    if (currentTarget != null) currentTarget.TakeDamage(damageThisFrame);
                    else if (currentObstacleTarget != null) currentObstacleTarget.TakeDamage(damageThisFrame);

                    // 이펙트 생성은 기존의 타격 주기를 유지
                    attackTimer -= Time.deltaTime;
                    if (attackTimer <= 0f)
                    {
                        if (data.assets.hitEffectPrefab != null)
                        {
                            ObjectPoolManager.Instance.SpawnFromPool(data.assets.hitEffectPrefab, targetPos, Quaternion.identity);
                        }
                        attackTimer = 1f / Mathf.Max(0.1f, currentTier.attackSpeed);
                    }
                }
                else
                {
                    if (laserBeam != null && laserBeam.gameObject.activeSelf) laserBeam.gameObject.SetActive(false);
                    attackTimer -= Time.deltaTime;
                    if (attackTimer <= 0f)
                    {
                        FireProjectile();
                        attackTimer = 1f / Mathf.Max(0.1f, currentTier.attackSpeed);
                    }
                }
            }
            else
            {
                if (laserBeam != null) laserBeam.gameObject.SetActive(false);
                RevertToIdle();
            }
        }

        private void LateUpdate()
        {
            if (data != null && data.assets != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    float maxBound = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                    if (maxBound > 0.001f)
                    {
                        float currentScale = data.assets.visualScale / maxBound;
                        transform.localScale = Vector3.one * currentScale;
                    }
                }
            }
        }

        private void RevertToIdle()
        {
            if (data.assets.animatorController != null)
            {
                if (animator != null) animator.SetBool("IsAttacking", false);
            }
            else if (data.assets.idleAnim != null)
            {
                PlayClip(data.assets.idleAnim);
            }
            else if (data.assets != null && data.assets.idleSprite != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = data.assets.idleSprite;
            }
        }

        private void UpdateAttackSprite(Vector2 targetPos)
        {
            if (data.assets == null) return;
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            Vector2 dir = (targetPos - (Vector2)cachedTransform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            if (data.assets.attackSprites8Dir != null)
            {
                int octant = Mathf.RoundToInt(angle / 45f) % 8;
                Sprite s = null;
                var dirs = data.assets.attackSprites8Dir;
                switch (octant)
                {
                    case 0: s = dirs.right; break;
                    case 1: s = dirs.upRight; break;
                    case 2: s = dirs.up; break;
                    case 3: s = dirs.upLeft; break;
                    case 4: s = dirs.left; break;
                    case 5: s = dirs.downLeft; break;
                    case 6: s = dirs.down; break;
                    case 7: s = dirs.downRight; break;
                }
                if (s != null && data.assets.animatorController == null && data.assets.attackAnim == null) sr.sprite = s;
            }

            if (data.assets.animatorController != null)
            {
                if (animator != null)
                {
                    animator.SetBool("IsAttacking", true);
                    animator.SetFloat("DirX", dir.x);
                    animator.SetFloat("DirY", dir.y);
                    animator.SetFloat("Angle", angle);
                }
            }
            else if (data.assets.attackAnim != null) PlayClip(data.assets.attackAnim);
        }

        public void SetPriorityTarget(MonsterController monster)
        {
            if (monster == null) return;
            // 타겟 타입 검사 (공중 공격 불가능한 타워가 공중 유닛을 찍는 경우 방지)
            if (!IsValidTargetType(monster.GetFlyType())) return;

            priorityTarget = monster;
            priorityObstacleTarget = null;
            
            // 즉시 타겟팅 갱신
            var tier = data.upgradeTiers[currentTierIndex];
            FindTarget(tier.range);
        }

        public void SetPriorityTarget(ObstacleController obstacle)
        {
            if (obstacle == null) return;
            priorityObstacleTarget = obstacle;
            priorityTarget = null;
            
            // 즉시 타겟팅 갱신
            var tier = data.upgradeTiers[currentTierIndex];
            FindTarget(tier.range);
        }

        private void FindTarget(float range)
        {
            MonsterController closestMonster = null;
            float sqrRange = range * range;
            Vector3 towerGroundPos = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f), transform.position.z);

            // 0. 우선 순위 타겟 확인
            if (priorityTarget != null && priorityTarget.gameObject.activeInHierarchy)
            {
                Vector3 monsterGroundPos = priorityTarget.transform.position;
                if (priorityTarget.GetFlyType() == MonsterFlyType.Air) monsterGroundPos -= Vector3.up;
                
                if (Vector2.SqrMagnitude(towerGroundPos - monsterGroundPos) <= sqrRange)
                {
                    currentTarget = priorityTarget;
                    currentObstacleTarget = null;
                    return;
                }
            }
            else if (priorityObstacleTarget != null && priorityObstacleTarget.gameObject.activeInHierarchy)
            {
                if (Vector2.SqrMagnitude(towerGroundPos - priorityObstacleTarget.transform.position) <= sqrRange)
                {
                    currentObstacleTarget = priorityObstacleTarget;
                    currentTarget = null;
                    return;
                }
            }

            // 1. 일반 몬스터 탐색 (기지에 가장 가까운 적 우선 - 남은 경로 거리 기반)
            float minRemainingDistance = float.MaxValue;
            
            for (int i = 0; i < MonsterController.ActiveMonsters.Count; i++)
            {
                var monster = MonsterController.ActiveMonsters[i];
                if (monster == null || !monster.gameObject.activeInHierarchy) continue;

                // 지상/공중 공격 타입 엄격 체크
                if (!IsValidTargetType(monster.GetFlyType())) continue;

                Vector3 monsterGroundPos = monster.transform.position;
                if (monster.GetFlyType() == MonsterFlyType.Air) monsterGroundPos -= Vector3.up;
                
                if (Vector2.SqrMagnitude(towerGroundPos - monsterGroundPos) <= sqrRange)
                {
                    float remainingDist = monster.GetRemainingPathDistance();
                    
                    // [지능형 타겟팅 로직 적용]
                    // 즉발형 공격이 아닌 경우, 발사체가 도달하기 전에 몬스터가 기지에 도달하면 타겟팅 포기
                    if (data.attackType != AttackType.ContinuousHoming && data.attackType != AttackType.AreaSelf)
                    {
                        float distToMonster = Vector2.Distance(towerGroundPos, monsterGroundPos);
                        float projectileSpeed = data.upgradeTiers[currentTierIndex].projectileSpeed;
                        
                        float timeForProjectile = distToMonster / Mathf.Max(0.01f, projectileSpeed);
                        float timeForMonster = remainingDist / Mathf.Max(0.01f, monster.GetCurrentSpeed());
                        
                        // 몬스터가 기지에 도착하는 시간이 발사체 명중 시간보다 짧거나 같다면 무시
                        if (timeForMonster <= timeForProjectile)
                        {
                            continue;
                        }
                    }

                    if (remainingDist < minRemainingDistance)
                    {
                        minRemainingDistance = remainingDist;
                        closestMonster = monster;
                    }
                }
            }

            currentTarget = closestMonster;
            currentObstacleTarget = null;
        }

        private void FireProjectile()
        {
            if (data.assets == null || (currentTarget == null && currentObstacleTarget == null)) return;
            var tier = data.upgradeTiers[currentTierIndex];
            Vector3 tGroundPos = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f), transform.position.z);
            if (data.attackType == AttackType.AreaSelf && data.assets.projectilePrefab == null)
            {
                ExplodeAreaSelf(tGroundPos, tier.range, tier.damage);
                return;
            }
            if (data.assets.projectilePrefab == null) return;
            Vector3 spawnPos = (data.attackType == AttackType.AreaSelf) ? tGroundPos : cachedTransform.position;
            GameObject projObj = ObjectPoolManager.Instance.SpawnFromPool(data.assets.projectilePrefab, spawnPos, Quaternion.identity);
            if (projObj != null)
            {
                var projectile = projObj.GetComponent<ProjectileController>();
                if (projectile != null)
                {
                    projectile.Initialize(data.attackType, data.targetType, tier.range, tGroundPos, currentTarget, tier.damage, data.attackAttribute, data.assets.hitEffectPrefab, tier.projectileSprite, tier.projectileAnim, data.assets.projectileScale, tier.projectileSpeed);
                    projectile.SetObstacleTarget(currentObstacleTarget);
                }
            }
        }

        private void ExplodeAreaSelf(Vector3 center, float range, float damage)
        {
            float sqrRange = range * range;
            for (int i = MonsterController.ActiveMonsters.Count - 1; i >= 0; i--)
            {
                var monster = MonsterController.ActiveMonsters[i];
                if (monster != null && monster.gameObject.activeInHierarchy)
                {
                    if (!IsValidTargetType(monster.GetFlyType())) continue;
                    Vector3 monsterGroundPos = monster.transform.position;
                    if (monster.GetFlyType() == MonsterFlyType.Air) monsterGroundPos -= Vector3.up;
                    if (Vector2.SqrMagnitude(center - monsterGroundPos) <= sqrRange)
                    {
                        monster.TakeDamage(damage);
                        if (!monster.IsImmuneTo(data.attackAttribute))
                        {
                            var status = monster.GetComponent<StatusEffectManager>();
                            if (status != null)
                            {
                                var settings = AttackAttributeSettings.Instance;
                                switch (data.attackAttribute)
                                {
                                    case AttackAttribute.Cold: status.ApplySlow(settings.iceDuration, settings.iceSlowMultiplier); break;
                                    case AttackAttribute.Fire: status.ApplyBurn(settings.fireDuration, damage * settings.fireBurnDamageRatio); break;
                                    case AttackAttribute.Electric: status.ApplyStun(settings.lightningStunDuration); break;
                                }
                            }
                        }
                        if (data.assets.hitEffectPrefab != null) ObjectPoolManager.Instance.SpawnFromPool(data.assets.hitEffectPrefab, monster.transform.position, Quaternion.identity);
                    }
                }
            }

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

        public bool IsValidTargetType(MonsterFlyType flyType)
        {
            if (data.targetType == TargetType.Both || data.targetType == TargetType.Special) return true;
            if (data.targetType == TargetType.Ground && flyType == MonsterFlyType.Ground) return true;
            if (data.targetType == TargetType.Flying && flyType == MonsterFlyType.Air) return true;
            return false;
        }

        public bool UpgradeTower()
        {
            if (currentTierIndex >= data.upgradeTiers.Count - 1) return false;
            int nextCost = data.upgradeTiers[currentTierIndex + 1].buildOrUpgradeCost;
            if (GameManager.Instance.UseGold(nextCost))
            {
                currentTierIndex++;
                return true;
            }
            return false;
        }
    }
}
