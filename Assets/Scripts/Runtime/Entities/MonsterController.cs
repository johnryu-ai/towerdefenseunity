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

        private float currentHealth;
        private List<Vector2> waypoints;
        private int currentWaypointIndex;

        public static List<MonsterController> ActiveMonsters = new List<MonsterController>();

        // 최적화를 위한 캐싱
        private Transform cachedTransform;

        private void OnEnable()
        {
            if (!ActiveMonsters.Contains(this)) ActiveMonsters.Add(this);
        }

        private void OnDisable()
        {
            ActiveMonsters.Remove(this);
        }

        private void Awake()
        {
            cachedTransform = transform;
            statusEffects = GetComponent<StatusEffectManager>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(MonsterData monsterData, List<Vector2> pathWaypoints)
        {
            data = monsterData;
            waypoints = pathWaypoints;
            currentWaypointIndex = 0;

            if (data != null && data.stats != null)
            {
                currentHealth = data.stats.health;
            }

            if (data != null && data.assets != null)
            {
                // 스케일 적용
                transform.localScale = Vector3.one * data.assets.visualScale;

                // 하단 정렬 (발 위치를 타일 하단에 맞춤)
                // 몬스터는 경로 이동 중이므로 초기 위치 설정 시 오프셋 반영
                transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f + (data.assets.visualScale * 0.5f), transform.position.z);

                if (data.assets.moveSprite != null)
                {
                    spriteRenderer.sprite = data.assets.moveSprite;
                    // 어둡게 보이는 문제 해결: 머티리얼 강제 설정
                    spriteRenderer.material = new Material(Shader.Find("Sprites/Default"));
                    spriteRenderer.color = Color.white;
                }
            }

            UpdateSortingOrder();

            statusEffects.ResetEffects();
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (statusEffects.IsStunned) return;

            Move();
        }

        private void LateUpdate()
        {
            // 애니메이터 덮어씌우기 방지 및 해상도가 큰 스프라이트의 정규화 처리
            if (data != null && data.assets != null)
            {
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    // 스프라이트의 가로/세로 중 가장 큰 원본 사이즈 추출
                    float maxBound = Mathf.Max(spriteRenderer.sprite.bounds.size.x, spriteRenderer.sprite.bounds.size.y);
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

            UpdateSortingOrder();
        }

        private void UpdateSortingOrder()
        {
            if (spriteRenderer != null)
            {
                // 기준값 1000에서 Y값이 작을수록(하단), X값이 작을수록(좌측) 큰 값을 가지도록 수식 적용
                spriteRenderer.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 100f) - Mathf.RoundToInt(transform.position.x * 10f);
            }
        }

        private void Move()
        {
            if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count) return;

            Vector3 targetPosition = waypoints[currentWaypointIndex];
            float actualSpeed = data.stats.moveSpeed * statusEffects.CurrentSpeedModifier;
            
            // 이동 로직 (간단한 Vector3.MoveTowards 사용)
            cachedTransform.position = Vector3.MoveTowards(cachedTransform.position, targetPosition, actualSpeed * Time.deltaTime);

            // 목적지 도달 체크
            if (Vector3.SqrMagnitude(cachedTransform.position - targetPosition) < 0.01f)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Count)
                {
                    ReachBase();
                }
            }
        }

        public void TakeDamage(float amount)
        {
            currentHealth -= amount;
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void ReachBase()
        {
            GameManager.Instance.TakeDamage(data.stats.baseDamage);
            ReturnToPool();
        }

        private void Die()
        {
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
