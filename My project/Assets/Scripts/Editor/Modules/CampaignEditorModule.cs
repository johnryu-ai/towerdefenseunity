using UnityEditor;
using UnityEngine;
using TDF.Core.Data;

namespace TDF.Editor.Modules
{
    public class CampaignEditorModule
    {
        private CampaignData targetCampaign;
        private Vector2 scrollPos;

        public void Draw()
        {
            GUILayout.BeginVertical("box");
            targetCampaign = (CampaignData)EditorGUILayout.ObjectField("Target Campaign Data", targetCampaign, typeof(CampaignData), false);

            if (targetCampaign == null)
            {
                EditorGUILayout.HelpBox("CampaignData 에셋을 위 슬롯에 드래그 앤 드롭 하세요.", MessageType.Warning);
                GUILayout.EndVertical();
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            DrawCampaignInfo();
            GUILayout.Space(15);
            DrawStageSequence();

            GUILayout.Space(30);

            GUIStyle playStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 14 };
            playStyle.normal.textColor = Color.green;

            if (GUILayout.Button("▶ Test Campaign (전체 과정 테스트)", playStyle, GUILayout.Height(40)))
            {
                StartCampaignTest();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetCampaign);
            }
        }

        private void DrawCampaignInfo()
        {
            GUILayout.Label("Campaign Info", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            
            string newName = EditorGUILayout.TextField("Campaign Name", targetCampaign.campaignName);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetCampaign, "Edit Campaign Info");
                targetCampaign.campaignName = newName;
            }
        }

        private void DrawStageSequence()
        {
            GUILayout.Label("Stage Sequence", EditorStyles.boldLabel);

            if (targetCampaign.stages == null) targetCampaign.stages = new System.Collections.Generic.List<StageData>();

            for (int i = 0; i < targetCampaign.stages.Count; i++)
            {
                GUILayout.BeginHorizontal("box");
                GUILayout.Label($"Stage {i + 1}", GUILayout.Width(60));
                
                EditorGUI.BeginChangeCheck();
                StageData stage = (StageData)EditorGUILayout.ObjectField(targetCampaign.stages[i], typeof(StageData), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetCampaign, "Assign Stage");
                    targetCampaign.stages[i] = stage;
                }

                if (GUILayout.Button("Up", EditorStyles.miniButtonLeft, GUILayout.Width(35)) && i > 0)
                {
                    Undo.RecordObject(targetCampaign, "Move Stage Up");
                    var temp = targetCampaign.stages[i - 1];
                    targetCampaign.stages[i - 1] = targetCampaign.stages[i];
                    targetCampaign.stages[i] = temp;
                }
                
                if (GUILayout.Button("Down", EditorStyles.miniButtonMid, GUILayout.Width(45)) && i < targetCampaign.stages.Count - 1)
                {
                    Undo.RecordObject(targetCampaign, "Move Stage Down");
                    var temp = targetCampaign.stages[i + 1];
                    targetCampaign.stages[i + 1] = targetCampaign.stages[i];
                    targetCampaign.stages[i] = temp;
                }

                if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.Width(30)))
                {
                    Undo.RecordObject(targetCampaign, "Remove Stage");
                    targetCampaign.stages.RemoveAt(i);
                    break;
                }
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Stage to Sequence"))
            {
                Undo.RecordObject(targetCampaign, "Add Stage");
                targetCampaign.stages.Add(null);
            }
        }

        private void StartCampaignTest()
        {
            if (targetCampaign.stages == null || targetCampaign.stages.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "테스트할 스테이지가 하나도 없습니다.", "OK");
                return;
            }

            // GameManager에 static하게 전달
            Runtime.Managers.GameManager.staticTestCampaign = targetCampaign;
            Runtime.Managers.GameManager.staticTestStageIndex = 0;

            // 플레이 모드 진입
            EditorApplication.isPlaying = true;
        }
    }
}
