using UnityEditor;
using UnityEngine;
using TDF.Core.Data;
using System.Collections.Generic;

namespace TDF.Editor.Modules
{
    public class LobbyUIEditorModule
    {
        private LobbyMenuUIData currentData;
        private Vector2 leftScrollPos;
        
        // Interaction state
        private int activeElementIndex = -1; // -1: None, -2: Banner, >=0: Button Index
        private bool isDragging = false;
        private bool isResizing = false;
        private Vector2 dragOffset;

        private const float VIRTUAL_WIDTH = 1920f;
        private const float VIRTUAL_HEIGHT = 1080f;

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            DrawInspectorPanel();
            DrawVisualCanvas();
            GUILayout.EndHorizontal();
        }

        private void DrawInspectorPanel()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(350), GUILayout.ExpandHeight(true));
            
            GUILayout.Label("Lobby UI Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            currentData = (LobbyMenuUIData)EditorGUILayout.ObjectField("UI Data Asset", currentData, typeof(LobbyMenuUIData), false);
            
            if (currentData == null)
            {
                if (GUILayout.Button("Create New Lobby UI Data", GUILayout.Height(30)))
                {
                    CreateNewData();
                }
                GUILayout.EndVertical();
                return;
            }

            leftScrollPos = GUILayout.BeginScrollView(leftScrollPos);
            
            GUILayout.Space(10);
            GUILayout.Label("Global Graphics", EditorStyles.boldLabel);
            currentData.backgroundImage = (Sprite)EditorGUILayout.ObjectField("Background", currentData.backgroundImage, typeof(Sprite), false);
            currentData.topBannerImage = (Sprite)EditorGUILayout.ObjectField("Top Banner", currentData.topBannerImage, typeof(Sprite), false);
            currentData.topBannerRect = EditorGUILayout.RectField("Banner Rect", currentData.topBannerRect);

            GUILayout.Space(15);
            GUILayout.Label("Buttons", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add New Button"))
            {
                currentData.buttons.Add(new LobbyButtonData());
                GUI.FocusControl(null);
            }

            for (int i = 0; i < currentData.buttons.Count; i++)
            {
                GUILayout.BeginVertical("helpbox");
                var btn = currentData.buttons[i];
                
                GUILayout.BeginHorizontal();
                btn.buttonName = EditorGUILayout.TextField(btn.buttonName);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    currentData.buttons.RemoveAt(i);
                    i--;
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    continue;
                }
                GUILayout.EndHorizontal();

                btn.buttonText = EditorGUILayout.TextField("Text", btn.buttonText);
                btn.actionType = (ButtonActionType)EditorGUILayout.EnumPopup("Action Type", btn.actionType);
                if (btn.actionType == ButtonActionType.OpenPage)
                {
                    btn.targetPageAsset = (PageData)EditorGUILayout.ObjectField("Target Page Asset", btn.targetPageAsset, typeof(PageData), false);
                }
                btn.buttonImage = (Sprite)EditorGUILayout.ObjectField("Image", btn.buttonImage, typeof(Sprite), false);
                btn.buttonRect = EditorGUILayout.RectField("Rect", btn.buttonRect);
                
                GUILayout.Label("Font Settings", EditorStyles.miniBoldLabel);
                btn.textColor = EditorGUILayout.ColorField("Color", btn.textColor);
                btn.fontSize = EditorGUILayout.IntField("Size", btn.fontSize);
                btn.fontStyle = (FontStyle)EditorGUILayout.EnumPopup("Style", btn.fontStyle);
                btn.fontAsset = (Font)EditorGUILayout.ObjectField("Font Asset", btn.fontAsset, typeof(Font), false);

                GUILayout.EndVertical();
                GUILayout.Space(5);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(currentData);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private Rect ScaleRect(Rect r, float scale)
        {
            return new Rect(r.x * scale, r.y * scale, r.width * scale, r.height * scale);
        }

        private void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;
            Rect r = sprite.textureRect;
            Texture2D tex = sprite.texture;
            Rect uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
        }

        private void DrawVisualCanvas()
        {
            Rect areaRect = GUILayoutUtility.GetRect(400, 400, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.Box(areaRect, "", "window"); // Border

            if (currentData == null)
            {
                GUI.Label(new Rect(areaRect.center.x - 100, areaRect.center.y - 10, 200, 20), "Please select or create UI Data.");
                return;
            }

            // Calculate 16:9 scale
            float scale = Mathf.Min(areaRect.width / VIRTUAL_WIDTH, areaRect.height / VIRTUAL_HEIGHT);
            Vector2 scaledSize = new Vector2(VIRTUAL_WIDTH * scale, VIRTUAL_HEIGHT * scale);
            Rect canvasRect = new Rect(areaRect.x + (areaRect.width - scaledSize.x) / 2f, 
                                       areaRect.y + (areaRect.height - scaledSize.y) / 2f, 
                                       scaledSize.x, scaledSize.y);

            // Draw Canvas Background (Letterbox area)
            EditorGUI.DrawRect(canvasRect, new Color(0.1f, 0.1f, 0.1f));

            GUI.BeginGroup(canvasRect);

            // 1. Draw Background
            if (currentData.backgroundImage != null)
            {
                DrawSprite(new Rect(0, 0, scaledSize.x, scaledSize.y), currentData.backgroundImage);
            }

            // Handle Interactions
            Event e = Event.current;
            // Mouse position is relative to BeginGroup, we scale it to virtual 1920x1080 space
            Vector2 mouseVirtualPos = e.mousePosition / scale;

            HandleInteraction(e, mouseVirtualPos);

            // 2. Draw Top Banner
            Rect scaledBannerRect = ScaleRect(currentData.topBannerRect, scale);
            if (currentData.topBannerImage != null)
            {
                DrawSprite(scaledBannerRect, currentData.topBannerImage);
            }
            else
            {
                GUI.Box(scaledBannerRect, "Top Banner Area");
            }

            if (activeElementIndex == -2)
            {
                DrawSelectionBox(scaledBannerRect);
            }

            // 3. Draw Buttons
            for (int i = 0; i < currentData.buttons.Count; i++)
            {
                var btn = currentData.buttons[i];
                Rect scaledBtnRect = ScaleRect(btn.buttonRect, scale);
                
                if (btn.buttonImage != null)
                {
                    DrawSprite(scaledBtnRect, btn.buttonImage);
                }
                else
                {
                    GUI.Box(scaledBtnRect, "");
                }

                if (!string.IsNullOrEmpty(btn.buttonText))
                {
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.alignment = TextAnchor.MiddleCenter;
                    style.normal.textColor = btn.textColor;
                    style.fontSize = Mathf.RoundToInt(btn.fontSize * scale);
                    style.fontStyle = btn.fontStyle;
                    if (btn.fontAsset != null) style.font = btn.fontAsset;
                    
                    GUI.Label(scaledBtnRect, btn.buttonText, style);
                }

                if (activeElementIndex == i)
                {
                    DrawSelectionBox(scaledBtnRect);
                }
            }

            GUI.EndGroup();
        }

        private void DrawSelectionBox(Rect scaledRect)
        {
            // Draw Outline
            Handles.color = Color.green;
            Handles.DrawWireCube(scaledRect.center, scaledRect.size);
            
            // Draw Resize Handle (Bottom-Right)
            Rect handleRect = GetScaledResizeHandleRect(scaledRect);
            EditorGUI.DrawRect(handleRect, Color.green);
        }

        private Rect GetScaledResizeHandleRect(Rect scaledElementRect)
        {
            float size = 15f; // Fixed size in screen space, not scaled
            return new Rect(scaledElementRect.xMax - size, scaledElementRect.yMax - size, size, size);
        }

        private Rect GetVirtualResizeHandleRect(Rect elementRect)
        {
            float size = 30f; // Virtual size
            return new Rect(elementRect.xMax - size, elementRect.yMax - size, size, size);
        }

        private void HandleInteraction(Event e, Vector2 mouseVirtualPos)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                bool handled = false;
                
                // 1. Check if clicking on an active resize handle
                if (activeElementIndex != -1)
                {
                    Rect activeRect = activeElementIndex == -2 ? currentData.topBannerRect : currentData.buttons[activeElementIndex].buttonRect;
                    if (GetVirtualResizeHandleRect(activeRect).Contains(mouseVirtualPos))
                    {
                        isResizing = true;
                        handled = true;
                        e.Use();
                    }
                }

                // 2. Check for selecting/dragging elements
                if (!handled)
                {
                    activeElementIndex = -1; // Reset selection
                    
                    // Check buttons (reverse order)
                    for (int i = currentData.buttons.Count - 1; i >= 0; i--)
                    {
                        if (currentData.buttons[i].buttonRect.Contains(mouseVirtualPos))
                        {
                            activeElementIndex = i;
                            isDragging = true;
                            dragOffset = mouseVirtualPos - new Vector2(currentData.buttons[i].buttonRect.x, currentData.buttons[i].buttonRect.y);
                            handled = true;
                            e.Use();
                            break;
                        }
                    }

                    // Check banner
                    if (!handled && currentData.topBannerRect.Contains(mouseVirtualPos))
                    {
                        activeElementIndex = -2;
                        isDragging = true;
                        dragOffset = mouseVirtualPos - new Vector2(currentData.topBannerRect.x, currentData.topBannerRect.y);
                        e.Use();
                    }
                }
            }
            else if (e.type == EventType.MouseDrag && e.button == 0)
            {
                if (activeElementIndex != -1)
                {
                    Rect r = activeElementIndex == -2 ? currentData.topBannerRect : currentData.buttons[activeElementIndex].buttonRect;
                    
                    if (isResizing)
                    {
                        // Resize by modifying width/height based on mouse delta from original rect position
                        r.width = Mathf.Max(50f, mouseVirtualPos.x - r.x);
                        r.height = Mathf.Max(20f, mouseVirtualPos.y - r.y);
                    }
                    else if (isDragging)
                    {
                        r.x = mouseVirtualPos.x - dragOffset.x;
                        r.y = mouseVirtualPos.y - dragOffset.y;
                    }

                    if (activeElementIndex == -2) currentData.topBannerRect = r;
                    else currentData.buttons[activeElementIndex].buttonRect = r;
                    
                    EditorUtility.SetDirty(currentData);
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDragging = false;
                isResizing = false;
            }
        }

        private void CreateNewData()
        {
            LobbyMenuUIData newData = ScriptableObject.CreateInstance<LobbyMenuUIData>();
            string folderPath = "Assets/Data/Pages";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
                AssetDatabase.CreateFolder("Assets/Data", "Pages");
            }
            
            string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/NewLobbyMenuUIData.asset");
            AssetDatabase.CreateAsset(newData, fullPath);
            AssetDatabase.SaveAssets();
            currentData = newData;
            Selection.activeObject = newData;
        }
    }
}
