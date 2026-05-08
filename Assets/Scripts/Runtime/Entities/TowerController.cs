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

        private float searchTimer = 0f;
        private const float SEARCH_INTERVAL = 0.2f;

        private Transform cachedTransform;

        // 선택 및 사거리 시각화
        private LineRenderer currentRangeLine;
        private LineRenderer nextRangeLine;
        
        // 지속형 유도 공격(ContinuousHoming)용 레이저 빔
        private LineRenderer laserBeam;
        
        private Animator animator; // 애니메이터 캐싱
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
            cachedTransform = transform; // 최적화
            
            // 사거리 원 그리기용 LineRenderer 생성
            currentRangeLine = CreateRangeCircle("CurrentRange", Color.green);
            nextRangeLine = CreateRangeCircle("NextRange", Color.yellow);
            
            // 지속형 유도 공격용 레이저 생성
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
            lr.useWorldSpace = true; // 부모 스케일 영향을 받지 않도록 월드 좌표 사용
            lr.positionCount = 51; // 원을 그릴 점의 갯수
            lr.sortingOrder = 5000; // 모든 유닛보다 항상 위에 표시
            lr.gameObject.SetActive(false);

            return lr;
        }

        public TowerData GetData() => data;
        public int GetCurrentTierIndex() => currentTierIndex;

        public void Deselect()
        {
            currentRangeLine.gameObject.SetActive(false);
            nextRangeLine.gameObject.SetActive(false);
        }

        public void Select()
        {
            if (data != null && data.upgradeTiers != null)
            {
                // 현재 사거리 그리기
                float currentRange = data.upgradeTiers[currentTierIndex].range;
                UpdateCircle(currentRangeLine, currentRange);
                currentRangeLine.gameObject.SetActive(true);

                // 다음 업그레이드 사거리 그리기 (존재할 경우)
                if (currentTierIndex < data.upgradeTiers.Count - 1)
                {
                    float nextRange = data.upgradeTiers[currentTierIndex + 1].range;
                    UpdateCircle(nextRangeLine, nextRange);
                    nextRangeLine.gameObject.SetActive(true);
                }
            }
        }

        private void UpdateCircle(LineRenderer lr, float radius)
        {
            float x, y;
            float angle = 0f;
            // 타워의 실제 바닥(그라운드) 좌표를 기준으로 원을 그림
            Vector3 center = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f), transform.position.z);
            for (int i = 0; i < 51; i++)
            {
                x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
                lr.SetPosition(i, new Vector3(center.x + x, center.y + y, 0));
                angle += (360f / 50f);
            }
        }

        // 맵 그리드 좌표
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
                    // 애니메이션만 등록될 경우를 대비해 스프라이트가 비어있어도 머티리얼과 색상은 강제 세팅
                    sr.material = new Material(Shader.Find("Sprites/Default"));
                    sr.color = Color.white;

                    if (data.assets.idleSprite != null) {
                        sr.sprite = data.assets.idleSprite;
                    } else if (data.assets.idleAnim != null) {
                        data.assets.idleAnim.SampleAnimation(gameObject, 0f); // 첫 프레임 강제 적용
                    }
                    UpdateSortingOrder();
                }

                // 하단 정렬 (타일의 하단 경계에 발을 맞춤)
                // 타일의 월드 좌표는 중심점이므로, 하단으로 내렸다가 스케일 절반만큼 위로 올림
                Vector3 centerPos = transform.position;
                transform.position = new Vector3(centerPos.x, centerPos.y - 0.5f + (data.assets.visualScale * 0.5f), centerPos.z);

                // 애니메이터 설정
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
                // 기준값 1000에서 Y값이 작을수록(하단), X값이 작을수록(좌측) 큰 값을 가지도록 수식 적용
                sr.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 100f) - Mathf.RoundToInt(transform.position.x * 10f);
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
                // 타겟이 이미 죽었거나 비활성화되었으면 타겟 초기화
                if (!currentTarget.gameObject.activeInHierarchy)
                {
                    currentTarget = null;
                    RevertToIdle();
                    if (laserBeam != null) laserBeam.gameObject.SetActive(false);
                    return;
                }

                // 타겟이 사거리를 벗어났는지 확인 (바닥 좌표 기준)
                Vector3 monsterGroundPos = currentTarget.transform.position;
                if (currentTarget.GetFlyType() == MonsterFlyType.Air) monsterGroundPos -= Vector3.up;

                Vector3 towerGroundPos = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f), transform.position.z);

                if (Vector2.SqrMagnitude(towerGroundPos - monsterGroundPos) > currentTier.range * currentTier.range)
                {
                    currentTarget = null;
                    RevertToIdle();
                    if (laserBeam != null) laserBeam.gameObject.SetActive(false);
                    return;
                }

                UpdateAttackSprite(monsterGroundPos);

                // 지속형 유도 공격(ContinuousHoming)의 경우 빔을 그리고 데미지를 즉시 입힘
                if (data.attackType == AttackType.ContinuousHoming)
                {
                    if (laserBeam != null)
                    {
                        laserBeam.gameObject.SetActive(true);
                        // 타워 중심 (포신 정도의 위치: 바닥에서 visualScale 만큼 올라온 지점 등)
                        laserBeam.SetPosition(0, towerGroundPos + Vector3.up * 0.5f);
                        // 타겟 중심
                        laserBeam.SetPosition(1, currentTarget.transform.position);
                    }

                    attackTimer -= Time.deltaTime;
                    if (attackTimer <= 0f)
                    {
                        // 투사체 생성 없이 즉시 데미지
                        currentTarget.TakeDamage(currentTier.damage);
                        
                        // 이펙트 스폰
                        if (data.assets.hitEffectPrefab != null)
                        {
                            ObjectPoolManager.Instance.SpawnFromPool(data.assets.hitEffectPrefab, currentTarget.transform.position, Quaternion.identity);
                        }

                        // 공격 속도에 따른 타격 주기
                        attackTimer = 1f / Mathf.Max(0.1f, currentTier.attackSpeed);
                    }
                    return; // 지속 공격은 여기서 종료 (일반 FireProjectile 미호출)
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
                    // 현재 출력중인 스프라이트(고정 이미지, 8방향, 애니메이션 등)의 실제 크기를 읽어와 1타일 꽉 차게 보정
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
            else // 애니메이션이 없을 때만 고정 이미지 세팅
            {
                if (data.assets != null && data.assets.idleSprite != null)
                {
                    var sr = GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = data.assets.idleSprite;
                }
            }
        }

        private void UpdateAttackSprite(Vector2 targetPos)
        {
            if (data.assets == null || data.assets.attackSprites8Dir == null) return;
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            Vector2 dir = (targetPos - (Vector2)cachedTransform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // 0=Right, 45=UpRight, 90=Up, 135=UpLeft, 180=Left, 225=DownLeft, 270=Down, 315=DownRight
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
            else if (data.assets.attackAnim != null)
            {
                PlayClip(data.assets.attackAnim);
            }
            else // 애니메이션이 없을 때만 고정 이미지 세팅
            {
                if (s != null) sr.sprite = s;
            }
        }

        private void FindTarget(float range)
        {
            float closestDistance = float.MaxValue;
            MonsterController closestMonster = null;
            float sqrRange = range * range;

            for (int i = 0; i < MonsterController.ActiveMonsters.Count; i++)
            {
                var monster = MonsterController.ActiveMonsters[i];
                if (monster != null && monster.gameObject.activeInHierarchy)
                {
                    // 타겟팅 타입 검사
                    bool canAttack = false;
                    var flyType = monster.GetFlyType();
                    if (data.targetType == TargetType.Both || data.targetType == TargetType.Special) canAttack = true;
                    else if (data.targetType == TargetType.Ground && flyType == MonsterFlyType.Ground) canAttack = true;
                    else if (data.targetType == TargetType.Flying && flyType == MonsterFlyType.Air) canAttack = true;

                    if (!canAttack) continue;

                    // 사거리 계산은 몬스터의 "지상(그림자)" 위치 기준 (공중 유닛은 이미지보다 1칸 아래)
                    Vector3 monsterGroundPos = monster.transform.position;
                    if (flyType == MonsterFlyType.Air)
                    {
                        monsterGroundPos -= Vector3.up;
                    }

                    Vector3 towerGroundPos = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f), transform.position.z);
                    float dist = Vector2.SqrMagnitude(towerGroundPos - monsterGroundPos);
                    if (dist <= sqrRange && dist < closestDistance)
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
            if (data.assets == null || currentTarget == null) return;

            var tier = data.upgradeTiers[currentTierIndex];
            Vector3 towerGroundPos = new Vector3(transform.position.x, transform.position.y + 0.5f - (data.assets.visualScale * 0.5f), transform.position.z);
            
            // AreaSelf 타입은 발사체 프리팹이 없어도 즉시 데미지를 가해야 함
            if (data.attackType == AttackType.AreaSelf && data.assets.projectilePrefab == null)
            {
                ExplodeAreaSelf(towerGroundPos, tier.range, tier.damage);
                return;
            }

            if (data.assets.projectilePrefab == null) return;

            // AreaSelf 타입은 발사체를 타워의 바닥 정중앙에서 생성
            Vector3 spawnPos = (data.attackType == AttackType.AreaSelf) ? towerGroundPos : cachedTransform.position;

            GameObject projObj = ObjectPoolManager.Instance.SpawnFromPool(
                data.assets.projectilePrefab,
                spawnPos,
                Quaternion.identity
            );

            if (projObj != null)
            {
                var projectile = projObj.GetComponent<ProjectileController>();
                if (projectile != null)
                {
                    projectile.Initialize(data.attackType, data.targetType, tier.range, towerGroundPos, currentTarget, tier.damage, data.attackAttribute, data.assets.hitEffectPrefab, tier.projectileSprite, tier.projectileAnim, data.assets.projectileScale);
                }
            }
        }

        private void ExplodeAreaSelf(Vector3 center, float range, float damage)
        {
            float sqrRange = range * range;
            // 리스트 요소가 제거될 수 있으므로 역순 순회
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
                        if (data.assets.hitEffectPrefab != null)
                        {
                            ObjectPoolManager.Instance.SpawnFromPool(data.assets.hitEffectPrefab, monster.transform.position, Quaternion.identity);
                        }
                    }
                }
            }
        }

        private bool IsValidTargetType(MonsterFlyType flyType)
        {
            if (data.targetType == TargetType.Both || data.targetType == TargetType.Special) return true;
            if (data.targetType == TargetType.Ground && flyType == MonsterFlyType.Ground) return true;
            if (data.targetType == TargetType.Flying && flyType == MonsterFlyType.Air) return true;
            return false;
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
