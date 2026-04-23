using UnityEngine;
using TDF.Runtime.Managers;

namespace TDF.Runtime.UI
{
    public class InGameUIManager : MonoBehaviour
    {
        private float currentSpeed = 1f;

        private void OnGUI()
        {
            if (GameManager.Instance == null || WaveManager.Instance == null) return;

            // 상단 바 배경 박스 그리기
            GUILayout.BeginArea(new Rect(0, 0, Screen.width, 50), GUI.skin.box);
            GUILayout.BeginHorizontal();

            // 스타일 설정
            GUIStyle textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 20;
            textStyle.alignment = TextAnchor.MiddleLeft;
            textStyle.normal.textColor = Color.white;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 16;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            // 1. 현재 골드 표시
            GUILayout.Label($"💰 Gold: {GameManager.Instance.CurrentGold}", textStyle, GUILayout.Width(150), GUILayout.Height(40));

            // 2. 기지 생명력 표시
            GUILayout.Label($"❤️ HP: {GameManager.Instance.CurrentHP}", textStyle, GUILayout.Width(130), GUILayout.Height(40));

            // 3. 웨이브 상황 표시 (예: 1 / 10)
            int currentWave = WaveManager.Instance.CurrentWaveIndex + 1;
            int totalWaves = WaveManager.Instance.TotalWaves;
            if (totalWaves == 0) totalWaves = 1; 
            if (currentWave > totalWaves) currentWave = totalWaves; 

            GUILayout.Label($"⚔️ Wave: {currentWave} / {totalWaves}", textStyle, GUILayout.Width(160), GUILayout.Height(40));

            // 빈 공간 채우기 (버튼들을 우측으로 밀어냄)
            GUILayout.FlexibleSpace();

            // 4. 다음 웨이브 부르기 버튼
            if (GUILayout.Button("Next Wave", buttonStyle, GUILayout.Width(120), GUILayout.Height(40)))
            {
                WaveManager.Instance.StartNextWave();
            }

            GUILayout.Space(10);

            // 5. 속도 조절 버튼 (1x, 2x, 3x)
            if (GUILayout.Button($"Speed: {currentSpeed}x", buttonStyle, GUILayout.Width(100), GUILayout.Height(40)))
            {
                currentSpeed += 1f;
                if (currentSpeed > 3f) currentSpeed = 1f;
                Time.timeScale = currentSpeed;
            }
            
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            
            // 게임 상태 오버레이 (일시정지 또는 게임오버 시)
            if (GameManager.Instance.CurrentState == GameState.GameOver)
            {
                GUIStyle centerStyle = new GUIStyle(GUI.skin.label);
                centerStyle.fontSize = 50;
                centerStyle.alignment = TextAnchor.MiddleCenter;
                centerStyle.normal.textColor = Color.red;
                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "GAME OVER", centerStyle);
            }
        }
    }
}
