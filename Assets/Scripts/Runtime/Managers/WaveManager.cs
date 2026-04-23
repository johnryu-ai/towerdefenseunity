using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TDF.Core.Data;

namespace TDF.Runtime.Managers
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        public event System.Action<int> OnRoundStarted;
        public event System.Action<int> OnRoundCleared;

        [Header("Current Stage Data")]
        public StageData currentStageData;
        
        [Header("Test Mode")]
        public WaveData testWaveData; // 테스트용 단일 웨이브 데이터

        private bool isWaveRunning = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            // 테스트용 웨이브 시작 단축키 (Space)
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (testWaveData != null) StartWave(testWaveData);
                else Debug.LogWarning("WaveManager: 할당된 Test Wave Data가 없습니다!");
            }
        }

        public void StartWave(WaveData wave)
        {
            if (isWaveRunning) return;
            isWaveRunning = true;
            OnRoundStarted?.Invoke(wave.roundNumber);
            StartCoroutine(SpawnSequenceCoroutine(wave));
        }

        private IEnumerator SpawnSequenceCoroutine(WaveData wave)
        {
            // 각 스폰 이벤트 순차 처리 (단순화: 딜레이 후 스폰)
            // 실제 기획에 따라 여러 스폰 포인트에서 동시에 타이머가 돌도록 병렬 코루틴 처리로 고도화 가능
            
            foreach (var spawnInfo in wave.spawnSequence)
            {
                if (spawnInfo.monsterToSpawn == null || spawnInfo.monsterToSpawn.assets.prefab == null) continue;

                yield return new WaitForSeconds(spawnInfo.startDelay);

                // WaveData에 직접 설정한 경로가 있으면 우선 사용, 없으면 MapController 자동 탐색 사용
                List<Vector2> path = (spawnInfo.customWaypoints != null && spawnInfo.customWaypoints.Count > 0) 
                    ? spawnInfo.customWaypoints 
                    : Map.MapController.Instance.GetPath();

                if (path == null || path.Count == 0)
                {
                    Debug.LogWarning("WaveManager: 몬스터 이동 경로를 찾을 수 없습니다! 맵에 Spawn/Base 타일이 없거나 경로가 끊겼는지 확인하세요.");
                }

                Vector3 spawnPos = (path != null && path.Count > 0) ? (Vector3)path[0] : Vector3.zero;

                for (int i = 0; i < spawnInfo.spawnCount; i++)
                {
                    GameObject monsterObj = ObjectPoolManager.Instance.SpawnFromPool(
                        spawnInfo.monsterToSpawn.assets.prefab, 
                        spawnPos, 
                        Quaternion.identity
                    );

                    if (monsterObj != null)
                    {
                        var controller = monsterObj.GetComponent<Entities.MonsterController>();
                        if (controller != null)
                        {
                            controller.Initialize(spawnInfo.monsterToSpawn, path);
                        }
                    }

                    if (i < spawnInfo.spawnCount - 1)
                        yield return new WaitForSeconds(spawnInfo.spawnInterval);
                }
            }

            // 모든 스폰 완료 대기
            // TODO: 필드에 남은 몬스터가 0마리가 될 때까지 감지하는 로직 추가

            WaveCleared(wave);
        }

        private void WaveCleared(WaveData wave)
        {
            isWaveRunning = false;
            GameManager.Instance.AddGold(wave.clearReward);
            OnRoundCleared?.Invoke(wave.roundNumber);

            // 다음 라운드 자동 시작 로직이 있다면 nextRoundDelay 이후 호출
        }
    }
}
