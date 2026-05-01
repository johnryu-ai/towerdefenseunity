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

        private float currentHealth;
        private List<Vector2> waypoints;
        private int currentWaypointIndex;
        private bool isProcessed = false; // 중복 처리 방지 플래그

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
            isProcessed = false; // 스폰 시 플래그 초기화

            if (data != null && data.stats != null)
            {
                currentHealth = data.stats.health;
            }

            if (data != null && data.assets != null)
            {
                if (data.assets.moveSprite != null)
                {
                    spriteRenderer.sprite = data.assets.moveSprite;
                }
                else if (data.assets.moveAnim != null)
                {
                    data.assets.moveAnim.SampleAnimation(gameObject, 0f);
                }

                // 어둡게 보이는 문제 해결: 머티리얼 강제 설정
                spriteRenderer.material = new Material(Shader.Find("Sprites/Default"));
                spriteRenderer.color = Color.white;

                // 하단 정렬 (발 위치를 타일 하단에 맞춤)
                // 몬스터는 경로 이동 중이므로 초기 위치 설정 시 오프셋 반영
                transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f + (data.assets.visualScale * 0.5f), transform.position.z);

                // 애니메이터 설정
                if (data.assets.animatorController != null)
                {
                    if (animator == null) animator = gameObject.AddComponent<Animator>();
                    animator.runtimeAnimatorController = data.assets.animatorController;
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

            if (data != null && data.assets != null && data.assets.animatorController == null && data.assets.moveAnim != null)
            {
                PlayClip(data.assets.moveAnim);
            }
        }

        private void LateUpdate()
        {
            if (data != null && data.assets != null)
            {
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    float maxBound = Mathf.Max(spriteRenderer.sprite.bounds.size.x, spriteRenderer.sprite.bounds.size.y);
                    if (maxBound > 0.001f)
                    {
                        float currentScale = data.assets.visualScale / maxBound;
                        transform.localScale = Vector3.one * currentScale;
                    }
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
                Debug.Log($"[Base Hit] {data.monsterName} (ID:{gameObject.GetInstanceID()}) 기지 도착! 데미지: {data.stats.baseDamage}");
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
