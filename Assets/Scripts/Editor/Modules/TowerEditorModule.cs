using UnityEditor;
using UnityEngine;
using TDF.Core.Data;

namespace TDF.Editor.Modules
{
    public class TowerEditorModule
    {
        private TowerData targetTower;
        private Vector2 scrollPos;

        public void Draw()
        {
            GUILayout.BeginVertical("box");
            targetTower = (TowerData)EditorGUILayout.ObjectField("Target Tower Data", targetTower, typeof(TowerData), false);

            if (targetTower == null)
            {
                EditorGUILayout.HelpBox("TowerData 에셋을 위 슬롯에 드래그 앤 드롭 하세요.", MessageType.Warning);
                GUILayout.EndVertical();
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            DrawBasicInfo();
            GUILayout.Space(10);
            DrawProperties();
            GUILayout.Space(10);
            DrawUpgradeTiers();
            GUILayout.Space(10);
            DrawAssets();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetTower);
            }
        }

        private void DrawBasicInfo()
        {
            GUILayout.Label("Basic Info", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string newId = EditorGUILayout.TextField("Tower ID", targetTower.towerId);
            string newName = EditorGUILayout.TextField("Tower Name", targetTower.towerName);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetTower, "Edit Tower Basic Info");
                targetTower.towerId = newId;
                targetTower.towerName = newName;
            }
        }

        private void DrawProperties()
        {
            GUILayout.Label("Properties", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var newAttr = (AttackAttribute)EditorGUILayout.EnumPopup("Attack Attribute", targetTower.attackAttribute);
            var newType = (AttackType)EditorGUILayout.EnumPopup("Attack Type", targetTower.attackType);
            var newTarget = (TargetType)EditorGUILayout.EnumPopup("Target Type", targetTower.targetType);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetTower, "Edit Tower Properties");
                targetTower.attackAttribute = newAttr;
                targetTower.attackType = newType;
                targetTower.targetType = newTarget;
            }
        }

        private void DrawUpgradeTiers()
        {
            GUILayout.Label("Upgrade Tiers (0 = Base)", EditorStyles.boldLabel);

            for (int i = 0; i < targetTower.upgradeTiers.Count; i++)
            {
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Tier {targetTower.upgradeTiers[i].tierLevel}", EditorStyles.boldLabel, GUILayout.Width(100));
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    Undo.RecordObject(targetTower, "Remove Tier");
                    targetTower.upgradeTiers.RemoveAt(i);
                    break; // Exit loop to avoid collection modified exception
                }
                GUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                var tier = targetTower.upgradeTiers[i];
                
                GUILayout.BeginHorizontal();
                tier.buildOrUpgradeCost = EditorGUILayout.IntField("Cost", tier.buildOrUpgradeCost);
                tier.sellPrice = EditorGUILayout.IntField("Sell Price", tier.sellPrice);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                tier.damage = EditorGUILayout.FloatField("Damage", tier.damage);
                tier.range = EditorGUILayout.FloatField("Range", tier.range);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                tier.attackSpeed = EditorGUILayout.FloatField("Atk Speed", tier.attackSpeed);
                tier.projectileSpeed = EditorGUILayout.FloatField("Proj Speed", tier.projectileSpeed);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                tier.manaCost = EditorGUILayout.FloatField("Mana Cost", tier.manaCost);
                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetTower, "Edit Tier");
                }
                GUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Upgrade Tier"))
            {
                Undo.RecordObject(targetTower, "Add Tier");
                int newLevel = targetTower.upgradeTiers.Count > 0 ? targetTower.upgradeTiers[^1].tierLevel + 1 : 0;
                targetTower.upgradeTiers.Add(new TowerUpgradeTier { tierLevel = newLevel });
            }
        }

        private void DrawAssets()
        {
            GUILayout.Label("Visual & Prefab Assets", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            
            if (targetTower.assets == null) targetTower.assets = new TowerAssets();
            var assets = targetTower.assets;
            
            assets.idleSprite = (Sprite)EditorGUILayout.ObjectField("Idle Sprite", assets.idleSprite, typeof(Sprite), false);
            assets.attackSprite = (Sprite)EditorGUILayout.ObjectField("Attack Sprite", assets.attackSprite, typeof(Sprite), false);
            
            if (assets.attackSprites8Dir == null) assets.attackSprites8Dir = new DirectionalSprites();
            var dirs = assets.attackSprites8Dir;
            GUILayout.Label("8-Direction Attack Animations", EditorStyles.boldLabel);
            dirs.up = (AnimationClip)EditorGUILayout.ObjectField("Up (8)", dirs.up, typeof(AnimationClip), false);
            dirs.down = (AnimationClip)EditorGUILayout.ObjectField("Down (2)", dirs.down, typeof(AnimationClip), false);
            dirs.left = (AnimationClip)EditorGUILayout.ObjectField("Left (4)", dirs.left, typeof(AnimationClip), false);
            dirs.right = (AnimationClip)EditorGUILayout.ObjectField("Right (6)", dirs.right, typeof(AnimationClip), false);
            dirs.upLeft = (AnimationClip)EditorGUILayout.ObjectField("Up-Left (7)", dirs.upLeft, typeof(AnimationClip), false);
            dirs.upRight = (AnimationClip)EditorGUILayout.ObjectField("Up-Right (9)", dirs.upRight, typeof(AnimationClip), false);
            dirs.downLeft = (AnimationClip)EditorGUILayout.ObjectField("Down-Left (1)", dirs.downLeft, typeof(AnimationClip), false);
            dirs.downRight = (AnimationClip)EditorGUILayout.ObjectField("Down-Right (3)", dirs.downRight, typeof(AnimationClip), false);
            
            assets.idleAnim = (AnimationClip)EditorGUILayout.ObjectField("Idle Anim", assets.idleAnim, typeof(AnimationClip), false);
            assets.attackAnim = (AnimationClip)EditorGUILayout.ObjectField("Attack Anim", assets.attackAnim, typeof(AnimationClip), false);
            assets.prefab = (GameObject)EditorGUILayout.ObjectField("Tower Prefab", assets.prefab, typeof(GameObject), false);
            assets.projectilePrefab = (GameObject)EditorGUILayout.ObjectField("Projectile Prefab", assets.projectilePrefab, typeof(GameObject), false);
            assets.hitEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Hit Effect Prefab", assets.hitEffectPrefab, typeof(GameObject), false);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetTower, "Edit Tower Assets");
                targetTower.assets = assets;
            }
        }
    }
}
