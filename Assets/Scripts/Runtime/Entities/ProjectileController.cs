using UnityEngine;
using TDF.Core.Data;
using TDF.Runtime.Managers;

namespace TDF.Runtime.Entities
{
    public class ProjectileController : MonoBehaviour
    {
        public float speed = 10f;
        
        private MonsterController target;
        private float damage;
        private AttackAttribute attribute;
        private GameObject hitEffectPrefab;
        private float targetScale = 1f;

        private Transform cachedTransform;
        
        private Animator animator;
        private float animTime = 0f;
        private AnimationClip currentPlayingClip;

        private void Awake()
        {
            cachedTransform = transform;
            animator = GetComponent<Animator>();
        }

        public void Initialize(MonsterController targetMonster, float dmg, AttackAttribute attr, GameObject hitEffect, Sprite projSprite = null, AnimationClip projAnim = null, float projScale = 1f)
        {
            target = targetMonster;
            damage = dmg;
            attribute = attr;
            hitEffectPrefab = hitEffect;
            targetScale = projScale;
            
            currentPlayingClip = projAnim;
            animTime = 0f;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // 애니메이션만 있을 경우를 대비해 머티리얼 강제 세팅
                sr.material = new Material(Shader.Find("Sprites/Default"));
                sr.color = Color.white;
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

            // 초기 방향 설정 (생성 직후 0도 방향으로 튀는 현상 방지)
            if (target != null)
            {
                Vector3 dir = (target.transform.position - transform.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void Update()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                // 타겟이 이미 죽었거나 사라지면 풀로 반환
                ReturnToPool();
                return;
            }

            // 애니메이션 재생
            if (currentPlayingClip != null)
            {
                animTime += Time.deltaTime;
                if (currentPlayingClip.isLooping) animTime %= currentPlayingClip.length;
                else animTime = Mathf.Clamp(animTime, 0f, currentPlayingClip.length);
                currentPlayingClip.SampleAnimation(gameObject, animTime);
            }

            // 타겟을 향해 이동
            Vector3 dir = (target.transform.position - cachedTransform.position).normalized;
            cachedTransform.position += dir * speed * Time.deltaTime;

            // 회전 처리 (옵션)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            cachedTransform.rotation = Quaternion.Euler(0, 0, angle);

            // 충돌 체크 (거리가 매우 가까워지면 명중으로 간주)
            if (Vector2.SqrMagnitude(cachedTransform.position - target.transform.position) < 0.1f)
            {
                HitTarget();
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

        private void HitTarget()
        {
            // 데미지 및 상태이상 적용
            target.TakeDamage(damage);
            
            var status = target.GetComponent<StatusEffectManager>();
            if (status != null)
            {
                switch (attribute)
                {
                    case AttackAttribute.Cold:
                        status.ApplySlow(2f, 0.4f); // 2초간 40% 감속 (데이터화 가능)
                        break;
                    case AttackAttribute.Fire:
                        status.ApplyBurn(3f, damage * 0.2f); // 3초간 초당 데미지
                        break;
                    case AttackAttribute.Electric:
                        status.ApplyStun(0.5f); // 0.5초 스턴
                        break;
                }
            }

            // 이펙트 스폰
            if (hitEffectPrefab != null)
            {
                ObjectPoolManager.Instance.SpawnFromPool(hitEffectPrefab, cachedTransform.position, Quaternion.identity);
            }

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            // 여기서 투사체 자체의 원본 프리팹 참조를 알아야 정확히 ReturnToPool이 가능합니다.
            // 임시 방편으로 비활성화만 시키거나, 생성 시 넘겨받은 원본 참조를 보관해야 합니다.
            gameObject.SetActive(false);
        }
    }
}
