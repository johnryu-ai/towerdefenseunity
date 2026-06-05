using UnityEditor;
using UnityEngine;
using TDF.Core.Data;

namespace TDF.Editor.Modules
{
    public class MonsterEditorModule
    {
        private MonsterData targetMonster;
        private Vector2 scrollPos;

        public void Draw()
        {
            GUILayout.BeginVertical("box");
            targetMonster = (MonsterData)EditorGUILayout.ObjectField("Target Monster Data", targetMonster, typeof(MonsterData), false);

            if (targetMonster == null)
            {
                EditorGUILayout.HelpBox("MonsterData 에셋을 위 슬롯에 드래그 앤 드롭 하세요.", MessageType.Warning);
                GUILayout.EndVertical();
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            DrawBasicInfo();
            GUILayout.Space(10);
            DrawStats();
            GUILayout.Space(10);
            DrawMovement();
            GUILayout.Space(10);
            DrawSpecialLogic();
            GUILayout.Space(10);
            DrawAssets();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetMonster);
            }
        }

        private void DrawBasicInfo()
        {
            GUILayout.Label("Basic Info", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string newId = EditorGUILayout.TextField("Monster ID", targetMonster.monsterId);
            string newName = EditorGUILayout.TextField("Monster Name", targetMonster.monsterName);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetMonster, "Edit Monster Basic Info");
                targetMonster.monsterId = newId;
                targetMonster.monsterName = newName;
            }
        }

        private void DrawStats()
        {
            GUILayout.Label("Combat Stats", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            
            if (targetMonster.stats == null) targetMonster.stats = new MonsterStats();
            var stats = targetMonster.stats;

            stats.health = EditorGUILayout.FloatField("Health", stats.health);
            stats.moveSpeed = EditorGUILayout.FloatField("Move Speed", stats.moveSpeed);
            stats.killReward = EditorGUILayout.IntField("Kill Reward (Gold)", stats.killReward);
            stats.baseDamage = EditorGUILayout.IntField("Base Damage (To Player)", stats.baseDamage);
            
            // Immunity Selection (Mapping user terms to internal enums)
            string[] immuneOptions = { "None", "Fire", "Ice", "Lightning" };
            AttackAttribute[] immuneValues = { AttackAttribute.Normal, AttackAttribute.Fire, AttackAttribute.Cold, AttackAttribute.Electric };
            
            int currentIndex = System.Array.IndexOf(immuneValues, stats.immuneAttribute);
            if (currentIndex == -1) currentIndex = 0; // Default to None

            int nextIndex = EditorGUILayout.Popup("Immune Attribute", currentIndex, immuneOptions);
            stats.immuneAttribute = immuneValues[nextIndex];

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetMonster, "Edit Monster Stats");
                targetMonster.stats = stats;
            }
        }

        private void DrawMovement()
        {
            GUILayout.Label("Movement Properties", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var newFlyType = (MonsterFlyType)EditorGUILayout.EnumPopup("Fly Type (Ground/Air)", targetMonster.flyType);
            var newMovement = (MonsterMovementType)EditorGUILayout.EnumPopup("Movement Path Type", targetMonster.movementType);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetMonster, "Edit Monster Movement");
                targetMonster.flyType = newFlyType;
                targetMonster.movementType = newMovement;
            }
        }

        private void DrawSpecialLogic()
        {
            GUILayout.Label("Special Logic (Split on Death)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            
            if (targetMonster.splitLogic == null) targetMonster.splitLogic = new SplitLogic();
            var logic = targetMonster.splitLogic;

            logic.splitOnDeath = EditorGUILayout.Toggle("Enable Splitting", logic.splitOnDeath);

            if (logic.splitOnDeath)
            {
                EditorGUI.indentLevel++;
                logic.splitMonsterType = (MonsterData)EditorGUILayout.ObjectField("Split Monster Spawn", logic.splitMonsterType, typeof(MonsterData), false);
                logic.splitCount = EditorGUILayout.IntField("Split Count", logic.splitCount);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetMonster, "Edit Monster Split Logic");
                targetMonster.splitLogic = logic;
            }
        }

        private void DrawAssets()
        {
            GUILayout.Label("Visual & Prefab Assets", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            
            if (targetMonster.assets == null) targetMonster.assets = new MonsterAssets();
            var assets = targetMonster.assets;

            assets.moveSprite = (Sprite)EditorGUILayout.ObjectField("Move Sprite", assets.moveSprite, typeof(Sprite), false);
            assets.hitSprite = (Sprite)EditorGUILayout.ObjectField("Hit Sprite", assets.hitSprite, typeof(Sprite), false);
            assets.dieSprite = (Sprite)EditorGUILayout.ObjectField("Die Sprite", assets.dieSprite, typeof(Sprite), false);
            assets.moveAnim = (AnimationClip)EditorGUILayout.ObjectField("Move Animation", assets.moveAnim, typeof(AnimationClip), false);
            assets.visualScale = EditorGUILayout.FloatField("Visual Scale (Tile)", assets.visualScale);
            assets.visualOffsetY = EditorGUILayout.FloatField("Visual Offset Y", assets.visualOffsetY);
            assets.prefab = (GameObject)EditorGUILayout.ObjectField("Monster Prefab", assets.prefab, typeof(GameObject), false);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetMonster, "Edit Monster Assets");
                targetMonster.assets = assets;
            }
        }
    }
}
