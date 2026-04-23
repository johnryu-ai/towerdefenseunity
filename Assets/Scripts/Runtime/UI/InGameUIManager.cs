using UnityEngine;
using UnityEngine.UI;

namespace TDF.Runtime.UI
{
    public class InGameUIManager : MonoBehaviour
    {
        [Header("UI Text References")]
        public Text hpText;
        public Text goldText;
        public Text roundText;
        public Text statusText;

        [Header("UI Buttons")]
        public Button speedUpButton;
        public Button pauseButton;

        private float currentSpeed = 1f;

        private void Start()
        {
            // 이벤트 구독 (GameManager가 먼저 초기화되었다고 가정하거나, 콜백으로 처리)
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnHPChanged += UpdateHP;
                Managers.GameManager.Instance.OnGoldChanged += UpdateGold;
                Managers.GameManager.Instance.OnGameStateChanged += UpdateGameState;
                
                // 초기값 강제 갱신
                UpdateHP(Managers.GameManager.Instance.CurrentHP);
                UpdateGold(Managers.GameManager.Instance.CurrentGold);
            }

            if (Managers.WaveManager.Instance != null)
            {
                Managers.WaveManager.Instance.OnRoundStarted += UpdateRound;
            }

            if (speedUpButton != null) speedUpButton.onClick.AddListener(ToggleSpeed);
            if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);
        }

        private void OnDestroy()
        {
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnHPChanged -= UpdateHP;
                Managers.GameManager.Instance.OnGoldChanged -= UpdateGold;
                Managers.GameManager.Instance.OnGameStateChanged -= UpdateGameState;
            }
            
            if (Managers.WaveManager.Instance != null)
            {
                Managers.WaveManager.Instance.OnRoundStarted -= UpdateRound;
            }
        }

        private void UpdateHP(int hp)
        {
            if (hpText != null) hpText.text = $"HP: {hp}";
        }

        private void UpdateGold(int gold)
        {
            if (goldText != null) goldText.text = $"Gold: {gold}";
        }

        private void UpdateRound(int round)
        {
            if (roundText != null) roundText.text = $"Round: {round}";
        }

        private void UpdateGameState(Managers.GameState state)
        {
            if (statusText != null)
            {
                statusText.text = state.ToString();
                statusText.gameObject.SetActive(state != Managers.GameState.Playing);
            }
        }

        private void ToggleSpeed()
        {
            if (Managers.GameManager.Instance.CurrentState != Managers.GameState.Playing) return;

            currentSpeed = (currentSpeed == 1f) ? 2f : 1f;
            Time.timeScale = currentSpeed;
            
            // Text 갱신 (예: Button 자식 컴포넌트)
            var btnText = speedUpButton.GetComponentInChildren<Text>();
            if (btnText != null) btnText.text = $"x{currentSpeed}";
        }

        private void TogglePause()
        {
            var state = Managers.GameManager.Instance.CurrentState;
            if (state == Managers.GameState.Playing)
            {
                Managers.GameManager.Instance.ChangeState(Managers.GameState.Paused);
            }
            else if (state == Managers.GameState.Paused)
            {
                Managers.GameManager.Instance.ChangeState(Managers.GameState.Playing);
            }
        }
    }
}
