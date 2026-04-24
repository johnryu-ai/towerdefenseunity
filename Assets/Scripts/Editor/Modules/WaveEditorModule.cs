using UnityEditor;
using UnityEngine;
using TDF.Core.Data;

namespace TDF.Editor.Modules
{
    public class WaveEditorModule
    {
        private WaveData targetWave;
        private Vector2 scrollPos;
        private int expandedRoundIndex = -1;

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

            DrawTopLevelInfo();
            GUILayout.Space(15);
            DrawRounds();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetWave);
            }
        }

        private void DrawTopLevelInfo()
        {
            GUILayout.Label("General Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            
            MapData newLinkedMap = (MapData)EditorGUILayout.ObjectField("Linked Map", targetWave.linkedMapData, typeof(MapData), false);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetWave, "Edit Wave Top Level Info");
                targetWave.linkedMapData = newLinkedMap;
            }
        }

        private void DrawRounds()
        {
            GUILayout.Label("Rounds (Waves) Sequence", EditorStyles.boldLabel);
            
            if (targetWave.rounds == null) targetWave.rounds = new System.Collections.Generic.List<WaveRound>();

            for (int i = 0; i < targetWave.rounds.Count; i++)
            {
                GUILayout.BeginVertical("box");
                
                GUILayout.BeginHorizontal();
                string title = $"Round {targetWave.rounds[i].roundNumber} (Reward: {targetWave.rounds[i].clearReward})";
                if (GUILayout.Button(expandedRoundIndex == i ? "▼ " + title : "▶ " + title, EditorStyles.boldLabel))
                {
                    if (expandedRoundIndex == i) expandedRoundIndex = -1;
                    else expandedRoundIndex = i;
                }

                if (GUILayout.Button("▲", GUILayout.Width(25)) && i > 0)
                {
                    Undo.RecordObject(targetWave, "Move Round Up");
                    var temp = targetWave.rounds[i];
                    targetWave.rounds[i] = targetWave.rounds[i - 1];
                    targetWave.rounds[i - 1] = temp;
                    expandedRoundIndex = -1;
                    break;
                }
                if (GUILayout.Button("▼", GUILayout.Width(25)) && i < targetWave.rounds.Count - 1)
                {
                    Undo.RecordObject(targetWave, "Move Round Down");
                    var temp = targetWave.rounds[i];
                    targetWave.rounds[i] = targetWave.rounds[i + 1];
                    targetWave.rounds[i + 1] = temp;
                    expandedRoundIndex = -1;
                    break;
                }
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(targetWave, "Remove Round");
                    targetWave.rounds.RemoveAt(i);
                    expandedRoundIndex = -1;
                    break;
                }
                GUILayout.EndHorizontal();

                if (expandedRoundIndex == i)
                {
                    DrawRoundDetails(targetWave.rounds[i]);
                }

                GUILayout.EndVertical();
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Add New Round", GUILayout.Height(30)))
            {
                Undo.RecordObject(targetWave, "Add Round");
                int newRoundNum = targetWave.rounds.Count > 0 ? targetWave.rounds[targetWave.rounds.Count - 1].roundNumber + 1 : 1;
                targetWave.rounds.Add(new WaveRound { roundNumber = newRoundNum, nextRoundDelay = 5f });
                expandedRoundIndex = targetWave.rounds.Count - 1;
            }
        }

        private void DrawRoundDetails(WaveRound round)
        {
            GUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            
            GUILayout.BeginHorizontal();
            int newRoundNum = EditorGUILayout.IntField("Round Number", round.roundNumber);
            int newReward = EditorGUILayout.IntField("Clear Reward", round.clearReward);
            GUILayout.EndHorizontal();
            
            float newDelay = EditorGUILayout.FloatField("Next Round Delay (s)", round.nextRoundDelay);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetWave, "Edit Round Info");
                round.roundNumber = newRoundNum;
                round.clearReward = newReward;
                round.nextRoundDelay = newDelay;
            }

            GUILayout.Space(10);
            GUILayout.Label("Spawn Sequence", EditorStyles.boldLabel);

            if (round.spawnSequence == null) round.spawnSequence = new System.Collections.Generic.List<WaveSpawnInfo>();

            for (int j = 0; j < round.spawnSequence.Count; j++)
            {
                GUILayout.BeginVertical("helpbox");
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Spawn Event #{j}", EditorStyles.miniBoldLabel, GUILayout.Width(120));
                
                if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(25)) && j > 0)
                {
                    Undo.RecordObject(targetWave, "Move Spawn Up");
                    var temp = round.spawnSequence[j];
                    round.spawnSequence[j] = round.spawnSequence[j - 1];
                    round.spawnSequence[j - 1] = temp;
                    break;
                }
                if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(25)) && j < round.spawnSequence.Count - 1)
                {
                    Undo.RecordObject(targetWave, "Move Spawn Down");
                    var temp = round.spawnSequence[j];
                    round.spawnSequence[j] = round.spawnSequence[j + 1];
                    round.spawnSequence[j + 1] = temp;
                    break;
                }
                if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.Width(25)))
                {
                    Undo.RecordObject(targetWave, "Remove Spawn");
                    round.spawnSequence.RemoveAt(j);
                    break;
                }
                GUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                var seq = round.spawnSequence[j];

                seq.monsterToSpawn = (MonsterData)EditorGUILayout.ObjectField("Monster", seq.monsterToSpawn, typeof(MonsterData), false);
                
                GUILayout.BeginHorizontal();
                seq.spawnPointIndex = EditorGUILayout.IntField("Spawn ID", seq.spawnPointIndex);
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

            if (GUILayout.Button("Add Spawn Event"))
            {
                Undo.RecordObject(targetWave, "Add Spawn Event");
                round.spawnSequence.Add(new WaveSpawnInfo { spawnCount = 1, spawnInterval = 1f });
            }
        }
    }
}
