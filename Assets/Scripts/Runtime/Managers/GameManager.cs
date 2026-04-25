using System;
using UnityEngine;
using TDF.Core.Data;

namespace TDF.Runtime.Managers
{
    public enum GameState { Story, Ready, Playing, Paused, GameOver, Victory, ClearStory }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Ready;

        public int CurrentGold { get; private set; }
        public int CurrentHP { get; private set; }

        public event Action<int> OnGoldChanged;
        public event Action<int> OnHPChanged;
        public event Action<GameState> OnGameStateChanged;

        [Header("Initial Setup")]
        public CampaignData currentCampaign;
        public int currentStageIndex = 0;
        public StageData currentStageData;
        public MapData currentMapData; // 에디터/씬에서 임시 할당 또는 StageManager를 통해 주입됨

        [Header("Scene Names")]
        [Tooltip("메인메뉴(로비) 씬 이름. Build Settings에 등록된 씬 이름과 일치해야 합니다.")]
        public string mainMenuSceneName = "LobbyScene";

        public static CampaignData staticTestCampaign;
        public static int staticTestStageIndex = 0;
        
        public float stageStartTime { get; private set; }
        public float stageClearTime { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                
                // 테스터에서 넘겨준 static 데이터가 있으면 우선 적용
                if (staticTestCampaign != null)
                {
                    currentCampaign = staticTestCampaign;
                    currentStageIndex = staticTestStageIndex;
                }
                
                // 싱글톤 유지가 필요한 경우 아래 주석 해제 (단일 씬 구조라면 불필요)
                // DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            if (currentCampaign != null && currentCampaign.stages != null && currentStageIndex < currentCampaign.stages.Count)
            {
                currentStageData = currentCampaign.stages[currentStageIndex];
                
                // WaveManager에도 해당 스테이지 데이터 전달 (현재는 인스펙터 참조 방식이 혼용되어 있으므로 강제 할당)
                if (WaveManager.Instance != null)
                {
                    WaveManager.Instance.currentStageData = currentStageData;
                }
            }

            if (currentStageData != null && currentStageData.linkedMapData != null)
            {
                currentMapData = currentStageData.linkedMapData;
            }

            if (currentMapData != null && TDF.Runtime.Map.MapController.Instance != null)
            {
                currentMapData.ResetRuntimeState();
                TDF.Runtime.Map.MapController.Instance.GenerateMap(currentMapData);
            }

            if (currentMapData != null)
            {
                CurrentGold = currentMapData.config.initialGold;
                CurrentHP = currentMapData.config.initialLives;
                
                OnGoldChanged?.Invoke(CurrentGold);
                OnHPChanged?.Invoke(CurrentHP);

                // ── UserDataManager 연동: 스테이지 기본 제공 타워를 'default'로 자동 언락 ──
                if (UserDataManager.Instance != null && currentMapData.config.availableTowers != null)
                {
                    string stageSourceId = currentStageData != null ? currentStageData.name : "unknown";
                    foreach (var towerData in currentMapData.config.availableTowers)
                    {
                        if (towerData != null && !string.IsNullOrEmpty(towerData.towerId))
                        {
                            UserDataManager.Instance.UnlockTower(
                                towerData.towerId,
                                TDF.Core.Data.TowerUnlockSource.Default,
                                stageSourceId
                            );
                        }
                    }
                }
            }

            if (currentStageData != null && currentStageData.entryStory != null && currentStageData.entryStory.Count > 0)
            {
                ChangeState(GameState.Story);
            }
            else
            {
                FinishStory();
            }
        }

        public void FinishStory()
        {
            stageStartTime = Time.time;

            // ── UserDataManager 연동: 플레이 세션 스냅샷 저장 ──
            if (UserDataManager.Instance != null && currentMapData != null)
            {
                UserDataManager.Instance.SavePlaySession(
                    campaignName: currentCampaign != null ? currentCampaign.campaignName : "standalone",
                    stageIndex:   currentStageIndex,
                    gold:         CurrentGold,
                    hp:           CurrentHP,
                    waveIndex:    0
                );
            }

            ChangeState(GameState.Ready);
        }

        public void GoToNextStage()
        {
            if (currentCampaign != null && currentStageIndex + 1 < currentCampaign.stages.Count)
            {
                staticTestCampaign = currentCampaign;
                staticTestStageIndex = currentStageIndex + 1;
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
            else
            {
                Debug.Log("캠페인이 완료되었습니다!");
                staticTestCampaign = null;
                staticTestStageIndex = 0;
                
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
            }
        }

        /// <summary>정적 캠페인 상태를 초기화하고 메인메뉴 씬으로 이동한다.</summary>
        public void GoToMainMenu()
        {
            // 정적 캠페인 진행 상태 초기화
            staticTestCampaign = null;
            staticTestStageIndex = 0;

            // timeScale 복구 (Victory/Paused 상태에서 0이 될 수 있음)
            Time.timeScale = 1f;

            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnGameStateChanged?.Invoke(CurrentState);

            if (CurrentState == GameState.Paused)
            {
                Time.timeScale = 0f;
            }
            else if (CurrentState == GameState.Playing)
            {
                Time.timeScale = 1f;
            }
            else if (CurrentState == GameState.Victory)
            {
                stageClearTime = Time.time;
                Time.timeScale = 0f;

                // ── UserDataManager 연동: 클리어 결과 기록 + 세션 삭제 ──
                if (UserDataManager.Instance != null && currentStageData != null)
                {
                    int clearTimeSec = Mathf.FloorToInt(stageClearTime - stageStartTime);
                    UserDataManager.Instance.RecordStageClear(
                        stageId:          currentStageData.name,
                        stageName:        currentStageData.name,
                        livesRemaining:   CurrentHP,
                        goldRemaining:    CurrentGold,
                        clearTimeSeconds: Mathf.Max(0, clearTimeSec)
                    );
                    UserDataManager.Instance.ClearPlaySession();
                }
            }
            else if (CurrentState == GameState.GameOver)
            {
                // ── UserDataManager 연동: 게임오버 시 세션 삭제 ──
                if (UserDataManager.Instance != null)
                {
                    UserDataManager.Instance.ClearPlaySession();
                }
            }
        }

        public void AddGold(int amount)
        {
            if (amount == 0) return;
            CurrentGold += amount;
            OnGoldChanged?.Invoke(CurrentGold);
        }

        public bool UseGold(int amount)
        {
            if (CurrentGold >= amount)
            {
                CurrentGold -= amount;
                OnGoldChanged?.Invoke(CurrentGold);
                return true;
            }
            return false;
        }

        public void TakeDamage(int damage)
        {
            if (CurrentState != GameState.Playing) return;

            CurrentHP -= damage;
            if (CurrentHP < 0) CurrentHP = 0;
            OnHPChanged?.Invoke(CurrentHP);

            if (CurrentHP == 0)
            {
                ChangeState(GameState.GameOver);
            }
        }
    }
}
