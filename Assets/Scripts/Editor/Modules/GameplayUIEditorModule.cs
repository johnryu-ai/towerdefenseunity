using UnityEditor;
using UnityEngine;
using TDF.Core.Data;
using System.IO;

namespace TDF.Editor.Modules
{
    public class GameplayUIEditorModule
    {
        private GameplayUISettings settings;
        private const string SETTINGS_PATH = "Assets/Resources/Settings/GameplayUISettings.asset";

        // 드래그 상태 관리
        private enum DraggedArea { None, TopBar, LeftPanel, RightPanel }
        private DraggedArea activeDragArea = DraggedArea.None;
        private Vector2 dragStartMousePos;
        private Rect dragStartRect;

        // 프리뷰 축소 배율 (1920x1080 -> 480x270)
        private const float PREVIEW_SCALE = 0.25f;
        private const float VIRTUAL_WIDTH = 2340f;
        private const float VIRTUAL_HEIGHT = 1080f;

        public void Draw()
        {
            if (settings == null)
            {
                LoadSettings();
            }

            if (settings == null)
            {
                EditorGUILayout.HelpBox("Gameplay UI Settings asset not found. Click the button below to create it.", MessageType.Warning);
                if (GUILayout.Button("Create Gameplay UI Settings Asset", GUILayout.Height(30)))
                {
                    CreateSettings();
                }
                return;
            }

            SerializedObject serializedSettings = new SerializedObject(settings);
            serializedSettings.Update();

            EditorGUILayout.LabelField("Gameplay UI Screen Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();

            // 왼쪽: 속성 조절 패널
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(350), GUILayout.ExpandHeight(true));
            DrawSettingsProperties(serializedSettings);
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // 오른쪽: 시각적 드래그 프리뷰 캔버스
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawVisualCanvas();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (serializedSettings.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawSettingsProperties(SerializedObject serializedSettings)
        {
            EditorGUILayout.LabelField("UI Configuration Properties", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 배경 이미지 등록
            SerializedProperty bgProp = serializedSettings.FindProperty("backgroundImage");
            EditorGUILayout.PropertyField(bgProp, new GUIContent("Background Image"));
            EditorGUILayout.Space(10);

            // 상단 정보 바 Rect
            DrawRectControls("Top Info Bar UI", serializedSettings.FindProperty("topBarRect"));
            EditorGUILayout.Space(10);

            // 왼쪽 설치 UI Rect
            DrawRectControls("Left Tower Build UI", serializedSettings.FindProperty("leftPanelRect"));
            EditorGUILayout.Space(10);

            // 오른쪽 조작 UI Rect
            DrawRectControls("Right Tower Upgrade UI", serializedSettings.FindProperty("rightPanelRect"));
            EditorGUILayout.Space(15);

            // 리셋 버튼
            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Reset Settings", "Are you sure you want to reset UI layout to defaults?", "Yes", "No"))
                {
                    Undo.RecordObject(settings, "Reset UI Settings");
                    settings.backgroundImage = null;
                    settings.topBarRect = new Rect(0, 0, 2340, 135);
                    settings.leftPanelRect = new Rect(0, 135, 330, 945);
                    settings.rightPanelRect = new Rect(2190, 135, 150, 945);
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
            }
            GUI.color = Color.white;
        }

        private void DrawRectControls(string label, SerializedProperty rectProp)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            Rect r = rectProp.rectValue;

            EditorGUI.BeginChangeCheck();

            // 값 조작용 필드들
            int x = EditorGUILayout.IntField("  X Position", Mathf.RoundToInt(r.x));
            int y = EditorGUILayout.IntField("  Y Position", Mathf.RoundToInt(r.y));
            int w = EditorGUILayout.IntField("  Width", Mathf.RoundToInt(r.width));
            int h = EditorGUILayout.IntField("  Height", Mathf.RoundToInt(r.height));

            if (EditorGUI.EndChangeCheck())
            {
                x = Mathf.Clamp(x, -500, 2340);
                y = Mathf.Clamp(y, -500, 1080);
                w = Mathf.Max(10, w);
                h = Mathf.Max(10, h);
                rectProp.rectValue = new Rect(x, y, w, h);
            }
        }

        private void DrawVisualCanvas()
        {
            EditorGUILayout.LabelField("Live Interactive Layout Preview (Drag to Move / 2340x1080 Canvas)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("You can drag the colored UI panels below directly to position them.", MessageType.Info);
            EditorGUILayout.Space(10);

            // 가상 캔버스 크기 계산 (480 x 270)
            float canvasW = VIRTUAL_WIDTH * PREVIEW_SCALE;
            float canvasH = VIRTUAL_HEIGHT * PREVIEW_SCALE;

            // 캔버스 그릴 렉트 예약
            Rect canvasBoxRect = GUILayoutUtility.GetRect(canvasW, canvasH, GUILayout.Width(canvasW), GUILayout.Height(canvasH));

            // 배경 그리기 (검은 어두운 프레임)
            EditorGUI.DrawRect(canvasBoxRect, new Color(0.12f, 0.12f, 0.12f));

            // 배경 이미지가 존재하면 캔버스 배경에 축소 렌더링
            if (settings.backgroundImage != null)
            {
                GUI.DrawTexture(canvasBoxRect, settings.backgroundImage, ScaleMode.StretchToFill);
            }

            // 가이드 그리드 선 (센터 라인)
            EditorGUI.DrawRect(new Rect(canvasBoxRect.x + canvasW / 2f, canvasBoxRect.y, 1, canvasH), new Color(0.3f, 0.3f, 0.3f, 0.3f));
            EditorGUI.DrawRect(new Rect(canvasBoxRect.x, canvasBoxRect.y + canvasH / 2f, canvasW, 1), new Color(0.3f, 0.3f, 0.3f, 0.3f));

            // 각 UI 영역의 실제 렉트를 스케일링한 가상 렉트 계산
            Rect topBarPreview = ScaleRect(settings.topBarRect, canvasBoxRect.position);
            Rect leftPanelPreview = ScaleRect(settings.leftPanelRect, canvasBoxRect.position);
            Rect rightPanelPreview = ScaleRect(settings.rightPanelRect, canvasBoxRect.position);

            // 반투명 상자 그리기
            Color topColor = new Color(0.2f, 0.5f, 0.9f, 0.5f);
            Color leftColor = new Color(0.2f, 0.8f, 0.4f, 0.5f);
            Color rightColor = new Color(0.9f, 0.6f, 0.2f, 0.5f);

            EditorGUI.DrawRect(topBarPreview, topColor);
            EditorGUI.DrawRect(leftPanelPreview, leftColor);
            EditorGUI.DrawRect(rightPanelPreview, rightColor);

            // 박스 아웃라인 테두리 그리기
            DrawOutline(topBarPreview, new Color(0.2f, 0.5f, 0.9f, 0.9f));
            DrawOutline(leftPanelPreview, new Color(0.2f, 0.8f, 0.4f, 0.9f));
            DrawOutline(rightPanelPreview, new Color(0.9f, 0.6f, 0.2f, 0.9f));

            // 라벨 그리기 (텍스트 중앙 정렬)
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = Color.white;

            GUI.Label(topBarPreview, $"Top Info Bar\n({(int)settings.topBarRect.width}x{(int)settings.topBarRect.height})", labelStyle);
            GUI.Label(leftPanelPreview, $"Left Build UI\n({(int)settings.leftPanelRect.width}x{(int)settings.leftPanelRect.height})", labelStyle);
            GUI.Label(rightPanelPreview, $"Right Manage UI\n({(int)settings.rightPanelRect.width}x{(int)settings.rightPanelRect.height})", labelStyle);

            // 이벤트 처리 (드래그 앤 드롭 구현)
            HandleCanvasEvents(canvasBoxRect, topBarPreview, leftPanelPreview, rightPanelPreview);
        }

        private Rect ScaleRect(Rect source, Vector2 canvasOrigin)
        {
            return new Rect(
                canvasOrigin.x + source.x * PREVIEW_SCALE,
                canvasOrigin.y + source.y * PREVIEW_SCALE,
                source.width * PREVIEW_SCALE,
                source.height * PREVIEW_SCALE
            );
        }

        private void DrawOutline(Rect rect, Color color)
        {
            // 상단
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), color);
            // 하단
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), color);
            // 좌측
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), color);
            // 우측
            EditorGUI.DrawRect(new Rect(rect.xMax - 2, rect.y, 2, rect.height), color);
        }

        private void HandleCanvasEvents(Rect canvasRect, Rect topPreview, Rect leftPreview, Rect rightPreview)
        {
            Event evt = Event.current;
            Vector2 mousePos = evt.mousePosition;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (evt.button == 0) // 좌클릭
                    {
                        // Z-order 우선순위를 주어 클릭 대상을 검출 (오른쪽 패널 -> 왼쪽 패널 -> 상단 바 순)
                        if (rightPreview.Contains(mousePos))
                        {
                            activeDragArea = DraggedArea.RightPanel;
                            StartDrag(settings.rightPanelRect, mousePos);
                            evt.Use();
                        }
                        else if (leftPreview.Contains(mousePos))
                        {
                            activeDragArea = DraggedArea.LeftPanel;
                            StartDrag(settings.leftPanelRect, mousePos);
                            evt.Use();
                        }
                        else if (topPreview.Contains(mousePos))
                        {
                            activeDragArea = DraggedArea.TopBar;
                            StartDrag(settings.topBarRect, mousePos);
                            evt.Use();
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (activeDragArea != DraggedArea.None)
                    {
                        Vector2 delta = mousePos - dragStartMousePos;
                        
                        // 축소 배율에 따라 델타 스케일 복원
                        float realDeltaX = delta.x / PREVIEW_SCALE;
                        float realDeltaY = delta.y / PREVIEW_SCALE;

                        Rect targetRect = dragStartRect;
                        targetRect.x = Mathf.Round(dragStartRect.x + realDeltaX);
                        targetRect.y = Mathf.Round(dragStartRect.y + realDeltaY);

                        // 5픽셀 단위 그리드 스냅 연동
                        targetRect.x = Mathf.Round(targetRect.x / 5f) * 5f;
                        targetRect.y = Mathf.Round(targetRect.y / 5f) * 5f;

                        // 변경 값 기록 및 Dirty 세팅
                        Undo.RecordObject(settings, "Drag Gameplay UI Panels");
                        if (activeDragArea == DraggedArea.TopBar) settings.topBarRect = targetRect;
                        else if (activeDragArea == DraggedArea.LeftPanel) settings.leftPanelRect = targetRect;
                        else if (activeDragArea == DraggedArea.RightPanel) settings.rightPanelRect = targetRect;

                        EditorUtility.SetDirty(settings);
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (activeDragArea != DraggedArea.None)
                    {
                        activeDragArea = DraggedArea.None;
                        AssetDatabase.SaveAssets();
                        evt.Use();
                    }
                    break;
            }
        }

        private void StartDrag(Rect initialRect, Vector2 mousePos)
        {
            dragStartMousePos = mousePos;
            dragStartRect = initialRect;
        }

        private void LoadSettings()
        {
            settings = AssetDatabase.LoadAssetAtPath<GameplayUISettings>(SETTINGS_PATH);
            GameplayUISettings.ResetInstance();
        }

        private void CreateSettings()
        {
            string dir = Path.GetDirectoryName(SETTINGS_PATH);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            settings = ScriptableObject.CreateInstance<GameplayUISettings>();
            AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            GameplayUISettings.ResetInstance();
        }
    }
}
