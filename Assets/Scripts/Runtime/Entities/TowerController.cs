using UnityEngine;
using TDF.Core.Data;
using TDF.Runtime.Managers;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Animations;

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
        private AnimationClip currentPlayingClip;
        private PlayableGraph playableGraph;
        private AnimationClipPlayable clipPlayable;
        private AnimationPlayableOutput playableOutput;
        private bool isPlayableActive = false;
        private bool isAttackingAnimPlaying = false;
        private float attackAnimTimer = 0f;
        private AnimationClip lastAttackClip = null;
        private SpriteRenderer tierVisualRenderer;
        private float currentHp;

        private void PlayClipWithPlayables(AnimationClip clip, bool loop, bool shouldPlay = true)
        {
            if (clip == null) return;
            
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = gameObject.AddComponent<Animator>();
                }
            }
            animator.enabled = true;

            if (isPlayableActive && currentPlayingClip == clip && playableGraph.IsValid())
            {
                if (shouldPlay && !playableGraph.IsPlaying())
                {
                    playableGraph.Play();
                }
                else if (!shouldPlay && playableGraph.IsPlaying())
                {
                    clipPlayable.SetTime(0.0);
                    playableGraph.Evaluate();
                    playableGraph.Stop();
                }
                return;
            }

            CleanUpPlayableGraph();

            currentPlayingClip = clip;

            playableGraph = PlayableGraph.Create("TowerPlayableGraph");
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
            clipPlayable = AnimationClipPlayable.Create(playableGraph, clip);
            clipPlayable.SetApplyFootIK(false);

            playableOutput.SetSourcePlayable(clipPlayable);
            
            if (shouldPlay)
            {
                playableGraph.Play();
            }
            else
            {
                clipPlayable.SetTime(0.0);
                playableGraph.Evaluate();
                playableGraph.Stop();
            }
            
            isPlayableActive = true;
        }

        private void CleanUpPlayableGraph()
        {
            if (isPlayableActive && playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }
            isPlayableActive = false;
        }

        private void OnDestroy()
        {
            CleanUpPlayableGraph();
        }

        private void OnDisable()
        {
            CleanUpPlayableGraph();
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
                currentRangeFill.transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f) - (data.assets.visualOffsetY - 1.0f), transform.position.z);
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
            Vector3 center = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f) - (data.assets.visualOffsetY - 1.0f), transform.position.z);
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
            if (data != null) currentHp = data.maxHp;
            
            if (data.assets != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.material = new Material(Shader.Find("Sprites/Default"));
                    sr.color = Color.white;
                    if (data.assets.idleSprite != null) sr.sprite = data.assets.idleSprite;
                    else if (data.assets.idleAnim != null)
                    {
                        PlayClipWithPlayables(data.assets.idleAnim, true, true);
                    }
                    UpdateSortingOrder();
                }
                Vector3 centerPos = transform.position;
                transform.position = new Vector3(centerPos.x, centerPos.y - 0.5f + (data.assets.visualScale * 0.5f) + (data.assets.visualOffsetY - 1.0f), centerPos.z);
                var existingAnimator = GetComponent<Animator>();
                if (data.assets.animatorController != null)
                {
                    if (animator == null) animator = existingAnimator != null ? existingAnimator : gameObject.AddComponent<Animator>();
                    animator.runtimeAnimatorController = data.assets.animatorController;
                    animator.enabled = true;
                }
                else
                {
                    if (existingAnimator != null)
                    {
                        existingAnimator.enabled = false;
                    }
                }
                
                UpdateTierVisual(currentTierIndex);
            }
        }

        private void UpdateTierVisual(int tierIndex)
        {
            if (data == null || data.upgradeTiers == null || tierIndex >= data.upgradeTiers.Count) return;
            var tier = data.upgradeTiers[tierIndex];

            if (tierVisualRenderer == null)
            {
                Transform tierVisualTransform = transform.Find("TierVisual");
                if (tierVisualTransform != null)
                {
                    tierVisualRenderer = tierVisualTransform.GetComponent<SpriteRenderer>();
                }
                else
                {
                    GameObject tierVisualObj = new GameObject("TierVisual");
                    tierVisualObj.transform.SetParent(transform);
                    tierVisualRenderer = tierVisualObj.AddComponent<SpriteRenderer>();
                }
            }

            if (tier.tierSprite != null)
            {
                tierVisualRenderer.gameObject.SetActive(true);
                tierVisualRenderer.sprite = tier.tierSprite;
                tierVisualRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }
            else
            {
                tierVisualRenderer.gameObject.SetActive(false);
            }
            UpdateSortingOrder();
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
                if (tierVisualRenderer != null)
                {
                    tierVisualRenderer.sortingOrder = sr.sortingOrder + 10;
                }
            }
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (data == null || data.upgradeTiers.Count == 0) return;

            if (isAttackingAnimPlaying)
            {
                attackAnimTimer += Time.deltaTime;
                float clipLength = lastAttackClip != null ? lastAttackClip.length : 0.2f;
                if (attackAnimTimer >= clipLength)
                {
                    isAttackingAnimPlaying = false;
                    if (currentTarget == null && currentObstacleTarget == null)
                    {
                        RevertToIdle();
                    }
                }
            }

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
                    if (!isAttackingAnimPlaying)
                    {
                        RevertToIdle();
                    }
                    if (laserBeam != null) laserBeam.gameObject.SetActive(false);
                    return;
                }

                Vector3 towerGroundPos = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f) - (data.assets.visualOffsetY - 1.0f), transform.position.z);
                if (Vector2.SqrMagnitude(towerGroundPos - targetPos) > currentTier.range * currentTier.range)
                {
                    currentTarget = null;
                    currentObstacleTarget = null;
                    if (!isAttackingAnimPlaying)
                    {
                        RevertToIdle();
                    }
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
                if (!isAttackingAnimPlaying)
                {
                    RevertToIdle();
                }
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

                        if (tierVisualRenderer != null && tierVisualRenderer.gameObject.activeSelf && tierVisualRenderer.sprite != null)
                        {
                            float tierMaxBound = Mathf.Max(tierVisualRenderer.sprite.bounds.size.x, tierVisualRenderer.sprite.bounds.size.y);
                            if (tierMaxBound > 0.001f)
                            {
                                var currentTier = data.upgradeTiers[currentTierIndex];
                                float targetLocalScale = currentTier.tierScale / (currentScale * tierMaxBound);
                                tierVisualRenderer.transform.localScale = Vector3.one * targetLocalScale;

                                // 부모 타워의 배치 좌표(yOffset)를 활용해 원래 타일 격자의 월드 중심점 복원
                                float yOffset = -0.5f + (data.assets.visualScale * 0.5f) + (data.assets.visualOffsetY - 1.0f);
                                Vector3 gridCenter = new Vector3(transform.position.x, transform.position.y - yOffset, transform.position.z);

                                // 월드 좌표 기준으로 우측 하단 구석에 티어 스프라이트 배치 (넘침 방지)
                                float targetWorldX = gridCenter.x + 0.5f - (currentTier.tierScale * 0.5f);
                                float targetWorldY = gridCenter.y - 0.5f + (currentTier.tierScale * 0.5f);
                                tierVisualRenderer.transform.position = new Vector3(targetWorldX, targetWorldY, gridCenter.z);
                            }
                        }
                    }
                }
            }
        }

        private void RevertToIdle()
        {
            currentPlayingClip = null;
            isAttackingAnimPlaying = false;
            CleanUpPlayableGraph();

            if (data.assets.animatorController != null)
            {
                if (animator != null)
                {
                    animator.enabled = true;
                    animator.SetBool("IsAttacking", false);
                }
            }
            else if (data.assets.idleAnim != null)
            {
                PlayClipWithPlayables(data.assets.idleAnim, true, true);
            }
            else if (data.assets != null && data.assets.idleSprite != null)
            {
                if (animator != null) animator.enabled = false;
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

            AnimationClip clip = null;
            if (data.assets.attackSprites8Dir != null)
            {
                int octant = Mathf.RoundToInt(angle / 45f) % 8;
                var dirs = data.assets.attackSprites8Dir;
                switch (octant)
                {
                    case 0: clip = dirs.right; break;
                    case 1: clip = dirs.upRight; break;
                    case 2: clip = dirs.up; break;
                    case 3: clip = dirs.upLeft; break;
                    case 4: clip = dirs.left; break;
                    case 5: clip = dirs.downLeft; break;
                    case 6: clip = dirs.down; break;
                    case 7: clip = dirs.downRight; break;
                }
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
            else if (isAttackingAnimPlaying)
            {
                if (clip != null)
                {
                    PlayClipWithPlayables(clip, false, true);
                    lastAttackClip = clip;
                }
                else if (data.assets.attackAnim != null)
                {
                    PlayClipWithPlayables(data.assets.attackAnim, false, true);
                    lastAttackClip = data.assets.attackAnim;
                }
            }
            else
            {
                if (clip != null)
                {
                    PlayClipWithPlayables(clip, false, false);
                    lastAttackClip = clip;
                }
                else
                {
                    RevertToIdle();
                }
            }
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
            Vector3 towerGroundPos = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f) - (data.assets.visualOffsetY - 1.0f), transform.position.z);

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
            
            isAttackingAnimPlaying = true;
            attackAnimTimer = 0f;

            if (isPlayableActive && playableGraph.IsValid())
            {
                clipPlayable.SetTime(0.0);
                playableGraph.Play();
            }

            var tier = data.upgradeTiers[currentTierIndex];
            Vector3 tGroundPos = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f) - (data.assets.visualOffsetY - 1.0f), transform.position.z);
            if (data.attackType == AttackType.AreaSelf && data.assets.projectilePrefab == null)
            {
                ExplodeAreaSelf(tGroundPos, tier.range, tier.damage);
                return;
            }
            if (data.assets.projectilePrefab == null) return;
            Vector3 spawnPos;
            if (data.attackType == AttackType.AreaSelf)
            {
                spawnPos = tGroundPos;
            }
            else if (data.attackType == AttackType.AreaProjectile)
            {
                spawnPos = currentTarget != null ? currentTarget.transform.position : currentObstacleTarget.transform.position;
            }
            else
            {
                spawnPos = cachedTransform.position;
            }
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
            
            // towerPoint 값에 따른 최대 업그레이드 가능 티어 매핑
            int userTowerPoint = UserDataManager.Instance != null ? UserDataManager.Instance.GetTowerPoint(data.towerId) : data.towerPoint;
            int maxUpgradeTier = 0;
            if (userTowerPoint <= 1) maxUpgradeTier = 0;
            else if (userTowerPoint <= 3) maxUpgradeTier = 1;
            else if (userTowerPoint <= 6) maxUpgradeTier = 2;
            else maxUpgradeTier = 3;

            if (currentTierIndex >= maxUpgradeTier)
            {
                Debug.LogWarning($"[TowerController] 타워 포인트 제한으로 인해 더 이상 업그레이드할 수 없습니다. 타워 포인트: {userTowerPoint}, 현재 티어: {currentTierIndex}, 최대 티어: {maxUpgradeTier}");
                return false;
            }

            var nextTier = data.upgradeTiers[currentTierIndex + 1];
            int nextCost = nextTier.buildOrUpgradeCost;
            if (GameManager.Instance.UseGold(nextCost))
            {
                currentTierIndex++;
                UpdateTierVisual(currentTierIndex);
                return true;
            }
            return false;
        }
    }
}
