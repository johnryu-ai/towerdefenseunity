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
            int displayWave = WaveManager.Instance.CurrentWaveIndex;
            if (!WaveManager.Instance.HasStartedFirstWave) displayWave = 1;
            int totalWaves = WaveManager.Instance.TotalWaves;
            if (totalWaves == 0) totalWaves = 1; 
            if (displayWave > totalWaves) displayWave = totalWaves; 

            GUILayout.Label($"⚔️ Wave: {displayWave} / {totalWaves}", textStyle, GUILayout.Width(350), GUILayout.Height(100));

            // 빈 공간 채우기 (버튼들을 우측으로 밀어냄)
            GUILayout.FlexibleSpace();

            // 4. 다음 웨이브 부르기 버튼
            string waveBtnText = WaveManager.Instance.HasStartedFirstWave ? "WAVE" : "START";
            if (GUILayout.Button(waveBtnText, buttonStyle, GUILayout.Width(250), GUILayout.Height(90)))
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

            // 게임 상태 오버레이 (일시정지 또는 게임오버, 스토리, 클리어 시)
            GameState state = GameManager.Instance.CurrentState;
            
            if (state == GameState.GameOver)
            {
                GUIStyle centerStyle = new GUIStyle(GUI.skin.label) { fontSize = 100, alignment = TextAnchor.MiddleCenter };
                centerStyle.normal.textColor = Color.red;
                GUI.Label(new Rect(0, 0, 1920, 1080), "GAME OVER", centerStyle);
            }
            else if (state == GameState.Story)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(0, 0, 0, 0.85f);
                GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
                GUI.color = oldColor;

                string storyText = "StageData가 할당되지 않았거나 스토리가 없습니다.";
                if (GameManager.Instance.currentStageData != null && GameManager.Instance.currentStageData.entryStory.Count > 0)
                {
                    storyText = GameManager.Instance.currentStageData.entryStory[0].storyText;
                    if (string.IsNullOrEmpty(storyText)) storyText = "(텍스트가 입력되지 않았습니다. StageData 인스펙터를 확인하세요)";
                }

                GUILayout.BeginArea(new Rect(460, 240, 1000, 600), GUI.skin.box);
                GUILayout.Space(50);
                
                GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 60, alignment = TextAnchor.MiddleCenter };
                titleStyle.normal.textColor = Color.cyan;
                GUILayout.Label("Story", titleStyle);
                
                GUILayout.Space(50);
                
                GUIStyle storyStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                storyStyle.normal.textColor = Color.white;
                GUILayout.Label(storyText, storyStyle, GUILayout.Height(300));
                
                GUILayout.FlexibleSpace();
                
                GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 40 };
                if (GUILayout.Button("전투 시작", btnStyle, GUILayout.Height(80)))
                {
                    GameManager.Instance.FinishStory();
                }
                GUILayout.Space(30);
                GUILayout.EndArea();
            }
            else if (state == GameState.Victory)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(0, 0, 0, 0.85f);
                GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
                GUI.color = oldColor;

                int timeSec = Mathf.FloorToInt(GameManager.Instance.stageClearTime - GameManager.Instance.stageStartTime);
                if (timeSec < 0) timeSec = 0;
                int score = (GameManager.Instance.CurrentGold * 10) + (GameManager.Instance.CurrentHP * 50) + Mathf.Max(0, 10000 - (timeSec * 20));

                GUILayout.BeginArea(new Rect(560, 190, 800, 700), GUI.skin.box);
                GUILayout.Space(40);
                
                GUIStyle clearStyle = new GUIStyle(GUI.skin.label) { fontSize = 70, alignment = TextAnchor.MiddleCenter };
                clearStyle.normal.textColor = Color.yellow;
                GUILayout.Label("스테이지 클리어!", clearStyle);
                GUILayout.Space(50);

                GUIStyle infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleLeft };
                infoStyle.normal.textColor = Color.cyan;
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(100);
                GUILayout.BeginVertical();
                GUILayout.Label($"❤️ 남은 체력: {GameManager.Instance.CurrentHP} (+{GameManager.Instance.CurrentHP * 50}점)", infoStyle);
                GUILayout.Space(20);
                GUILayout.Label($"💰 남은 골드: {GameManager.Instance.CurrentGold} (+{GameManager.Instance.CurrentGold * 10}점)", infoStyle);
                GUILayout.Space(20);
                GUILayout.Label($"⏱️ 클리어 타임: {timeSec}초", infoStyle);
                GUILayout.Space(40);
                
                GUIStyle scoreStyle = new GUIStyle(GUI.skin.label) { fontSize = 60, alignment = TextAnchor.MiddleCenter };
                scoreStyle.normal.textColor = Color.yellow;
                GUILayout.Label($"🏆 총 점수: {score:N0} 점", scoreStyle);
                
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                
                GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 40 };
                if (GUILayout.Button("다음", btnStyle, GUILayout.Height(80)))
                {
                    GameManager.Instance.ChangeState(GameState.ClearStory);
                }
                GUILayout.Space(40);
                GUILayout.EndArea();
            }
            else if (state == GameState.ClearStory)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(0, 0, 0, 0.85f);
                GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
                GUI.color = oldColor;

                string clearStoryText = "(클리어 스토리가 없습니다. StageData를 확인하세요)";
                if (GameManager.Instance.currentStageData != null && GameManager.Instance.currentStageData.clearStory.Count > 0)
                {
                    clearStoryText = GameManager.Instance.currentStageData.clearStory[0].storyText;
                    if (string.IsNullOrEmpty(clearStoryText)) clearStoryText = "(클리어 스토리 텍스트가 비어 있습니다)";
                }

                GUILayout.BeginArea(new Rect(460, 240, 1000, 600), GUI.skin.box);
                GUILayout.Space(50);
                
                GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 60, alignment = TextAnchor.MiddleCenter };
                titleStyle.normal.textColor = Color.yellow;
                GUILayout.Label("Clear Story", titleStyle);
                
                GUILayout.Space(50);
                
                GUIStyle storyStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                storyStyle.normal.textColor = Color.white;
                GUILayout.Label(clearStoryText, storyStyle, GUILayout.Height(300));
                
                GUILayout.FlexibleSpace();
                
                GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 40 };
                
                string nextBtnText = "다음 스테이지";
                if (GameManager.Instance.currentCampaign == null || GameManager.Instance.currentStageIndex + 1 >= GameManager.Instance.currentCampaign.stages.Count)
                {
                    nextBtnText = "캠페인 완료";
                }

                if (GUILayout.Button(nextBtnText, btnStyle, GUILayout.Height(80)))
                {
                    GameManager.Instance.GoToNextStage();
                }
                GUILayout.Space(30);
                GUILayout.EndArea();
            }

            // 매트릭스 복구
            GUI.matrix = oldMatrix;
        }
    }
}
