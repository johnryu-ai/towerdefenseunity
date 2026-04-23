using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using TDF.Core.Data;

namespace TDF.Editor.Modules
{
    public class StageEditorModule
    {
        private StageData targetStage;
        private Vector2 scrollPos;

        public void Draw()
        {
            GUILayout.BeginVertical("box");
            targetStage = (StageData)EditorGUILayout.ObjectField("Target Stage Data", targetStage, typeof(StageData), false);

            if (targetStage == null)
            {
                EditorGUILayout.HelpBox("StageData 에셋을 위 슬롯에 드래그 앤 드롭 하세요.", MessageType.Warning);
                GUILayout.EndVertical();
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            DrawSequenceInfo();
            GUILayout.Space(15);
            DrawNarrativeList("Entry Story (스테이지 진입 전)", targetStage.entryStory);
            GUILayout.Space(15);
            DrawNarrativeList("Clear Story (스테이지 클리어 후)", targetStage.clearStory);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetStage);
            }
        }

        private void DrawSequenceInfo()
        {
            GUILayout.Label("Sequence Info", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            
            int newIdx = EditorGUILayout.IntField("Stage Index", targetStage.stageIndex);
            MapData newMap = (MapData)EditorGUILayout.ObjectField("Linked Map", targetStage.linkedMapData, typeof(MapData), false);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetStage, "Edit Stage Sequence Info");
                targetStage.stageIndex = newIdx;
                targetStage.linkedMapData = newMap;
            }
        }

        private void DrawNarrativeList(string label, System.Collections.Generic.List<StoryMoment> storyList)
        {
            GUILayout.Label(label, EditorStyles.boldLabel);
            
            for (int i = 0; i < storyList.Count; i++)
            {
                GUILayout.BeginVertical("box");
                
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Scene #{i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    Undo.RecordObject(targetStage, "Remove Story Moment");
                    storyList.RemoveAt(i);
                    break;
                }
                GUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                var moment = storyList[i];

                moment.backgroundImage = (Sprite)EditorGUILayout.ObjectField("Background Image", moment.backgroundImage, typeof(Sprite), false);
                moment.videoClip = (VideoClip)EditorGUILayout.ObjectField("Video Clip (Optional)", moment.videoClip, typeof(VideoClip), false);
                
                GUILayout.Label("Dialogue Text:");
                moment.storyText = EditorGUILayout.TextArea(moment.storyText, GUILayout.Height(50));

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetStage, "Edit Story Moment");
                }
                
                GUILayout.EndVertical();
            }

            if (GUILayout.Button($"Add Scene to {label}"))
            {
                Undo.RecordObject(targetStage, "Add Story Moment");
                storyList.Add(new StoryMoment());
            }
        }
    }
}
