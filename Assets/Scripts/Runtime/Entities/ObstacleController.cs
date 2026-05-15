using UnityEngine;
using TDF.Core.Data;
using TDF.Runtime.Managers;
using System.Collections.Generic;

namespace TDF.Runtime.Entities
{
    public class ObstacleController : MonoBehaviour
    {
        public static List<ObstacleController> ActiveObstacles = new List<ObstacleController>();

        private ObstacleData data;
        private float currentHealth;
        private Vector2Int gridPos;
        private bool isDestroyed = false;

        public float CurrentHealth => currentHealth;
        public Vector2Int GridPos => gridPos;

        public void Initialize(ObstacleData obstacleData, Vector2Int pos)
        {
            data = obstacleData;
            gridPos = pos;
            currentHealth = data.health;
            isDestroyed = false;

            if (!ActiveObstacles.Contains(this))
                ActiveObstacles.Add(this);

            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            
            sr.sprite = data.sprite;
            sr.sortingOrder = 15; // 몬스터와 비슷한 레이어
            
            // 스케일 설정
            if (data.sprite != null)
            {
                float maxBound = Mathf.Max(data.sprite.bounds.size.x, data.sprite.bounds.size.y);
                float scale = data.visualScale / maxBound;
                transform.localScale = Vector3.one * scale;
            }

            // 클릭을 위한 콜라이더 추가
            var col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;
        }

        private void OnDisable()
        {
            ActiveObstacles.Remove(this);
        }

        public void TakeDamage(float amount)
        {
            if (isDestroyed) return;

            currentHealth -= amount;
            
            // 피격 연출 (약간 붉게)
            StartCoroutine(HitEffect());

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private System.Collections.IEnumerator HitEffect()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                sr.color = Color.white;
            }
        }

        private void Die()
        {
            if (isDestroyed) return;
            isDestroyed = true;

            GameManager.Instance.AddGold(data.rewardGold);
            
            // 길막기 해제 및 경로 재계산 알림
            ActiveObstacles.Remove(this);
            
            // 전역 이벤트나 직접 호출을 통해 몬스터들에게 경로 재계산 요청
            NotifyPathBlockedChange();

            gameObject.SetActive(false);
        }

        private void NotifyPathBlockedChange()
        {
            // 모든 몬스터들에게 다음 타겟 지점을 다시 계산하도록 요청
            var monsters = MonsterController.ActiveMonsters;
            foreach (var monster in monsters)
            {
                if (monster != null && monster.gameObject.activeInHierarchy)
                {
                    // 몬스터의 현재 위치에서 다시 경로를 찾도록 함
                    monster.RecalculatePath();
                }
            }
        }
    }
}
