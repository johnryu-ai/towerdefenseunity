using UnityEditor;
using UnityEngine;
using TDF.Core.Data;

namespace TDF.Editor.Modules
{
    public class WaveEditorModule
    {
        private WaveData targetWave;
        private Vector2 scrollPos;

        public void Draw()
        {
            GUILayout.BeginVertical("box");
            targetWave = (WaveData)EditorGUILayout.ObjectField("Target Wave Data", targetWave, typeof(WaveData), false);

            if (targetWave == null)
            {
                EditorGUILayout.HelpBox("WaveData 에셋을 위 슬롯에 드래그 앤 드롭 하세요.", MessageType.Warning);
                GUILayout.EndVertical();
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            DrawRoundInfo();
            GUILayout.Space(15);
            DrawSpawnSequence();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetWave);
            }
        }

        private void DrawRoundInfo()
        {
            GUILayout.Label("Round Information", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            
            int newRoundNum = EditorGUILayout.IntField("Round Number", targetWave.roundNumber);
            MapData newLinkedMap = (MapData)EditorGUILayout.ObjectField("Linked Map", targetWave.linkedMapData, typeof(MapData), false);
            int newReward = EditorGUILayout.IntField("Clear Reward (Gold)", targetWave.clearReward);
            float newDelay = EditorGUILayout.FloatField("Next Round Delay", targetWave.nextRoundDelay);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetWave, "Edit Wave Round Info");
                targetWave.roundNumber = newRoundNum;
                targetWave.linkedMapData = newLinkedMap;
                targetWave.clearReward = newReward;
                targetWave.nextRoundDelay = newDelay;
            }
        }

        private void DrawSpawnSequence()
        {
            GUILayout.Label("Spawn Sequence Timeline", EditorStyles.boldLabel);
            
            for (int i = 0; i < targetWave.spawnSequence.Count; i++)
            {
                GUILayout.BeginVertical("box");
                
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Spawn Event #{i}", EditorStyles.boldLabel, GUILayout.Width(150));
                
                // 순서 변경 버튼
                if (GUILayout.Button("▲", GUILayout.Width(30)) && i > 0)
                {
                    Undo.RecordObject(targetWave, "Move Spawn Event Up");
                    var temp = targetWave.spawnSequence[i];
                    targetWave.spawnSequence[i] = targetWave.spawnSequence[i - 1];
                    targetWave.spawnSequence[i - 1] = temp;
                    break;
                }
                if (GUILayout.Button("▼", GUILayout.Width(30)) && i < targetWave.spawnSequence.Count - 1)
                {
                    Undo.RecordObject(targetWave, "Move Spawn Event Down");
                    var temp = targetWave.spawnSequence[i];
                    targetWave.spawnSequence[i] = targetWave.spawnSequence[i + 1];
                    targetWave.spawnSequence[i + 1] = temp;
                    break;
                }

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    Undo.RecordObject(targetWave, "Remove Spawn Event");
                    targetWave.spawnSequence.RemoveAt(i);
                    break;
                }
                GUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                var seq = targetWave.spawnSequence[i];

                seq.monsterToSpawn = (MonsterData)EditorGUILayout.ObjectField("Monster", seq.monsterToSpawn, typeof(MonsterData), false);
                
                GUILayout.BeginHorizontal();
                seq.spawnPointIndex = EditorGUILayout.IntField("Spawn Point ID", seq.spawnPointIndex);
                seq.spawnCount = EditorGUILayout.IntField("Count", seq.spawnCount);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                seq.startDelay = EditorGUILayout.FloatField("Start Delay (s)", seq.startDelay);
                seq.spawnInterval = EditorGUILayout.FloatField("Interval (s)", seq.spawnInterval);
                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetWave, "Edit Spawn Event");
                }
                
                GUILayout.EndVertical();
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Add Spawn Event"))
            {
                Undo.RecordObject(targetWave, "Add Spawn Event");
                targetWave.spawnSequence.Add(new WaveSpawnInfo { spawnCount = 1, spawnInterval = 1f });
            }
        }
    }
}
