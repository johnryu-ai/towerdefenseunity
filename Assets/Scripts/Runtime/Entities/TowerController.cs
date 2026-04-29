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

        private void Awake()
        {
            // 사거리 원 그리기용 LineRenderer 생성
            currentRangeLine = CreateRangeCircle("CurrentRange", Color.green);
            nextRangeLine = CreateRangeCircle("NextRange", Color.yellow);
        }

        private LineRenderer CreateRangeCircle(string name, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;

            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.startWidth = 0.5f;
            lr.endWidth = 0.5f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            lr.useWorldSpace = false;
            lr.positionCount = 51; // 원을 그릴 점의 갯수
            lr.sortingOrder = 20;
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
            for (int i = 0; i < 51; i++)
            {
                x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
                lr.SetPosition(i, new Vector3(x, y, 0));
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
            
            if (data.assets != null)
            {
                // 스케일 적용
                transform.localScale = Vector3.one * data.assets.visualScale;

                // 하단 정렬 (타일의 하단 경계에 발을 맞춤)
                // 타일의 월드 좌표는 중심점이므로, 하단으로 내렸다가 스케일 절반만큼 위로 올림
                Vector3 centerPos = transform.position;
                transform.position = new Vector3(centerPos.x, centerPos.y - 0.5f + (data.assets.visualScale * 0.5f), centerPos.z);

                if (data.assets.idleSprite != null)
                {
                    var sr = GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = data.assets.idleSprite;
                        // 어둡게 보이는 문제 해결: 머티리얼을 기본 언릿 스프라이트용으로 강제 설정
                        sr.material = new Material(Shader.Find("Sprites/Default"));
                        sr.color = Color.white;
                        UpdateSortingOrder();
                    }
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
                // 타겟이 사거리를 벗어났는지 확인
                if (Vector2.SqrMagnitude(cachedTransform.position - currentTarget.transform.position) > currentTier.range * currentTier.range)
                {
                    currentTarget = null;
                    RevertToIdle();
                    return;
                }

                UpdateAttackSprite(currentTarget.transform.position);

                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    FireProjectile();
                    // attackSpeed가 1초당 공격 횟수라면 쿨타임은 1 / attackSpeed
                    attackTimer = 1f / Mathf.Max(0.1f, currentTier.attackSpeed);
                }
            }
            else
            {
                RevertToIdle();
            }
        }

        private void LateUpdate()
        {
            // 애니메이터 덮어씌우기 방지 및 해상도가 큰 스프라이트의 정규화 처리
            if (data != null && data.assets != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    // 스프라이트의 가로/세로 중 가장 큰 원본 사이즈 추출
                    float maxBound = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                    if (maxBound > 0.001f)
                    {
                        // 원본 크기가 달라도 visualScale(타일 크기) 안에 들어가도록 비율 조정
                        float normalizedScale = data.assets.visualScale / maxBound;
                        transform.localScale = Vector3.one * normalizedScale;
                    }
                    else
                    {
                        transform.localScale = Vector3.one * data.assets.visualScale;
                    }
                }
                else
                {
                    transform.localScale = Vector3.one * data.assets.visualScale;
                }
            }
        }

        private void RevertToIdle()
        {
            if (data.assets != null && data.assets.idleSprite != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = data.assets.idleSprite;
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

            if (s != null) sr.sprite = s;
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
                    float dist = Vector2.SqrMagnitude(cachedTransform.position - monster.transform.position);
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
                    projectile.Initialize(currentTarget, tier.damage, data.attackAttribute, data.assets.hitEffectPrefab, tier.projectileSprite);
                    
                    // 발사체 스케일 적용
                    projectile.transform.localScale = Vector3.one * data.assets.projectileScale;
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
