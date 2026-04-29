using System;
using System.Collections.Generic;
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
        public string mainMenuSceneName = "Lobby_Main";

        [Header("Achievements")]
        public List<AchievementData> allAchievements = new List<AchievementData>();

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
#if UNITY_EDITOR
            if (allAchievements == null || allAchievements.Count == 0)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AchievementData");
                allAchievements = new List<AchievementData>();
                foreach (var g in guids) 
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                    allAchievements.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<AchievementData>(path));
                }
            }
#endif
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
                Debug.Log("캠페인이 완료되었습니다! 메인 메뉴로 돌아갑니다.");
                GoToMainMenu();
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
                    
                    // 메타 재화(로비 상점용 골드 및 젬) 지급
                    int score = (CurrentGold * 10) + (CurrentHP * 50) + Mathf.Max(0, 10000 - (clearTimeSec * 20));
                    int rewardGold = score / 10;
                    int rewardGems = 5;
                    UserDataManager.Instance.AddCurrency(gold: rewardGold, gems: rewardGems);
                    
                    UserDataManager.Instance.ClearPlaySession();

                    // ── 업적 달성 검증 ──
                    if (allAchievements != null)
                    {
                        foreach (var ach in allAchievements)
                        {
                            if (ach == null) continue;
                            
                            UserDataManager.Instance.RegisterAchievement(ach.achievementId, ach.achievementName);
                            var prog = UserDataManager.Instance.GetAchievementProgress(ach.achievementId);
                            if (prog != null && prog.completed) continue;

                            switch (ach.conditionType)
                            {
                                case AchievementConditionType.TotalStagesCleared:
                                    UserDataManager.Instance.IncrementAchievement(ach.achievementId, ach.targetValue, 1);
                                    break;
                                case AchievementConditionType.TotalGoldEarned:
                                    // 누적 골드 획득량 달성
                                    UserDataManager.Instance.IncrementAchievement(ach.achievementId, ach.targetValue, rewardGold);
                                    break;
                                case AchievementConditionType.SpecificStageClear:
                                    // 특정 스테이지(이름) 클리어 시
                                    if (currentStageData.name == ach.targetStringData)
                                    {
                                        UserDataManager.Instance.UpdateAchievementProgress(ach.achievementId, ach.targetValue, ach.targetValue);
                                    }
                                    break;
                            }
                        }
                    }
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

        public void ReportMonsterKilled()
        {
            if (UserDataManager.Instance == null || allAchievements == null) return;

            foreach (var ach in allAchievements)
            {
                if (ach == null) continue;

                if (ach.conditionType == AchievementConditionType.TotalMonstersKilled)
                {
                    UserDataManager.Instance.RegisterAchievement(ach.achievementId, ach.achievementName);
                    UserDataManager.Instance.IncrementAchievement(ach.achievementId, ach.targetValue, 1);
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
