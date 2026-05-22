using System.Collections.Generic;
using UnityEngine;
using TDF.Core.Data;

namespace TDF.Runtime.Entities
{
    public class StatusEffectManager : MonoBehaviour
    {
        private MonsterController monster;

        // 상태이상 타이머
        private float stunTimer = 0f;
        private float slowTimer = 0f;
        private float burnTimer = 0f;
        
        private float slowAmount = 0f; // 0 ~ 1 (예: 0.5면 50% 감속)
        private float burnDamagePerSecond = 0f;

        private GameObject fireEffectObj;
        private SpriteRenderer fireEffectSr;
        private float fireAnimTime = 0f;

        private GameObject iceEffectObj;
        private SpriteRenderer iceEffectSr;
        private float iceAnimTime = 0f;

        public bool IsStunned => stunTimer > 0f;
        public float CurrentSpeedModifier => (slowTimer > 0f) ? (1f - slowAmount) : 1f;

        private void Awake()
        {
            monster = GetComponent<MonsterController>();
        }

        public void ResetEffects()
        {
            stunTimer = 0f;
            slowTimer = 0f;
            burnTimer = 0f;
            slowAmount = 0f;
            burnDamagePerSecond = 0f;
            DisableFireEffect();
            DisableIceEffect();
        }

        private void Update()
        {
            if (stunTimer > 0f) stunTimer -= Time.deltaTime;
            
            if (slowTimer > 0f)
            {
                slowTimer -= Time.deltaTime;
                UpdateIceEffectAnimation();
            }
            else
            {
                DisableIceEffect();
            }

            if (burnTimer > 0f)
            {
                burnTimer -= Time.deltaTime;
                if (monster != null) monster.TakeDamage(burnDamagePerSecond * Time.deltaTime);
                UpdateFireEffectAnimation();
            }
            else
            {
                DisableFireEffect();
            }
        }

        private void UpdateFireEffectAnimation()
        {
            var settings = AttackAttributeSettings.Instance;
            if (settings == null)
            {
                Debug.LogWarning("[StatusEffect] UpdateFireEffectAnimation: settings is null!");
                DisableFireEffect();
                return;
            }
#if UNITY_EDITOR
            if ((settings.fireSprites == null || settings.fireSprites.Length == 0) && settings.fireEffectAnimation != null)
            {
                settings.ExtractSprites();
            }
#endif
            if (settings.fireEffectAnimation == null)
            {
                Debug.LogWarning("[StatusEffect] UpdateFireEffectAnimation: fireEffectAnimation is null!");
                DisableFireEffect();
                return;
            }

            if (fireEffectObj == null)
            {
                Debug.Log($"[StatusEffect] Creating FireEffect object for {gameObject.name}");
                // 부모 오브젝트 (오프셋 및 절대 스케일 유지용)
                fireEffectObj = new GameObject("FireEffect");
                fireEffectObj.transform.SetParent(this.transform);

                // 자식 오브젝트 (애니메이션 샘플링 타겟용)
                GameObject visualObj = new GameObject("Visual");
                visualObj.transform.SetParent(fireEffectObj.transform);
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localScale = Vector3.one;
                visualObj.transform.localRotation = Quaternion.identity;

                fireEffectSr = visualObj.AddComponent<SpriteRenderer>();
                if (monster != null && monster.GetComponent<SpriteRenderer>() != null)
                {
                    fireEffectSr.material = monster.GetComponent<SpriteRenderer>().material;
                }
                fireAnimTime = 0f;
            }

            fireEffectObj.SetActive(true);
            
            // 애니메이션 재생 (직접 스프라이트 할당)
            fireAnimTime += Time.deltaTime;
            if (settings.fireSprites != null && settings.fireSprites.Length > 0)
            {
                float duration = settings.fireDurationCalculated > 0.001f ? settings.fireDurationCalculated : 1f;
                float progress = fireAnimTime % duration;
                
                int frameCount = settings.fireSprites.Length;
                int frameIndex = Mathf.Clamp(Mathf.FloorToInt((progress / duration) * frameCount), 0, frameCount - 1);
                
                fireEffectSr.sprite = settings.fireSprites[frameIndex];
            }
            else
            {
                // Fallback: 스프라이트가 캐싱되지 않았다면 기존 SampleAnimation 시도
                AnimationClip clip = settings.fireEffectAnimation;
                if (clip != null)
                {
                    clip.legacy = true;
                    if (clip.isLooping) fireAnimTime %= clip.length;
                    else fireAnimTime = Mathf.Clamp(fireAnimTime, 0f, clip.length);
                    clip.SampleAnimation(fireEffectSr.gameObject, fireAnimTime);
                }
            }
        }

        private void DisableFireEffect()
        {
            if (fireEffectObj != null && fireEffectObj.activeSelf)
            {
                fireEffectObj.SetActive(false);
            }
        }

        private void UpdateIceEffectAnimation()
        {
            var settings = AttackAttributeSettings.Instance;
            if (settings == null)
            {
                Debug.LogWarning("[StatusEffect] UpdateIceEffectAnimation: settings is null!");
                DisableIceEffect();
                return;
            }
#if UNITY_EDITOR
            if ((settings.iceSprites == null || settings.iceSprites.Length == 0) && settings.iceEffectAnimation != null)
            {
                settings.ExtractSprites();
            }
#endif
            if (settings.iceEffectAnimation == null)
            {
                Debug.LogWarning("[StatusEffect] UpdateIceEffectAnimation: iceEffectAnimation is null!");
                DisableIceEffect();
                return;
            }

            if (iceEffectObj == null)
            {
                Debug.Log($"[StatusEffect] Creating IceEffect object for {gameObject.name}");
                // 부모 오브젝트 (오프셋 및 절대 스케일 유지용)
                iceEffectObj = new GameObject("IceEffect");
                iceEffectObj.transform.SetParent(this.transform);

                // 자식 오브젝트 (애니메이션 샘플링 타겟용)
                GameObject visualObj = new GameObject("Visual");
                visualObj.transform.SetParent(iceEffectObj.transform);
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localScale = Vector3.one;
                visualObj.transform.localRotation = Quaternion.identity;

                iceEffectSr = visualObj.AddComponent<SpriteRenderer>();
                if (monster != null && monster.GetComponent<SpriteRenderer>() != null)
                {
                    iceEffectSr.material = monster.GetComponent<SpriteRenderer>().material;
                }
                iceAnimTime = 0f;
            }

            iceEffectObj.SetActive(true);
            
            // 애니메이션 재생 (직접 스프라이트 할당)
            iceAnimTime += Time.deltaTime;
            if (settings.iceSprites != null && settings.iceSprites.Length > 0)
            {
                float duration = settings.iceDurationCalculated > 0.001f ? settings.iceDurationCalculated : 1f;
                float progress = iceAnimTime % duration;
                
                int frameCount = settings.iceSprites.Length;
                int frameIndex = Mathf.Clamp(Mathf.FloorToInt((progress / duration) * frameCount), 0, frameCount - 1);
                
                iceEffectSr.sprite = settings.iceSprites[frameIndex];
            }
            else
            {
                // Fallback: 스프라이트가 캐싱되지 않았다면 기존 SampleAnimation 시도
                AnimationClip clip = settings.iceEffectAnimation;
                if (clip != null)
                {
                    clip.legacy = true;
                    if (clip.isLooping) iceAnimTime %= clip.length;
                    else iceAnimTime = Mathf.Clamp(iceAnimTime, 0f, clip.length);
                    clip.SampleAnimation(iceEffectSr.gameObject, iceAnimTime);
                }
            }
        }

        private void DisableIceEffect()
        {
            if (iceEffectObj != null && iceEffectObj.activeSelf)
            {
                iceEffectObj.SetActive(false);
            }
        }

        public void UpdateEffectsPositionAndScale()
        {
            var settings = AttackAttributeSettings.Instance;
            if (settings == null) return;

            // Fire Effect 위치 및 스케일 업데이트
            if (fireEffectObj != null && fireEffectObj.activeSelf)
            {
                // 오프셋 계산 (절대값이 2보다 크면 픽셀 단위로 오해하여 입력한 것으로 보고 100으로 나눔)
                Vector3 offset = settings.fireEffectOffset;
                if (Mathf.Abs(offset.x) > 2f) offset.x /= 100f;
                if (Mathf.Abs(offset.y) > 2f) offset.y /= 100f;
                if (Mathf.Abs(offset.z) > 2f) offset.z /= 100f;

                fireEffectObj.transform.position = transform.position + offset;

                // 스프라이트 바운드 크기로 정규화하여 scale 1이 격자 1칸 크기(TILE_SIZE = 1.0f)가 되게 보정
                float spriteBoundX = 1f;
                float spriteBoundY = 1f;
                if (fireEffectSr != null && fireEffectSr.sprite != null)
                {
                    spriteBoundX = fireEffectSr.sprite.bounds.size.x;
                    spriteBoundY = fireEffectSr.sprite.bounds.size.y;
                    if (spriteBoundX < 0.001f) spriteBoundX = 1f;
                    if (spriteBoundY < 0.001f) spriteBoundY = 1f;
                }

                // 부모의 로컬 스케일에 반비례하게 적용하여 월드 크기를 항상 settings.fireEffectScale가 되도록 유지
                float parentScaleX = transform.lossyScale.x;
                float parentScaleY = transform.lossyScale.y;
                float parentScaleZ = transform.lossyScale.z;

                Vector3 targetWorldScale = new Vector3(
                    settings.fireEffectScale.x / spriteBoundX,
                    settings.fireEffectScale.y / spriteBoundY,
                    settings.fireEffectScale.z
                );

                fireEffectObj.transform.localScale = new Vector3(
                    parentScaleX > 0.001f ? targetWorldScale.x / parentScaleX : targetWorldScale.x,
                    parentScaleY > 0.001f ? targetWorldScale.y / parentScaleY : targetWorldScale.y,
                    parentScaleZ > 0.001f ? targetWorldScale.z / parentScaleZ : targetWorldScale.z
                );
            }

            // Ice Effect 위치 및 스케일 업데이트
            if (iceEffectObj != null && iceEffectObj.activeSelf)
            {
                // 오프셋 계산 (절대값이 2보다 크면 픽셀 단위로 오해하여 입력한 것으로 보고 100으로 나눔)
                Vector3 offset = settings.iceEffectOffset;
                if (Mathf.Abs(offset.x) > 2f) offset.x /= 100f;
                if (Mathf.Abs(offset.y) > 2f) offset.y /= 100f;
                if (Mathf.Abs(offset.z) > 2f) offset.z /= 100f;

                iceEffectObj.transform.position = transform.position + offset;

                // 스프라이트 바운드 크기로 정규화하여 scale 1이 격자 1칸 크기(TILE_SIZE = 1.0f)가 되게 보정
                float spriteBoundX = 1f;
                float spriteBoundY = 1f;
                if (iceEffectSr != null && iceEffectSr.sprite != null)
                {
                    spriteBoundX = iceEffectSr.sprite.bounds.size.x;
                    spriteBoundY = iceEffectSr.sprite.bounds.size.y;
                    if (spriteBoundX < 0.001f) spriteBoundX = 1f;
                    if (spriteBoundY < 0.001f) spriteBoundY = 1f;
                }

                // 부모의 로컬 스케일에 반비례하게 적용하여 월드 크기를 항상 settings.iceEffectScale가 되도록 유지
                float parentScaleX = transform.lossyScale.x;
                float parentScaleY = transform.lossyScale.y;
                float parentScaleZ = transform.lossyScale.z;

                Vector3 targetWorldScale = new Vector3(
                    settings.iceEffectScale.x / spriteBoundX,
                    settings.iceEffectScale.y / spriteBoundY,
                    settings.iceEffectScale.z
                );

                iceEffectObj.transform.localScale = new Vector3(
                    parentScaleX > 0.001f ? targetWorldScale.x / parentScaleX : targetWorldScale.x,
                    parentScaleY > 0.001f ? targetWorldScale.y / parentScaleY : targetWorldScale.y,
                    parentScaleZ > 0.001f ? targetWorldScale.z / parentScaleZ : targetWorldScale.z
                );
            }
        }

        public void ApplyStun(float duration)
        {
            if (duration > stunTimer) stunTimer = duration;
        }

        public void ApplySlow(float duration, float amount)
        {
            Debug.Log($"[StatusEffect] ApplySlow({duration}, {amount}) on {gameObject.name}. Current slowTimer: {slowTimer}");
            if (duration > slowTimer) slowTimer = duration;
            if (amount > slowAmount) slowAmount = amount;
        }

        public void ApplyBurn(float duration, float dps)
        {
            Debug.Log($"[StatusEffect] ApplyBurn({duration}, {dps}) on {gameObject.name}. Current burnTimer: {burnTimer}");
            if (duration > burnTimer) burnTimer = duration;
            if (dps > burnDamagePerSecond) burnDamagePerSecond = dps;
        }
    }
}
