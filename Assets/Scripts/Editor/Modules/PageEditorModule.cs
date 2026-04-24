using UnityEditor;
using UnityEngine;
using TDF.Core.Data;
using System.Collections.Generic;

namespace TDF.Editor.Modules
{
    public class PageEditorModule
    {
        private enum SubTab { PageRegistry, PageContent }
        private SubTab currentSubTab = SubTab.PageRegistry;
        private PageType selectedContentType = PageType.Shop;
        private Vector2 scrollPos;

        public void Draw()
        {
            GUILayout.BeginVertical("box");
            DrawHeader();
            
            GUILayout.Space(10);
            
            switch (currentSubTab)
            {
                case SubTab.PageRegistry:
                    DrawPageRegistry();
                    break;
                case SubTab.PageContent:
                    DrawPageContent();
                    break;
            }
            GUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(currentSubTab == SubTab.PageRegistry, "Page Registry", "ButtonLeft"))
                currentSubTab = SubTab.PageRegistry;
            if (GUILayout.Toggle(currentSubTab == SubTab.PageContent, "Page Content Editor", "ButtonRight"))
                currentSubTab = SubTab.PageContent;
            GUILayout.EndHorizontal();
        }

        private void DrawPageRegistry()
        {
            GUILayout.Label("Registered Pages", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create New Page Data", GUILayout.Height(30)))
            {
                CreateNewPageData();
            }

            GUILayout.Space(10);
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            string[] guids = AssetDatabase.FindAssets("t:PageData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PageData data = AssetDatabase.LoadAssetAtPath<PageData>(path);
                
                if (data != null)
                {
                    GUILayout.BeginVertical("box");
                    EditorGUI.BeginChangeCheck();
                    
                    data.pageId = EditorGUILayout.TextField("Page ID", data.pageId);
                    data.pageType = (PageType)EditorGUILayout.EnumPopup("Page Type", data.pageType);
                    data.uiPrefab = (GameObject)EditorGUILayout.ObjectField("UI Prefab", data.uiPrefab, typeof(GameObject), false);
                    data.isPopup = EditorGUILayout.Toggle("Is Popup", data.isPopup);

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(data);
                    }
                    
                    if (GUILayout.Button("Select Asset", GUILayout.Width(100)))
                    {
                        Selection.activeObject = data;
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(5);
                }
            }
            GUILayout.EndScrollView();
        }

        private void DrawPageContent()
        {
            GUILayout.Label("Page Content Editor (Meta Items)", EditorStyles.boldLabel);
            selectedContentType = (PageType)EditorGUILayout.EnumPopup("Select Target Page", selectedContentType);
            
            GUILayout.Space(10);
            
            if (selectedContentType == PageType.Shop)
            {
                DrawShopEditor();
            }
            else if (selectedContentType == PageType.Achievement)
            {
                DrawAchievementEditor();
            }
            else if (selectedContentType == PageType.Event)
            {
                DrawEventEditor();
            }
            else
            {
                EditorGUILayout.HelpBox($"No specific content editor implemented for {selectedContentType} yet.", MessageType.Info);
            }
        }

        private void DrawShopEditor()
        {
            if (GUILayout.Button("Create New Shop Item", GUILayout.Height(30)))
            {
                ShopItemData newItem = ScriptableObject.CreateInstance<ShopItemData>();
                CreateAssetSafe(newItem, "Assets/Data/ShopItems", "NewShopItem.asset");
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            string[] guids = AssetDatabase.FindAssets("t:ShopItemData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ShopItemData data = AssetDatabase.LoadAssetAtPath<ShopItemData>(path);
                if (data != null)
                {
                    GUILayout.BeginVertical("box");
                    EditorGUI.BeginChangeCheck();
                    
                    data.itemId = EditorGUILayout.TextField("Item ID", data.itemId);
                    data.itemName = EditorGUILayout.TextField("Item Name", data.itemName);
                    data.costGold = EditorGUILayout.IntField("Cost (Gold)", data.costGold);
                    data.costGems = EditorGUILayout.IntField("Cost (Gems)", data.costGems);
                    data.itemIcon = (Sprite)EditorGUILayout.ObjectField("Icon", data.itemIcon, typeof(Sprite), false);
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(data);
                    }
                    if (GUILayout.Button("Select", GUILayout.Width(60))) Selection.activeObject = data;
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndScrollView();
        }

        private void DrawAchievementEditor()
        {
            if (GUILayout.Button("Create New Achievement", GUILayout.Height(30)))
            {
                AchievementData newAchieve = ScriptableObject.CreateInstance<AchievementData>();
                CreateAssetSafe(newAchieve, "Assets/Data/Achievements", "NewAchievement.asset");
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            string[] guids = AssetDatabase.FindAssets("t:AchievementData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AchievementData data = AssetDatabase.LoadAssetAtPath<AchievementData>(path);
                if (data != null)
                {
                    GUILayout.BeginVertical("box");
                    EditorGUI.BeginChangeCheck();
                    
                    data.achievementId = EditorGUILayout.TextField("Achievement ID", data.achievementId);
                    data.achievementName = EditorGUILayout.TextField("Name", data.achievementName);
                    data.targetValue = EditorGUILayout.IntField("Target Value", data.targetValue);
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(data);
                    }
                    if (GUILayout.Button("Select", GUILayout.Width(60))) Selection.activeObject = data;
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndScrollView();
        }

        private void DrawEventEditor()
        {
            if (GUILayout.Button("Create New Event", GUILayout.Height(30)))
            {
                EventData newEvent = ScriptableObject.CreateInstance<EventData>();
                CreateAssetSafe(newEvent, "Assets/Data/Events", "NewEvent.asset");
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            string[] guids = AssetDatabase.FindAssets("t:EventData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EventData data = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (data != null)
                {
                    GUILayout.BeginVertical("box");
                    EditorGUI.BeginChangeCheck();
                    
                    data.eventId = EditorGUILayout.TextField("Event ID", data.eventId);
                    data.eventName = EditorGUILayout.TextField("Event Name", data.eventName);
                    data.startDate = EditorGUILayout.TextField("Start Date", data.startDate);
                    data.endDate = EditorGUILayout.TextField("End Date", data.endDate);
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(data);
                    }
                    if (GUILayout.Button("Select", GUILayout.Width(60))) Selection.activeObject = data;
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndScrollView();
        }

        private void CreateNewPageData()
        {
            PageData newPage = ScriptableObject.CreateInstance<PageData>();
            CreateAssetSafe(newPage, "Assets/Data/Pages", "NewPageData.asset");
        }

        private void CreateAssetSafe(ScriptableObject asset, string folderPath, string defaultName)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string[] parts = folderPath.Split('/');
                string currentPath = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    if (!AssetDatabase.IsValidFolder(currentPath + "/" + parts[i]))
                    {
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    }
                    currentPath += "/" + parts[i];
                }
            }

            string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{defaultName}");
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
        }
    }
}
