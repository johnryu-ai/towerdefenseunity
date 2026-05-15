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
        }

        private void Update()
        {
            if (stunTimer > 0f) stunTimer -= Time.deltaTime;
            
            if (slowTimer > 0f) slowTimer -= Time.deltaTime;

            if (burnTimer > 0f)
            {
                burnTimer -= Time.deltaTime;
                if (monster != null) monster.TakeDamage(burnDamagePerSecond * Time.deltaTime);
            }
        }

        public void ApplyStun(float duration)
        {
            if (duration > stunTimer) stunTimer = duration;
        }

        public void ApplySlow(float duration, float amount)
        {
            if (duration > slowTimer) slowTimer = duration;
            if (amount > slowAmount) slowAmount = amount;
        }

        public void ApplyBurn(float duration, float dps)
        {
            if (duration > burnTimer) burnTimer = duration;
            if (dps > burnDamagePerSecond) burnDamagePerSecond = dps;
        }
    }
}
