using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        private List<WaveData> stageWaves = new List<WaveData>(); // 임시: StageData가 WaveData 리스트를 갖도록 구조 확장이 필요하거나, 별도로 로드

        private int currentWaveIndex = 0;
        private bool isWaveRunning = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
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

                // MapData에서 스폰 좌표 획득
                Vector3 spawnPos = Vector3.zero; // 기본값
                if (GameManager.Instance.currentMapData != null)
                {
                    var spawnPointList = GameManager.Instance.currentMapData.spawnPoints;
                    var targetSpawn = spawnPointList.Find(sp => sp.spawnIndex == spawnInfo.spawnPointIndex);
                    if (targetSpawn != null)
                    {
                        // MapController를 통해 실제 월드 좌표 획득하는 로직 연동
                        // 임시: spawnPos = MapController.Instance.GetWorldPosition(targetSpawn.coordinate.x, targetSpawn.coordinate.y);
                    }
                }

                for (int i = 0; i < spawnInfo.spawnCount; i++)
                {
                    // 오브젝트 풀을 통한 몬스터 생성
                    GameObject monsterObj = ObjectPoolManager.Instance.SpawnFromPool(
                        spawnInfo.monsterToSpawn.assets.prefab, 
                        spawnPos, 
                        Quaternion.identity
                    );

                    // TODO: monsterObj.GetComponent<MonsterController>().Initialize(spawnInfo.monsterToSpawn);

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
