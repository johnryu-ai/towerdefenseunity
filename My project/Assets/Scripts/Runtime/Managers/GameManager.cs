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
                TDF.Runtime.Map.MapController.Instance.GenerateMap(currentMapData);
            }

            if (currentMapData != null)
            {
                CurrentGold = currentMapData.config.initialGold;
                CurrentHP = currentMapData.config.initialLives;
                
                OnGoldChanged?.Invoke(CurrentGold);
                OnHPChanged?.Invoke(CurrentHP);
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

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnGameStateChanged?.Invoke(CurrentState);

            if (CurrentState == GameState.Paused) Time.timeScale = 0f;
            else if (CurrentState == GameState.Playing) Time.timeScale = 1f;
            else if (CurrentState == GameState.Victory) 
            {
                stageClearTime = Time.time;
                Time.timeScale = 0f;
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
