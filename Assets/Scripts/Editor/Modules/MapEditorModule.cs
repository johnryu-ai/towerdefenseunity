using UnityEditor;
using UnityEngine;
using TDF.Core.Data;

namespace TDF.Editor.Modules
{
    public class MapEditorModule
    {
        private MapData targetMap;
        private Vector2 scrollPos;
        private TileType currentBrush = TileType.Path;

        private const float TILE_SIZE = 40f;

        public void Draw()
        {
            GUILayout.BeginVertical("box");
            targetMap = (MapData)EditorGUILayout.ObjectField("Target Map Data", targetMap, typeof(MapData), false);
            
            if (targetMap == null)
            {
                EditorGUILayout.HelpBox("MapData 에셋을 위 슬롯에 드래그 앤 드롭 하세요.", MessageType.Warning);
                GUILayout.EndVertical();
                return;
            }

            DrawMapSettings();
            GUILayout.Space(10);
            DrawBrushSettings();
            GUILayout.Space(10);
            DrawGrid();

            GUILayout.EndVertical();
        }

        private void DrawMapSettings()
        {
            GUILayout.Label("Map Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            
            int newWidth = EditorGUILayout.IntField("Grid Width", targetMap.gridWidth);
            int newHeight = EditorGUILayout.IntField("Grid Height", targetMap.gridHeight);
            
            if (EditorGUI.EndChangeCheck())
            {
                newWidth = Mathf.Max(1, newWidth);
                newHeight = Mathf.Max(1, newHeight);
                Undo.RecordObject(targetMap, "Resize Map Grid");
                targetMap.ResizeGrid(newWidth, newHeight);
                EditorUtility.SetDirty(targetMap);
            }

            GUILayout.Space(10);
            GUILayout.Label("Map Config", EditorStyles.boldLabel);
            
            // MapConfig 속성들을 직렬화 오브젝트로 그려줌 (리스트 편집을 쉽게 하기 위함)
            SerializedObject serializedMap = new SerializedObject(targetMap);
            SerializedProperty configProp = serializedMap.FindProperty("config");
            if (configProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(configProp, new GUIContent("Game Rules"), true);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedMap.ApplyModifiedProperties();
                }
            }
        }

        private void DrawBrushSettings()
        {
            GUILayout.Label("Brush Settings", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            
            currentBrush = (TileType)EditorGUILayout.EnumPopup("Tile Brush", currentBrush);
            
            GUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            GUILayout.Label("Grid Editor", EditorStyles.boldLabel);
            
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            // 그리드 렌더링 영역 크기 계산
            Rect gridRect = GUILayoutUtility.GetRect(targetMap.gridWidth * TILE_SIZE, targetMap.gridHeight * TILE_SIZE);
            
            if (Event.current.type == EventType.Repaint)
            {
                for (int y = 0; y < targetMap.gridHeight; y++)
                {
                    for (int x = 0; x < targetMap.gridWidth; x++)
                    {
                        Rect tileRect = new Rect(gridRect.x + x * TILE_SIZE, gridRect.y + y * TILE_SIZE, TILE_SIZE, TILE_SIZE);
                        TileType tile = targetMap.GetTileAt(x, y);
                        
                        // 타일 색상 결정
                        Color tileColor = GetColorForTile(tile);
                        EditorGUI.DrawRect(tileRect, tileColor);
                        
                        // 테두리
                        Handles.color = Color.black;
                        Handles.DrawWireCube(tileRect.center, tileRect.size);
                    }
                }
            }

            // 마우스 클릭 및 드래그 이벤트 처리 (페인팅)
            HandleMouseEvents(gridRect);

            GUILayout.EndScrollView();
        }

        private void HandleMouseEvents(Rect gridRect)
        {
            Event e = Event.current;
            
            if (gridRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    if (e.button == 0) // 왼쪽 클릭
                    {
                        int x = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / TILE_SIZE);
                        int y = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / TILE_SIZE);

                        if (targetMap.GetTileAt(x, y) != currentBrush)
                        {
                            Undo.RecordObject(targetMap, "Paint Tile");
                            targetMap.SetTileAt(x, y, currentBrush);
                            EditorUtility.SetDirty(targetMap);
                        }
                        
                        e.Use(); // 이벤트 소모
                    }
                }
            }
        }

        private Color GetColorForTile(TileType type)
        {
            switch (type)
            {
                case TileType.Path: return new Color(0.6f, 0.4f, 0.2f); // Brown
                case TileType.Buildable: return Color.green;
                case TileType.NonBuildable: return Color.gray;
                case TileType.Spawn: return new Color(0.5f, 0f, 0.5f); // Purple
                case TileType.Base: return Color.blue;
                default: return Color.white;
            }
        }
    }
}
