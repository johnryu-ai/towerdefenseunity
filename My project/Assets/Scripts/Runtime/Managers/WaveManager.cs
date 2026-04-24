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
        private bool isVictoryTriggered = false;
        
        public int CurrentWaveIndex { get; private set; } = 0;
        public int TotalWaves => currentStageData != null && currentStageData.waveData != null && currentStageData.waveData.rounds != null ? currentStageData.waveData.rounds.Count : (testWaveData != null && testWaveData.rounds != null ? testWaveData.rounds.Count : 0);

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
                if (testWaveData != null && testWaveData.rounds != null && testWaveData.rounds.Count > 0) StartWave(testWaveData.rounds[0]);
                else Debug.LogWarning("WaveManager: 할당된 Test Wave Data가 없거나 웨이브 라운드가 없습니다!");
            }

            // 모든 웨이브 스폰 완료 & 승리 처리 안됨 & 게임 중일 때 모든 몬스터 처치 확인
            if (!isVictoryTriggered && CurrentWaveIndex > 0 && CurrentWaveIndex >= TotalWaves && !isWaveRunning)
            {
                if (GameManager.Instance.CurrentState == GameState.Playing)
                {
                    if (Entities.MonsterController.ActiveMonsters.Count == 0)
                    {
                        isVictoryTriggered = true;
                        GameManager.Instance.ChangeState(GameState.Victory);
                    }
                }
            }
        }

        public void StartNextWave()
        {
            if (isWaveRunning) return;

            if (currentStageData != null && currentStageData.waveData != null && currentStageData.waveData.rounds != null && CurrentWaveIndex < currentStageData.waveData.rounds.Count)
            {
                StartWave(currentStageData.waveData.rounds[CurrentWaveIndex]);
            }
            else if (testWaveData != null && testWaveData.rounds != null && CurrentWaveIndex < testWaveData.rounds.Count)
            {
                StartWave(testWaveData.rounds[CurrentWaveIndex]);
            }
            else
            {
                Debug.Log("모든 웨이브가 완료되었습니다!");
            }
        }

        public void StartWave(WaveRound wave)
        {
            if (isWaveRunning) return;
            
            // 웨이브가 시작되면 게임 상태를 Playing으로 변경하여 몬스터와 타워가 움직이도록 함
            if (GameManager.Instance.CurrentState != GameState.Playing)
            {
                GameManager.Instance.ChangeState(GameState.Playing);
            }

            isWaveRunning = true;
            OnRoundStarted?.Invoke(wave.roundNumber);
            StartCoroutine(SpawnSequenceCoroutine(wave));
        }

        private IEnumerator SpawnSequenceCoroutine(WaveRound wave)
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
                        else
                        {
                            Debug.LogError($"[치명적 오류] 소환된 몬스터 프리팹 '{monsterObj.name}'에 'MonsterController' 스크립트가 안 붙어 있습니다! 스크립트가 없으면 몬스터는 절대 움직이지 않습니다.");
                        }
                    }

                    if (i < spawnInfo.spawnCount - 1)
                        yield return new WaitForSeconds(spawnInfo.spawnInterval);
                }
            }

            // 모든 스폰 완료
            WaveCleared(wave);

            // 다음 라운드가 있다면 자동 시작 타이머 실행
            if (CurrentWaveIndex < TotalWaves)
            {
                StartCoroutine(AutoStartNextWaveCoroutine(wave.nextRoundDelay));
            }
        }

        private IEnumerator AutoStartNextWaveCoroutine(float delay)
        {
            float timer = delay;
            while (timer > 0)
            {
                // 사용자가 수동으로 'Next Wave'를 눌러 이미 웨이브가 시작되었다면 자동 타이머 중단
                if (isWaveRunning) yield break; 

                timer -= Time.deltaTime;
                yield return null;
            }

            if (!isWaveRunning)
            {
                StartNextWave();
            }
        }

        private void WaveCleared(WaveRound wave)
        {
            isWaveRunning = false;
            GameManager.Instance.AddGold(wave.clearReward);
            CurrentWaveIndex++;
            OnRoundCleared?.Invoke(wave.roundNumber);

            // 마지막 적이 파괴되었을 때 Update()에서 승리 처리됨
        }
    }
}
