using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using TDF.Core.Data;

namespace TDF.Editor
{
    [InitializeOnLoad]
    public static class MapEditorHelper
    {
        static MapEditorHelper()
        {
            // 플레이모드가 시작될 때 자동으로 해상도를 2340x1080 (19.5:9)로 고정하도록 등록
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SetGameViewResolution(2340, 1080, "2340x1080 (19.5:9)");
            }
        }

        [MenuItem("Tools/TDF/Set Game View to 19.5:9 (2340x1080)")]
        public static void ForceResolutionMenu()
        {
            SetGameViewResolution(2340, 1080, "2340x1080 (19.5:9)");
            Debug.Log("[MapEditorHelper] Game View resolution has been forced to 2340x1080 (19.5:9).");
        }

        [MenuItem("Tools/TDF/Resize All Maps to 14x8")]
        public static void ResizeAllMapsTo14x8()
        {
            string[] guids = AssetDatabase.FindAssets("t:MapData");
            int count = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MapData mapData = AssetDatabase.LoadAssetAtPath<MapData>(path);
                if (mapData != null)
                {
                    if (mapData.gridWidth != 14 || mapData.gridHeight != 8)
                    {
                        Undo.RecordObject(mapData, "Resize Map to 14x8");
                        mapData.ResizeGrid(14, 8);
                        EditorUtility.SetDirty(mapData);
                        count++;
                    }
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[MapEditorHelper] Resized {count} map assets to 14x8.");
            EditorUtility.DisplayDialog("Map Resizer", $"[MapEditorHelper] Resized {count} map assets to 14x8.", "OK");
        }

        public static void SetGameViewResolution(int width, int height, string label)
        {
            try
            {
                // GameView 윈도우 획득
                Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                if (gameView == null) return;

                // GameViewSizes 획득
                Type sizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
                var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instanceProp = singleType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                var sizesInstance = instanceProp.GetValue(null, null);

                // 현재 활성화된 group 획득
                var currentGroupProp = sizesType.GetProperty("currentGroup", BindingFlags.Public | BindingFlags.Instance);
                var currentGroup = currentGroupProp.GetValue(sizesInstance, null);
                
                // Group 내부의 Custom Size 리스트 또는 메소드들 탐색
                Type groupType = currentGroup.GetType();
                var getGameViewSizeMethod = groupType.GetMethod("GetGameViewSize", new Type[] { typeof(int) });
                var getCountMethod = groupType.GetMethod("GetTotalCount");

                int totalCount = (int)getCountMethod.Invoke(currentGroup, null);
                
                // 이미 해당 해상도가 존재하는지 확인
                int targetIndex = -1;
                for (int i = 0; i < totalCount; i++)
                {
                    object size = getGameViewSizeMethod.Invoke(currentGroup, new object[] { i });
                    var widthProp = size.GetType().GetProperty("width");
                    var heightProp = size.GetType().GetProperty("height");
                    
                    int w = (int)widthProp.GetValue(size, null);
                    int h = (int)heightProp.GetValue(size, null);
                    
                    if (w == width && h == height)
                    {
                        targetIndex = i;
                        break;
                    }
                }

                // 존재하지 않으면 신규 추가
                if (targetIndex == -1)
                {
                    Type gameViewSize = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize");
                    Type sizeTypeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeType");
                    
                    // GameViewSizeType.FixedResolution (1) 값을 Enum 객체로 변환하여 생성자에 전달
                    object sizeTypeVal = Enum.ToObject(sizeTypeType, 1);
                    object sizeObj = Activator.CreateInstance(gameViewSize, new object[] { sizeTypeVal, width, height, label });
                    
                    var addCustomSizeMethod = groupType.GetMethod("AddCustomSize");
                    addCustomSizeMethod.Invoke(currentGroup, new object[] { sizeObj });
                    
                    // 추가 후 인덱스 갱신
                    targetIndex = (int)getCountMethod.Invoke(currentGroup, null) - 1;
                }

                // 해당 해상도로 선택 변경
                var selectedSizeIndexProp = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                selectedSizeIndexProp.SetValue(gameView, targetIndex, null);
                
                // 강제 리페인트
                gameView.Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MapEditorHelper] Failed to change GameView size via reflection: {ex.Message}");
            }
        }
    }
}
