using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TDF.Core.Data;
using System.IO;

namespace TDF.Editor
{
    public class PageUIInitializer : EditorWindow
    {
        [MenuItem("Tools/TDF/Generate Default UI Prefabs")]
        public static void Generate()
        {
            string prefabPath = "Assets/Prefabs/UI";
            string dataPath = "Assets/Data/Pages";

            // 폴더 생성
            if (!Directory.Exists(Path.Combine(Application.dataPath, "Prefabs/UI"))) 
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs/UI"));
            if (!Directory.Exists(Path.Combine(Application.dataPath, "Data/Pages"))) 
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Pages"));

            foreach (PageType type in System.Enum.GetValues(typeof(PageType)))
            {
                string name = $"{type}Page";
                string fullPrefabPath = $"{prefabPath}/{name}.prefab";

                // 1. 프리팹 생성
                GameObject prefab = CreateBasicUIPrefab(name, type);
                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefab, fullPrefabPath);
                GameObject.DestroyImmediate(prefab);

                // 2. PageData 생성 및 할당
                string dataFilePath = $"{dataPath}/{type}Data.asset";
                PageData data = AssetDatabase.LoadAssetAtPath<PageData>(dataFilePath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<PageData>();
                    AssetDatabase.CreateAsset(data, dataFilePath);
                }

                data.pageId = type.ToString().ToLower();
                data.pageType = type;
                data.uiPrefab = savedPrefab;
                data.isPopup = (type == PageType.GameOption || type == PageType.Event);

                EditorUtility.SetDirty(data);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TDF UI Prefabs & PageData Generated Successfully!");
        }

        private static GameObject CreateBasicUIPrefab(string name, PageType type)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            // 배경 (Panel)
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root.transform);
            Image img = bg.GetComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            // 타이틀 (Title)
            GameObject title = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            title.transform.SetParent(root.transform);
            Text t = title.GetComponent<Text>();
            t.text = (type == PageType.Main) ? "TOWER DEFENSE FACTORY" : $"{type} PAGE";
            t.fontSize = 60;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.9f, 0.8f, 0.4f); // Golden color
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform tRt = title.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 1f);
            tRt.anchorMax = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0, -150);
            tRt.sizeDelta = new Vector2(1000, 120);

            // Main 페이지인 경우 메뉴 버튼들 생성
            if (type == PageType.Main)
            {
                CreateMainMenuButtons(root);
            }
            
            // 닫기 버튼 (팝업인 경우)
            if (type == PageType.GameOption || type == PageType.Event || type == PageType.Shop || type == PageType.Achievement || type == PageType.Leaderboard)
            {
                CreateCloseButton(root);
            }

            return root;
        }

        private static void CreateMainMenuButtons(GameObject root)
        {
            // 버튼들을 담을 패널 생성
            GameObject menuPanel = new GameObject("MenuPanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            menuPanel.transform.SetParent(root.transform);
            
            RectTransform panelRt = menuPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = new Vector2(0, -120); 
            panelRt.sizeDelta = new Vector2(500, 700);

            // Vertical Layout Group 설정 (강제 제어 활성화)
            VerticalLayoutGroup vlg = menuPanel.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 20; 
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = true; // 레이아웃 그룹이 높이 제어
            vlg.childControlWidth = true;  // 레이아웃 그룹이 너비 제어
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;

            // Content Size Fitter 설정
            ContentSizeFitter csf = menuPanel.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            string[] buttons = { "START", "STAGE", "SHOP", "ACHIEVEMENT", "LEADERBOARD", "EVENT" };

            for (int i = 0; i < buttons.Length; i++)
            {
                // LayoutElement를 추가하여 크기 정보 제공
                GameObject btnObj = new GameObject($"{buttons[i]}Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                btnObj.transform.SetParent(menuPanel.transform);
                
                // 버튼 크기 고정
                LayoutElement le = btnObj.GetComponent<LayoutElement>();
                le.preferredWidth = 450;
                le.preferredHeight = 90;
                le.minWidth = 450;
                le.minHeight = 90;

                Image img = btnObj.GetComponent<Image>();
                img.color = (buttons[i] == "START") ? new Color(0.15f, 0.45f, 0.15f) : new Color(0.25f, 0.25f, 0.35f);
                
                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                txtObj.transform.SetParent(btnObj.transform);
                Text t = txtObj.GetComponent<Text>();
                t.text = buttons[i];
                t.fontSize = 32;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                
                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;
            }
        }

        private static void CreateCloseButton(GameObject root)
        {
            GameObject closeBtn = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtn.transform.SetParent(root.transform);
            closeBtn.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 1f);
            RectTransform cbRt = closeBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1, 1);
            cbRt.anchorMax = new Vector2(1, 1);
            cbRt.anchoredPosition = new Vector2(-100, -100);
            cbRt.sizeDelta = new Vector2(80, 80);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(closeBtn.transform);
            Text t = txtObj.GetComponent<Text>();
            t.text = "X";
            t.fontSize = 40;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
        }
    }
}
