using UnityEditor;
using UnityEngine;
using TDF.Core.Data;
using System.IO;

namespace TDF.Editor.Modules
{
    public class AttributeEditorModule
    {
        private AttackAttributeSettings settings;
        private const string SETTINGS_PATH = "Assets/Resources/Settings/AttackAttributeSettings.asset";

        public void Draw()
        {
            if (settings == null)
            {
                LoadSettings();
            }

            if (settings == null)
            {
                if (GUILayout.Button("Create Attribute Settings Asset"))
                {
                    CreateSettings();
                }
                return;
            }

            EditorGUILayout.LabelField("Attack Attribute Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            // Fire Section
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Fire (Burn) Settings", EditorStyles.boldLabel);
            settings.fireBurnDamageRatio = EditorGUILayout.Slider("Damage Per Sec (%)", settings.fireBurnDamageRatio, 0f, 1f);
            settings.fireDuration = EditorGUILayout.FloatField("Duration (Sec)", settings.fireDuration);
            settings.fireEffectAnimation = (AnimationClip)EditorGUILayout.ObjectField("Fire Effect Animation", settings.fireEffectAnimation, typeof(AnimationClip), false);
            settings.fireEffectOffset = EditorGUILayout.Vector3Field("Fire Effect Offset", settings.fireEffectOffset);
            settings.fireEffectScale = EditorGUILayout.Vector3Field("Fire Effect Scale", settings.fireEffectScale);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Ice Section
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Ice (Cold) Settings", EditorStyles.boldLabel);
            settings.iceSlowMultiplier = EditorGUILayout.Slider("Movement Speed Multiplier", settings.iceSlowMultiplier, 0.1f, 1f);
            EditorGUILayout.HelpBox($"Target speed will be {settings.iceSlowMultiplier * 100}% of original.", MessageType.Info);
            settings.iceDuration = EditorGUILayout.FloatField("Duration (Sec)", settings.iceDuration);
            settings.iceEffectAnimation = (AnimationClip)EditorGUILayout.ObjectField("Ice Effect Animation", settings.iceEffectAnimation, typeof(AnimationClip), false);
            settings.iceEffectOffset = EditorGUILayout.Vector3Field("Ice Effect Offset", settings.iceEffectOffset);
            settings.iceEffectScale = EditorGUILayout.Vector3Field("Ice Effect Scale", settings.iceEffectScale);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Lightning Section
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Lightning (Electric) Settings", EditorStyles.boldLabel);
            settings.lightningStunDuration = EditorGUILayout.FloatField("Stun Duration (Sec)", settings.lightningStunDuration);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Tower Settings Section
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Tower Settings", EditorStyles.boldLabel);
            settings.multiAttackRange = EditorGUILayout.FloatField("Multi Attack Range", settings.multiAttackRange);
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                settings.ExtractSprites();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Re-load Settings"))
            {
                LoadSettings();
            }
        }

        private void LoadSettings()
        {
            settings = AssetDatabase.LoadAssetAtPath<AttackAttributeSettings>(SETTINGS_PATH);
        }

        private void CreateSettings()
        {
            string dir = Path.GetDirectoryName(SETTINGS_PATH);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            settings = ScriptableObject.CreateInstance<AttackAttributeSettings>();
            AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
