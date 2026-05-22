using UnityEditor;
using UnityEngine;
using TDF.Core.Data;
using System.IO;

namespace TDF.Editor.Modules
{
    public class ScoreEditorModule
    {
        private ScoreCalculationSettings settings;
        private const string SETTINGS_PATH = "Assets/Resources/Settings/ScoreCalculationSettings.asset";

        public void Draw()
        {
            if (settings == null)
            {
                LoadSettings();
            }

            if (settings == null)
            {
                EditorGUILayout.HelpBox("Score Calculation Settings asset not found. Click the button below to create it.", MessageType.Warning);
                if (GUILayout.Button("Create Score Settings Asset", GUILayout.Height(30)))
                {
                    CreateSettings();
                }
                return;
            }

            EditorGUILayout.LabelField("Score Calculation & Reward Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            // 1. Score Formula Parameters
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Score Formula Parameters", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            settings.goldWeight = EditorGUILayout.IntSlider("Gold Weight", settings.goldWeight, 0, 100);
            EditorGUILayout.HelpBox("Multiplier applied to remaining gold. (Score += Gold * Weight)", MessageType.None);
            EditorGUILayout.Space(5);

            settings.hpWeight = EditorGUILayout.IntSlider("HP (Lives) Weight", settings.hpWeight, 0, 500);
            EditorGUILayout.HelpBox("Multiplier applied to remaining HP/Lives. (Score += HP * Weight)", MessageType.None);
            EditorGUILayout.Space(5);

            settings.timeBonusBase = EditorGUILayout.IntField("Time Bonus Base", settings.timeBonusBase);
            settings.timeBonusDecay = EditorGUILayout.IntField("Time Bonus Decay Rate", settings.timeBonusDecay);
            EditorGUILayout.HelpBox("Time Bonus calculation: Max(0, Base - (ClearTimeInSeconds * DecayRate))", MessageType.None);
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 2. Reward Parameters
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Clear Reward Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            settings.scoreToGoldRatio = EditorGUILayout.Slider("Score to Gold Ratio", settings.scoreToGoldRatio, 0f, 1f);
            EditorGUILayout.HelpBox("Ratio to convert final score to Lobby Gold reward. (Gold Reward = Score * Ratio)", MessageType.None);
            EditorGUILayout.Space(5);

            settings.rewardGems = EditorGUILayout.IntField("Reward Gems", settings.rewardGems);
            EditorGUILayout.HelpBox("Fixed gem amount rewarded on stage completion.", MessageType.None);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 3. Formula Preview Calculator (Interactive)
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Formula Live Preview & Simulation", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Simulating variables
            int simGold = 100;
            int simHP = 20;
            int simTime = 120; // 2 minutes

            int simTimeBonus = Mathf.Max(0, settings.timeBonusBase - (simTime * settings.timeBonusDecay));
            int simScore = (simGold * settings.goldWeight) + (simHP * settings.hpWeight) + simTimeBonus;
            int simRewardGold = Mathf.FloorToInt(simScore * settings.scoreToGoldRatio);

            EditorGUILayout.HelpBox(
                $"[Simulation Case]\n" +
                $" - Remaining Gold: {simGold}\n" +
                $" - Remaining HP (Lives): {simHP}\n" +
                $" - Clear Time: {simTime} seconds (2:00)\n\n" +
                $"[Calculated Values]\n" +
                $" - Gold Points: {simGold} * {settings.goldWeight} = {simGold * settings.goldWeight}\n" +
                $" - HP Points: {simHP} * {settings.hpWeight} = {simHP * settings.hpWeight}\n" +
                $" - Time Bonus: Max(0, {settings.timeBonusBase} - ({simTime} * {settings.timeBonusDecay})) = {simTimeBonus}\n" +
                $" = Total Score: {simScore}\n\n" +
                $"[Lobby Rewards]\n" +
                $" - Gold Reward: {simScore} * {settings.scoreToGoldRatio:F2} = {simRewardGold} Gold\n" +
                $" - Gem Reward: {settings.rewardGems} Gems",
                MessageType.Info
            );

            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Settings", GUILayout.Height(25)))
            {
                LoadSettings();
            }
            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Reset Settings", "Are you sure you want to reset all score formula parameters to default values?", "Yes", "No"))
                {
                    settings.goldWeight = 10;
                    settings.hpWeight = 50;
                    settings.timeBonusBase = 10000;
                    settings.timeBonusDecay = 20;
                    settings.scoreToGoldRatio = 0.1f;
                    settings.rewardGems = 5;
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void LoadSettings()
        {
            settings = AssetDatabase.LoadAssetAtPath<ScoreCalculationSettings>(SETTINGS_PATH);
            // static 인스턴스 정보 초기화
            ScoreCalculationSettings.ResetInstance();
        }

        private void CreateSettings()
        {
            string dir = Path.GetDirectoryName(SETTINGS_PATH);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            settings = ScriptableObject.CreateInstance<ScoreCalculationSettings>();
            AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            ScoreCalculationSettings.ResetInstance();
        }
    }
}
