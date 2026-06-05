using System.Collections.Generic;
using UnityEngine;
using TDF.Core.Data;
using TDF.Runtime.Managers;

namespace TDF.Runtime.Entities
{
    [RequireComponent(typeof(StatusEffectManager))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class MonsterController : MonoBehaviour
    {
        private MonsterData data;
        private StatusEffectManager statusEffects;
        private SpriteRenderer spriteRenderer;
        private Animator animator; // 애니메이터 캐싱
        private float animTime = 0f;
        private AnimationClip currentPlayingClip;
        private GameObject shadowObj;
        private Sprite shadowSprite;

        // 체력바 관련
        private GameObject hpBarBg;
        private GameObject hpBarFg;
        private SpriteRenderer hpBarBgSr;
        private SpriteRenderer hpBarFgSr;
        private static Sprite cachedHpSprite;

        private void PlayClip(AnimationClip clip)
        {
            if (clip == null) return;
            clip.legacy = true; // Force legacy flag so SampleAnimation works on modern AnimationClips at runtime
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

        private float currentHealth;
        private float displayedHealth;
        private List<Vector2> waypoints;
        private int currentWaypointIndex;
        private bool isProcessed = false; // 중복 처리 방지 플래그
        private float currentAltitude = 1.0f; // 공중 유닛의 현재 고도 (다이브 시 사용)

        public static List<MonsterController> ActiveMonsters = new List<MonsterController>();

        // 최적화를 위한 캐싱
        private Transform cachedTransform;

        private void OnEnable()
        {
            if (!ActiveMonsters.Contains(this)) ActiveMonsters.Add(this);
        }

        private static Sprite cachedShadowSprite;

        private void OnDisable()
        {
            ActiveMonsters.Remove(this);
        }

        private void Awake()
        {
            cachedTransform = transform;
            statusEffects = GetComponent<StatusEffectManager>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // 그림자용 스프라이트 캐싱 처리 (반투명 회색)
            if (cachedShadowSprite == null)
            {
                Texture2D tex = new Texture2D(64, 64);
                Color[] colors = new Color[64 * 64];
                Vector2 center = new Vector2(32, 32);
                for (int i = 0; i < 64 * 64; i++) {
                    int x = i % 64; int y = i / 64;
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = dist < 32 ? Mathf.Lerp(0.6f, 0f, dist / 32f) : 0f;
                    colors[i] = new Color(0.3f, 0.3f, 0.3f, alpha); // 어두운 회색
                }
                tex.SetPixels(colors);
                tex.Apply();
                cachedShadowSprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            }

            // 체력바용 단색 스프라이트 캐싱 처리 (Pivot을 Left(0, 0.5)로 설정하여 스케일 조절이 한쪽으로만 되게 함)
            if (cachedHpSprite == null)
            {
                Texture2D tex = new Texture2D(2, 2);
                Color[] colors = new Color[4];
                for(int i=0; i<4; i++) colors[i] = Color.white;
                tex.SetPixels(colors);
                tex.Apply();
                // pixelsPerUnit을 2로 주어 2x2 픽셀이 월드 기준 1x1 크기가 되도록 설정
                cachedHpSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0f, 0.5f), 2f);
            }
        }

        public void Initialize(MonsterData monsterData, List<Vector2> pathWaypoints)
        {
            data = monsterData;
            waypoints = pathWaypoints;
            currentWaypointIndex = 0;
            isProcessed = false; // 스폰 시 플래그 초기화
            currentAltitude = 1.0f;

            if (data != null && data.stats != null)
            {
                currentHealth = data.stats.health;
                displayedHealth = currentHealth;
            }

            if (data != null && data.assets != null)
            {
                if (data.assets.moveSprite != null)
                {
                    spriteRenderer.sprite = data.assets.moveSprite;
                }
                else if (data.assets.moveAnim != null)
                {
                    data.assets.moveAnim.legacy = true;
                    data.assets.moveAnim.SampleAnimation(gameObject, 0f);
                }

                // 어둡게 보이는 문제 해결: 머티리얼 강제 설정 (모든 자식 렌더러 포함)
                var allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in allRenderers)
                {
                    if (shadowObj != null && sr.gameObject == shadowObj) continue;
                    sr.gameObject.SetActive(true); // 강제 오브젝트 활성화 (프리팹에서 꺼져있을 수 있음)
                    sr.material = new Material(Shader.Find("Sprites/Default"));
                    sr.color = Color.white;
                    sr.enabled = true; // 강제 렌더러 활성화
                }

                // 하단 정렬 (발 위치를 타일 하단에 맞춤)
                float safeScale = data.assets.visualScale <= 0.01f ? 1f : data.assets.visualScale;
                float yOffset = -0.5f + (safeScale * 0.5f) + (data.assets.visualOffsetY - 1.0f);
                if (data.flyType == MonsterFlyType.Air)
                {
                    yOffset += 1.0f; // 공중 몬스터는 한칸 위
                }
                transform.position = new Vector3(transform.position.x, transform.position.y + yOffset, transform.position.z);

                // 애니메이터 설정
                if (data.assets.animatorController != null)
                {
                    if (animator == null) animator = gameObject.AddComponent<Animator>();
                    animator.runtimeAnimatorController = data.assets.animatorController;
                }
            }

            // 클릭을 위한 콜라이더 추가
            var col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;

            UpdateSortingOrder();

            // 그림자 초기화
            if (data != null && data.flyType == MonsterFlyType.Air)
            {
                if (shadowObj == null)
                {
                    Transform existingShadow = transform.Find("Shadow");
                    if (existingShadow != null)
                    {
                        shadowObj = existingShadow.gameObject;
                    }
                    else
                    {
                        shadowObj = new GameObject("Shadow");
                        shadowObj.transform.SetParent(this.transform);
                        var shadowSr = shadowObj.AddComponent<SpriteRenderer>();
                        shadowSr.sprite = cachedShadowSprite;
                        shadowObj.transform.localScale = new Vector3(1f, 0.5f, 1f);
                    }
                }
                shadowObj.transform.localPosition = new Vector3(0, -1f, 0);
                shadowObj.SetActive(true);
            }
            else
            {
                if (shadowObj != null) shadowObj.SetActive(false);
            }

            // 체력바 초기화
            if (hpBarBg == null)
            {
                Transform existingBg = transform.Find("HpBarBg");
                if (existingBg != null)
                {
                    hpBarBg = existingBg.gameObject;
                    hpBarBgSr = hpBarBg.GetComponent<SpriteRenderer>();
                    Transform existingFg = hpBarBg.transform.Find("HpBarFg");
                    if (existingFg != null)
                    {
                        hpBarFg = existingFg.gameObject;
                        hpBarFgSr = hpBarFg.GetComponent<SpriteRenderer>();
                    }
                }
                else
                {
                    hpBarBg = new GameObject("HpBarBg");
                    hpBarBg.transform.SetParent(this.transform);
                    hpBarBgSr = hpBarBg.AddComponent<SpriteRenderer>();
                    hpBarBgSr.sprite = cachedHpSprite;
                    hpBarBgSr.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                    hpBarBgSr.material = new Material(Shader.Find("Sprites/Default"));

                    hpBarFg = new GameObject("HpBarFg");
                    hpBarFg.transform.SetParent(hpBarBg.transform); // 배경의 자식으로 등록
                    hpBarFgSr = hpBarFg.AddComponent<SpriteRenderer>();
                    hpBarFgSr.sprite = cachedHpSprite;
                    hpBarFgSr.color = Color.green;
                    hpBarFgSr.material = new Material(Shader.Find("Sprites/Default"));
                    
                    hpBarFg.transform.localPosition = new Vector3(0, 0, -0.1f); // 약간 앞으로
                }
            }
            hpBarBg.SetActive(true);
            hpBarFg.SetActive(true);

            statusEffects.ResetEffects();
        }

        public void RecalculatePath()
        {
            if (data == null || data.flyType == MonsterFlyType.Air) return;
            
            Vector3 worldPos = transform.position;
            float worldOffsetX = Map.MapController.Instance.offsetX;
            float worldOffsetY = Map.MapController.Instance.offsetY;
            int gx = Mathf.RoundToInt((worldPos.x - worldOffsetX) / Map.MapController.TILE_SIZE);
            int gy = Mathf.RoundToInt((worldPos.y - worldOffsetY) / Map.MapController.TILE_SIZE);
            
            // 현재 진행 방향 계산 (웨이포인트가 있으면)
            Vector2Int dirHint = Vector2Int.zero;
            if (waypoints != null && currentWaypointIndex < waypoints.Count)
            {
                Vector2 moveDir = (waypoints[currentWaypointIndex] - (Vector2)transform.position).normalized;
                if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y)) dirHint = new Vector2Int(moveDir.x > 0 ? 1 : -1, 0);
                else dirHint = new Vector2Int(0, moveDir.y > 0 ? 1 : -1);
            }

            var newPath = Map.MapController.Instance.GetPath(-1, new Vector2Int(gx, gy), dirHint);
            if (newPath != null && newPath.Count > 0)
            {
                waypoints = newPath;
                
                // 이미 현재 타일 중심을 지나 다음 타일로 향하고 있는 경우, 첫 번째 웨이포인트를 건너뜀
                if (newPath.Count > 1)
                {
                    Vector2 toFirst = (newPath[0] - (Vector2)transform.position).normalized;
                    Vector2 toSecond = (newPath[1] - (Vector2)transform.position).normalized;
                    
                    // 첫 번째 웨이포인트와 두 번째 웨이포인트가 서로 반대 방향에 있다면 (즉, 이미 그 사이 어딘가에 있다면)
                    if (Vector2.Dot(toFirst, toSecond) < -0.5f || Vector2.Distance(transform.position, newPath[0]) < 0.3f)
                    {
                        currentWaypointIndex = 1;
                    }
                    else
                    {
                        currentWaypointIndex = 0;
                    }
                }
                else
                {
                    currentWaypointIndex = 0;
                }
                // Debug.Log($"[MonsterController] Path updated for {gameObject.name}. New target: {waypoints[currentWaypointIndex]}");
            }
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (statusEffects.IsStunned) return;

            Move();

            if (data != null && data.assets != null && data.assets.animatorController == null && data.assets.moveAnim != null)
            {
                PlayClip(data.assets.moveAnim);
            }
        }

        private void LateUpdate()
        {
            if (data != null && data.assets != null)
            {
                var allRenderers = GetComponentsInChildren<SpriteRenderer>();
                float maxBound = 0f;
                foreach (var sr in allRenderers)
                {
                    if (sr.gameObject.name == "Shadow") continue;
                    if (sr.gameObject.name == "HpBarBg") continue;
                    if (sr.gameObject.name == "HpBarFg") continue;
                    if (sr.gameObject.name == "FireEffect" || sr.gameObject.name == "IceEffect") continue;
                    if (sr.transform.parent != null && (sr.transform.parent.name == "FireEffect" || sr.transform.parent.name == "IceEffect")) continue;
                    if (sr.sprite != null)
                    {
                        float bound = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                        if (bound > maxBound) maxBound = bound;
                    }
                }

                if (maxBound > 0.001f)
                {
                    float safeScale = data.assets.visualScale <= 0.01f ? 1f : data.assets.visualScale;
                    float currentScale = safeScale / maxBound;
                    transform.localScale = Vector3.one * currentScale;
                }
            }

            // 그림자 월드 위치 및 크기 강제 고정 (부모 스케일의 영향을 무시하고 항상 경로상에 표시)
            if (shadowObj != null && shadowObj.activeSelf)
            {
                // 공중 유닛의 이미지(transform.position)는 현재 고도(currentAltitude)만큼 떠있으므로, 그만큼 빼주고 visualOffsetY도 보정해줌
                shadowObj.transform.position = transform.position - (Vector3.up * currentAltitude) - (Vector3.up * (data.assets.visualOffsetY - 1.0f));
                
                // 1칸 격자를 넘지 않는 좌우로 긴 반투명 회색 타원 (스프라이트가 0.64크기이므로 1.4배하면 0.9정도, 높이는 0.3배하면 0.2정도)
                // 부모의 로컬 스케일에 반비례하게 적용하여 월드 크기를 1칸 내로 항상 일정하게 유지
                float invScale = 1f / Mathf.Max(0.001f, transform.localScale.x);
                shadowObj.transform.localScale = new Vector3(invScale * 1.4f, invScale * 0.4f, 1f);
            }

            // 체력바 위치 및 스케일 업데이트
            if (hpBarBg != null && hpBarBg.activeSelf && data != null && data.stats != null)
            {
                float invScale = 1f / Mathf.Max(0.001f, transform.localScale.x);
                
                // 실제 몬스터 이미지의 최상단(머리 위) 높이 계산
                float topY = transform.position.y;
                var allRenderers = GetComponentsInChildren<SpriteRenderer>();
                foreach(var sr in allRenderers)
                {
                    if (sr.gameObject.name == "Shadow") continue;
                    if (sr.gameObject.name == "HpBarBg") continue;
                    if (sr.gameObject.name == "HpBarFg") continue;
                    if (sr.sprite != null)
                    {
                        if (sr.bounds.max.y > topY) topY = sr.bounds.max.y;
                    }
                }

                // 머리 위보다 정확히 0.1칸 위에 체력바 배치 (중심점은 여전히 왼쪽부터 채워지도록 -0.4f 적용)
                hpBarBg.transform.position = new Vector3(transform.position.x - 0.4f, topY + 0.1f, 0f);
                hpBarBg.transform.localScale = new Vector3(invScale * 0.8f, invScale * 0.1f, 1f);

                // 체력 비율 반영 (연속적인 부드러운 감소)
                displayedHealth = Mathf.Lerp(displayedHealth, currentHealth, Time.deltaTime * 10f);
                float hpPercent = Mathf.Clamp01(displayedHealth / data.stats.health);
                hpBarFg.transform.localScale = new Vector3(hpPercent, 1f, 1f);

                // 남은 체력에 따른 색상 변화
                if (hpPercent > 0.5f) hpBarFgSr.color = Color.green;
                else if (hpPercent > 0.25f) hpBarFgSr.color = new Color(1f, 0.6f, 0f); // 주황색
                else hpBarFgSr.color = Color.red;

                // 소팅 오더 갱신 (가장 위에 그려지도록)
                int baseOrder = 1000 - Mathf.RoundToInt(transform.position.y * 100f) - Mathf.RoundToInt(transform.position.x * 10f);
                hpBarBgSr.sortingOrder = baseOrder + 50;
                hpBarFgSr.sortingOrder = baseOrder + 51;
            }

            statusEffects.UpdateEffectsPositionAndScale();
            UpdateSortingOrder();
        }

        private void UpdateSortingOrder()
        {
            int baseOrder = 1000 - Mathf.RoundToInt(transform.position.y * 100f) - Mathf.RoundToInt(transform.position.x * 10f);
            
            // 공중 유닛은 항상 최상단에 보이도록 오프셋 추가
            if (data != null && data.flyType == MonsterFlyType.Air)
            {
                baseOrder += 2000;
            }

            // 프리팹 내부에 있는 모든 SpriteRenderer를 찾아 레이어 오더를 갱신
            var allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in allRenderers)
            {
                if (sr.gameObject.name == "Shadow") continue;
                if (sr.gameObject.name == "HpBarBg") continue;
                if (sr.gameObject.name == "HpBarFg") continue;
                
                // FireEffect나 IceEffect 또는 그 자식 Visual의 SpriteRenderer인 경우 소팅오더 보정
                if (sr.gameObject.name == "FireEffect" || (sr.transform.parent != null && sr.transform.parent.gameObject.name == "FireEffect"))
                {
                    sr.sortingOrder = baseOrder + 5;
                    continue;
                }
                if (sr.gameObject.name == "IceEffect" || (sr.transform.parent != null && sr.transform.parent.gameObject.name == "IceEffect"))
                {
                    sr.sortingOrder = baseOrder + 5;
                    continue;
                }
                sr.sortingOrder = baseOrder;
            }

            if (shadowObj != null)
            {
                var shadowSr = shadowObj.GetComponent<SpriteRenderer>();
                if (shadowSr != null) shadowSr.sortingOrder = baseOrder - 1;
            }

            // 체력바도 캐릭터와 맞게 소팅오더 재조정
            if (hpBarBgSr != null) hpBarBgSr.sortingOrder = baseOrder + 10;
            if (hpBarFgSr != null) hpBarFgSr.sortingOrder = baseOrder + 11;
        }

        private void Move()
        {
            if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count) return;

            Vector3 basePosition = waypoints[waypoints.Count - 1];
            Vector3 targetPosition = waypoints[currentWaypointIndex];
            
            float actualSpeed = data.stats.moveSpeed * statusEffects.CurrentSpeedModifier;

            if (data != null && data.flyType == MonsterFlyType.Air)
            {
                // 그림자 기준 현재 위치 (현재 고도를 뺀 위치)
                Vector3 shadowPos = cachedTransform.position - (Vector3.up * currentAltitude);
                
                // 마지막 웨이포인트(기지)를 향해 가고 있을 때만 곤두박질(Swoop) 로직 적용
                bool isLastWaypoint = (currentWaypointIndex == waypoints.Count - 1);

                if (isLastWaypoint && Vector2.Distance(shadowPos, targetPosition) <= 1.0f)
                {
                    // === 곤두박질(Swoop) 모드 ===
                    Vector3 newShadowPos = Vector3.MoveTowards(shadowPos, targetPosition, actualSpeed * Time.deltaTime);
                    currentAltitude = Vector2.Distance(newShadowPos, targetPosition);
                    cachedTransform.position = newShadowPos + (Vector3.up * currentAltitude);

                    if (currentAltitude < 0.01f)
                    {
                        ReachBase();
                    }
                    return;
                }
                else
                {
                    // 평소 이동 (목표지점보다 1칸 위를 유지하며 비행)
                    currentAltitude = 1.0f;
                    Vector3 flyTarget = targetPosition + Vector3.up;
                    cachedTransform.position = Vector3.MoveTowards(cachedTransform.position, flyTarget, actualSpeed * Time.deltaTime);

                    // 목적지 도달 체크
                    if (Vector3.SqrMagnitude(cachedTransform.position - flyTarget) < 0.01f)
                    {
                        currentWaypointIndex++;
                        if (currentWaypointIndex >= waypoints.Count)
                        {
                            ReachBase();
                        }
                    }
                    return;
                }
            }

            // 일반 이동 로직
            cachedTransform.position = Vector3.MoveTowards(cachedTransform.position, targetPosition, actualSpeed * Time.deltaTime);

            // 목적지 도달 체크
            if (Vector3.SqrMagnitude(cachedTransform.position - targetPosition) < 0.01f)
            {
                if (currentWaypointIndex == waypoints.Count - 1 && data != null && data.flyType == MonsterFlyType.Air)
                {
                    ReachBase();
                    return;
                }
                
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Count)
                {
                    ReachBase();
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (isProcessed) return; // 이미 죽었거나 기지에 도착한 경우 데미지 무시

            currentHealth -= amount;
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void ReachBase()
        {
            if (isProcessed) return;
            isProcessed = true;

            if (data != null && data.stats != null)
            {
                Debug.Log($"[Base Hit] {data.monsterName} 기지 도착! 데미지: {data.stats.baseDamage}");
                GameManager.Instance.TakeDamage(data.stats.baseDamage);
            }
            ReturnToPool();
        }

        private void Die()
        {
            if (isProcessed) return;
            isProcessed = true;

            GameManager.Instance.AddGold(data.stats.killReward);
            GameManager.Instance.ReportMonsterKilled();

            // 분열 로직 처리
            if (data.splitLogic != null && data.splitLogic.splitOnDeath && data.splitLogic.splitMonsterType != null)
            {
                for (int i = 0; i < data.splitLogic.splitCount; i++)
                {
                    // 약간의 오프셋을 주어 스폰
                    Vector3 offset = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0);
                    GameObject splitObj = ObjectPoolManager.Instance.SpawnFromPool(
                        data.splitLogic.splitMonsterType.assets.prefab, 
                        cachedTransform.position + offset, 
                        Quaternion.identity);

                    if (splitObj != null)
                    {
                        var controller = splitObj.GetComponent<MonsterController>();
                        if (controller != null)
                        {
                            // 남은 경로를 그대로 물려줌
                            List<Vector2> remainingPath = new List<Vector2>();
                            for (int j = currentWaypointIndex; j < waypoints.Count; j++)
                            {
                                remainingPath.Add(waypoints[j]);
                            }
                            controller.Initialize(data.splitLogic.splitMonsterType, remainingPath);
                        }
                    }
                }
            }

            ReturnToPool();
        }

        public float GetRemainingPathDistance()
        {
            if (waypoints == null || currentWaypointIndex >= waypoints.Count) return 0f;

            float distance = Vector3.Distance(transform.position, waypoints[currentWaypointIndex]);
            for (int i = currentWaypointIndex; i < waypoints.Count - 1; i++)
            {
                distance += Vector3.Distance(waypoints[i], waypoints[i + 1]);
            }
            return distance;
        }

        public int GetCurrentWaypointIndex() => currentWaypointIndex;
        
        public float GetCurrentSpeed()
        {
            if (data == null || data.stats == null || statusEffects == null) return 1f;
            return data.stats.moveSpeed * statusEffects.CurrentSpeedModifier;
        }

        public float GetDistanceToCurrentWaypoint()
        {
            if (waypoints == null || currentWaypointIndex >= waypoints.Count) return 0f;
            return Vector3.Distance(transform.position, waypoints[currentWaypointIndex]);
        }

        public MonsterFlyType GetFlyType()
        {
            return data != null ? data.flyType : MonsterFlyType.Ground;
        }

        public bool IsImmuneTo(AttackAttribute attr)
        {
            if (data == null || data.stats == null) return false;
            if (attr == AttackAttribute.Normal) return false; // Normal attacks never trigger effects anyway
            return data.stats.immuneAttribute == attr;
        }

        private void ReturnToPool()
        {
            if (data != null && data.assets != null && data.assets.prefab != null)
            {
                ObjectPoolManager.Instance.ReturnToPool(this.gameObject, data.assets.prefab);
            }
            else
            {
                // 프리팹 참조가 끊어진 경우 폴백
                gameObject.SetActive(false);
            }
        }
    }
}
