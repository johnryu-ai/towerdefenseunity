using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using TDF.Core.Data;

namespace TDF.Editor.Modules
{
    public class MapEditorModule
    {
        private MapData targetMap;
        private Vector2 scrollPos;
        private TileType currentBrush = TileType.Path;
        private int editingSpawnPathIndex = -1;

        private const float TILE_SIZE = 40f;
        private const float WORLD_TILE_SIZE = 1f;

        public void Draw()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical("box");
            targetMap = (MapData)EditorGUILayout.ObjectField("Target Map Data", targetMap, typeof(MapData), false);
            
            if (targetMap == null)
            {
                EditorGUILayout.HelpBox("MapData 에셋을 위 슬롯에 드래그 앤 드롭 하세요.", MessageType.Warning);
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                return;
            }

            DrawMapSettings();
            GUILayout.Space(10);
            DrawBrushSettings();
            GUILayout.Space(10);
            DrawGrid();
            GUILayout.Space(10);
            DrawSpawnPointsSettings();

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
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
            
            if (editingSpawnPathIndex != -1)
            {
                EditorGUILayout.HelpBox($"Currently Editing Manual Path for Spawn Point {editingSpawnPathIndex}.\nLeft Click on Grid to add waypoint. Right click to remove last waypoint.\nClick 'Stop Editing' to return to normal brush.", MessageType.Info);
                if (GUILayout.Button("Stop Editing Path", GUILayout.Height(30)))
                {
                    editingSpawnPathIndex = -1;
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                currentBrush = (TileType)EditorGUILayout.EnumPopup("Tile Brush", currentBrush);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawSpawnPointsSettings()
        {
            GUILayout.Label("Spawn Points & Air Paths", EditorStyles.boldLabel);

            if (GUILayout.Button("Sync Spawn Points from Grid"))
            {
                SyncSpawnPointsFromGrid();
            }

            if (targetMap.spawnPoints.Count == 0)
            {
                EditorGUILayout.HelpBox("등록된 스폰 포인트가 없습니다. Grid에서 Spawn 타일을 배치한 후 Sync 버튼을 누르세요.", MessageType.Info);
                return;
            }

            for (int i = 0; i < targetMap.spawnPoints.Count; i++)
            {
                var sp = targetMap.spawnPoints[i];
                GUILayout.BeginVertical("box");
                
                EditorGUI.BeginChangeCheck();
                
                GUILayout.Label($"Spawn Point {i} (Grid: {sp.coordinate.x}, {sp.coordinate.y})");
                string newId = EditorGUILayout.TextField("Spawn ID", sp.spawnId);
                
                GUILayout.Space(5);
                GUILayout.Label("Manual Path Waypoints (For Air Units / Unconnected paths)");
                
                SerializedObject serializedMap = new SerializedObject(targetMap);
                SerializedProperty spawnPointsProp = serializedMap.FindProperty("spawnPoints");
                SerializedProperty spProp = spawnPointsProp.GetArrayElementAtIndex(i);
                SerializedProperty pathProp = spProp.FindPropertyRelative("pathWaypoints");
                
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(pathProp, new GUIContent("Path Waypoints"), true);
                if (editingSpawnPathIndex == i)
                {
                    if (GUILayout.Button("Stop Edit", GUILayout.Width(80))) editingSpawnPathIndex = -1;
                }
                else
                {
                    if (GUILayout.Button("Edit Path", GUILayout.Width(80))) editingSpawnPathIndex = i;
                }
                GUILayout.EndHorizontal();
                
                if (GUILayout.Button("Clear Path", GUILayout.Width(100)))
                {
                    Undo.RecordObject(targetMap, "Clear Spawn Path");
                    sp.pathWaypoints.Clear();
                    EditorUtility.SetDirty(targetMap);
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetMap, "Edit Spawn Point");
                    sp.spawnId = newId;
                    serializedMap.ApplyModifiedProperties();
                }

                GUILayout.EndVertical();
            }
        }

        private void SyncSpawnPointsFromGrid()
        {
            Undo.RecordObject(targetMap, "Sync Spawn Points");
            List<Vector2Int> foundSpawnsBL = new List<Vector2Int>();
            for (int y = 0; y < targetMap.gridHeight; y++)
            {
                for (int x = 0; x < targetMap.gridWidth; x++)
                {
                        int blY = targetMap.gridHeight - 1 - y;
                        if (targetMap.GetTileAt(x, blY) == TileType.Spawn)
                        {
                            foundSpawnsBL.Add(new Vector2Int(x, blY));
                        }
                }
            }

            // Remove non-existent ones
            targetMap.spawnPoints.RemoveAll(sp => !foundSpawnsBL.Contains(sp.coordinate));

            // Add new ones
            foreach (var pos in foundSpawnsBL)
            {
                if (!targetMap.spawnPoints.Exists(sp => sp.coordinate == pos))
                {
                    targetMap.spawnPoints.Add(new SpawnPointData()
                    {
                        spawnId = $"Spawn_{pos.x}_{pos.y}",
                        spawnIndex = targetMap.spawnPoints.Count,
                        coordinate = pos,
                        pathWaypoints = new List<Vector2>()
                    });
                }
            }
            EditorUtility.SetDirty(targetMap);
        }

        private void DrawGrid()
        {
            GUILayout.Label("Grid Editor", EditorStyles.boldLabel);

            // 그리드 렌더링 영역 크기 계산
            float reqWidth = targetMap.gridWidth * TILE_SIZE;
            float reqHeight = targetMap.gridHeight * TILE_SIZE;
            Rect gridRect = GUILayoutUtility.GetRect(reqWidth, reqHeight, GUILayout.Width(reqWidth), GUILayout.Height(reqHeight), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            
            if (Event.current.type == EventType.Repaint)
            {
                for (int guiY = 0; guiY < targetMap.gridHeight; guiY++)
                {
                    for (int x = 0; x < targetMap.gridWidth; x++)
                    {
                        int y = targetMap.gridHeight - 1 - guiY; // Bottom-Left Y
                        Rect tileRect = new Rect(gridRect.x + x * TILE_SIZE, gridRect.y + guiY * TILE_SIZE, TILE_SIZE, TILE_SIZE);
                        TileType tile = targetMap.GetTileAt(x, y);
                        
                        // 타일 색상 결정
                        Color tileColor = GetColorForTile(tile);
                        EditorGUI.DrawRect(tileRect, tileColor);
                        
                        // 테두리
                        Handles.color = Color.black;
                        Handles.DrawWireCube(tileRect.center, tileRect.size);
                    }
                }

                // Render Manual Path if editing
                if (editingSpawnPathIndex >= 0 && editingSpawnPathIndex < targetMap.spawnPoints.Count)
                {
                    var sp = targetMap.spawnPoints[editingSpawnPathIndex];
                    if (sp.pathWaypoints != null && sp.pathWaypoints.Count > 0)
                    {
                        Handles.color = Color.cyan;
                        Vector3[] linePoints = new Vector3[sp.pathWaypoints.Count + 1];
                        
                        // 스폰 포인트 시작점 (Grid)
                        int startGuiX = sp.coordinate.x;
                        int startGuiY = targetMap.gridHeight - 1 - sp.coordinate.y;
                        linePoints[0] = new Vector3(gridRect.x + startGuiX * TILE_SIZE + TILE_SIZE / 2f, gridRect.y + startGuiY * TILE_SIZE + TILE_SIZE / 2f, 0);

                        for (int i = 0; i < sp.pathWaypoints.Count; i++)
                        {
                            Vector2 blGridPos = sp.pathWaypoints[i];
                            // guiY converts Bottom-Left origin to Top-Left GUI origin
                            int guiX = Mathf.RoundToInt(blGridPos.x);
                            int guiY = targetMap.gridHeight - 1 - Mathf.RoundToInt(blGridPos.y);
                            
                            Vector3 center = new Vector3(gridRect.x + guiX * TILE_SIZE + TILE_SIZE / 2f, gridRect.y + guiY * TILE_SIZE + TILE_SIZE / 2f, 0);
                            linePoints[i + 1] = center;
                            
                            // Draw Point
                            Handles.DrawSolidDisc(center, Vector3.forward, 5f);
                            
                            // Draw Number
                            GUIStyle style = new GUIStyle();
                            style.normal.textColor = Color.black;
                            style.fontStyle = FontStyle.Bold;
                            Handles.Label(center + new Vector3(5, -10, 0), (i + 1).ToString(), style);
                        }
                        
                        Handles.DrawPolyLine(linePoints);
                    }
                }
            }

            // 마우스 클릭 및 드래그 이벤트 처리 (페인팅)
            HandleMouseEvents(gridRect);
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
                        int guiY = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / TILE_SIZE);
                        int y = targetMap.gridHeight - 1 - guiY; // Bottom-Left Y

                        if (editingSpawnPathIndex >= 0 && editingSpawnPathIndex < targetMap.spawnPoints.Count)
                        {
                            // Edit Path Mode
                            if (e.type == EventType.MouseDown) // 드래그 방지, 클릭만
                            {
                                Undo.RecordObject(targetMap, "Add Spawn Waypoint");
                                Vector2 gridCoord = new Vector2(x, y);
                                targetMap.spawnPoints[editingSpawnPathIndex].pathWaypoints.Add(gridCoord);
                                EditorUtility.SetDirty(targetMap);
                                e.Use();
                            }
                        }
                        else
                        {
                            // Normal Brush Mode
                            if (targetMap.GetTileAt(x, y) != currentBrush)
                            {
                                Undo.RecordObject(targetMap, "Paint Tile");
                                targetMap.SetTileAt(x, y, currentBrush);
                                EditorUtility.SetDirty(targetMap);
                            }
                            e.Use();
                        }
                    }
                    else if (e.button == 1 && e.type == EventType.MouseDown) // 오른쪽 클릭
                    {
                        if (editingSpawnPathIndex >= 0 && editingSpawnPathIndex < targetMap.spawnPoints.Count)
                        {
                            var list = targetMap.spawnPoints[editingSpawnPathIndex].pathWaypoints;
                            if (list.Count > 0)
                            {
                                Undo.RecordObject(targetMap, "Remove Spawn Waypoint");
                                list.RemoveAt(list.Count - 1);
                                EditorUtility.SetDirty(targetMap);
                                e.Use();
                            }
                        }
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

        private Vector2 GridToWorldPosition(int x, int y)
        {
            float offsetX = -targetMap.gridWidth * WORLD_TILE_SIZE / 2f + (WORLD_TILE_SIZE / 2f);
            float offsetY = targetMap.gridHeight * WORLD_TILE_SIZE / 2f - (WORLD_TILE_SIZE / 2f);
            return new Vector2(offsetX + (x * WORLD_TILE_SIZE), offsetY - (y * WORLD_TILE_SIZE));
        }

        private Vector2Int WorldToGridPosition(Vector2 worldPos)
        {
            float offsetX = -targetMap.gridWidth * WORLD_TILE_SIZE / 2f + (WORLD_TILE_SIZE / 2f);
            float offsetY = targetMap.gridHeight * WORLD_TILE_SIZE / 2f - (WORLD_TILE_SIZE / 2f);
            
            int x = Mathf.RoundToInt((worldPos.x - offsetX) / WORLD_TILE_SIZE);
            int y = Mathf.RoundToInt((offsetY - worldPos.y) / WORLD_TILE_SIZE);
            return new Vector2Int(x, y);
        }
    }
}
