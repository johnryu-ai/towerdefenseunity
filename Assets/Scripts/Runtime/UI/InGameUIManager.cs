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

            // 1920x1080 해상도 고정 대응을 위한 매트릭스 스케일링
            Vector2 ratio = new Vector2(Screen.width / 1920f, Screen.height / 1080f);
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(ratio.x, ratio.y, 1f));

            // 상단 바 배경 박스 그리기 (1920 x 135)
            GUILayout.BeginArea(new Rect(0, 0, 1920, 135), GUI.skin.box);
            GUILayout.BeginHorizontal();

            // 스타일 설정
            GUIStyle textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 40;
            textStyle.alignment = TextAnchor.MiddleLeft;
            textStyle.normal.textColor = Color.white;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 35;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            // 수직 가운데 정렬을 위한 여백 설정
            GUILayout.Space(20);

            // 1. 현재 골드 표시
            GUILayout.Label($"💰 Gold: {GameManager.Instance.CurrentGold}", textStyle, GUILayout.Width(350), GUILayout.Height(100));

            // 2. 기지 생명력 표시
            GUILayout.Label($"❤️ HP: {GameManager.Instance.CurrentHP}", textStyle, GUILayout.Width(250), GUILayout.Height(100));

            // 3. 웨이브 상황 표시 (예: 1 / 10)
            int currentWave = WaveManager.Instance.CurrentWaveIndex + 1;
            int totalWaves = WaveManager.Instance.TotalWaves;
            if (totalWaves == 0) totalWaves = 1; 
            if (currentWave > totalWaves) currentWave = totalWaves; 

            GUILayout.Label($"⚔️ Wave: {currentWave} / {totalWaves}", textStyle, GUILayout.Width(350), GUILayout.Height(100));

            // 빈 공간 채우기 (버튼들을 우측으로 밀어냄)
            GUILayout.FlexibleSpace();

            // 4. 다음 웨이브 부르기 버튼
            if (GUILayout.Button("Next Wave", buttonStyle, GUILayout.Width(250), GUILayout.Height(90)))
            {
                WaveManager.Instance.StartNextWave();
            }

            GUILayout.Space(20);

            // 5. 속도 조절 버튼 (1x, 2x, 3x)
            if (GUILayout.Button($"Speed: {currentSpeed}x", buttonStyle, GUILayout.Width(200), GUILayout.Height(90)))
            {
                currentSpeed += 1f;
                if (currentSpeed > 3f) currentSpeed = 1f;
                Time.timeScale = currentSpeed;
            }
            
            GUILayout.Space(20);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 게임 상태 오버레이 (일시정지 또는 게임오버 시)
            if (GameManager.Instance.CurrentState == GameState.GameOver)
            {
                GUIStyle centerStyle = new GUIStyle(GUI.skin.label);
                centerStyle.fontSize = 100;
                centerStyle.alignment = TextAnchor.MiddleCenter;
                centerStyle.normal.textColor = Color.red;
                GUI.Label(new Rect(0, 0, 1920, 1080), "GAME OVER", centerStyle);
            }

            // 매트릭스 복구
            GUI.matrix = oldMatrix;
        }
    }
}
