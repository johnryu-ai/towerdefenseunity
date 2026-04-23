using System;
using UnityEngine;
using TDF.Core.Data;

namespace TDF.Runtime.Managers
{
    public enum GameState { Ready, Playing, Paused, GameOver, Victory }

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
        public MapData currentMapData; // 에디터/씬에서 임시 할당 또는 StageManager를 통해 주입됨

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
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
            if (currentMapData != null)
            {
                CurrentGold = currentMapData.config.initialGold;
                CurrentHP = currentMapData.config.initialLives;
                
                OnGoldChanged?.Invoke(CurrentGold);
                OnHPChanged?.Invoke(CurrentHP);
            }
            ChangeState(GameState.Playing);
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnGameStateChanged?.Invoke(CurrentState);

            if (CurrentState == GameState.Paused) Time.timeScale = 0f;
            else if (CurrentState == GameState.Playing) Time.timeScale = 1f;
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
