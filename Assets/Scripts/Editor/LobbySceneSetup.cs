using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TDF.Core.Data;
using TDF.Runtime.Managers;
using System.Collections.Generic;

namespace TDF.Editor
{
    public class LobbySceneSetup : EditorWindow
    {
        [MenuItem("Tools/TDF/Setup Lobby Scene (Current)")]
        public static void Setup()
        {
            // 0. 카메라 확인 및 생성
            if (Camera.main == null && GameObject.FindAnyObjectByType<Camera>() == null)
            {
                GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camObj.tag = "MainCamera";
                camObj.transform.position = new Vector3(0, 0, -10);
                Camera cam = camObj.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f); // 어두운 남색 배경
            }

            // 1. Canvas 및 EventSystem 구성
            Canvas canvas = GameObject.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject cvObj = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = cvObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            CanvasScaler cs = canvas.GetComponent<CanvasScaler>();
            if (cs == null) cs = canvas.gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(2340, 1080);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 1f; // 상하에 꽉 차게 조율 (Height 매칭)

            // 전체 화면을 검은색으로 채울 Background 생성
            Transform blackBgTrans = canvas.transform.Find("BlackBackground");
            if (blackBgTrans == null)
            {
                GameObject bgObj = new GameObject("BlackBackground", typeof(RectTransform), typeof(Image));
                bgObj.transform.SetParent(canvas.transform, false);
                bgObj.transform.SetAsFirstSibling(); // 가장 뒤에 위치
                RectTransform bgRt = bgObj.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                bgObj.GetComponent<Image>().color = Color.black;
            }

            // 19.5:9 비율을 유지할 AspectContainer 생성
            Transform aspectTrans = canvas.transform.Find("AspectContainer");
            GameObject aspectObj;
            if (aspectTrans == null)
            {
                aspectObj = new GameObject("AspectContainer", typeof(RectTransform), typeof(AspectRatioFitter));
                aspectObj.transform.SetParent(canvas.transform, false);
                
                // BlackBackground 보다는 앞에 위치
                if (canvas.transform.Find("BlackBackground") != null)
                {
                    aspectObj.transform.SetSiblingIndex(1);
                }
            }
            else
            {
                aspectObj = aspectTrans.gameObject;
            }

            RectTransform aspectRt = aspectObj.GetComponent<RectTransform>();
            aspectRt.anchorMin = Vector2.zero;
            aspectRt.anchorMax = Vector2.one;
            aspectRt.offsetMin = Vector2.zero;
            aspectRt.offsetMax = Vector2.zero;

            AspectRatioFitter arf = aspectObj.GetComponent<AspectRatioFitter>();
            if (arf == null) arf = aspectObj.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = 1920f / 1080f; // 기존 UI 규격에 맞추어 16:9 비율 유지 (19.5:9 화면에서 하단 잘림 방지)

            // EventSystem 및 Input 모듈 체크/교체
            UnityEngine.EventSystems.EventSystem es = GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            else
            {
                // 기존에 구형 StandaloneInputModule이 있다면 제거
                var oldModule = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (oldModule != null)
                {
                    GameObject.DestroyImmediate(oldModule);
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    Debug.Log("Old StandaloneInputModule replaced with InputSystemUIInputModule.");
                }
            }

            // 2. LobbyManager 생성
            LobbyManager lm = GameObject.FindAnyObjectByType<LobbyManager>();
            if (lm == null)
            {
                GameObject lmObj = new GameObject("LobbyManager", typeof(LobbyManager));
                lm = lmObj.GetComponent<LobbyManager>();
            }

            lm.uiRoot = aspectObj.transform;

            // 3. 모든 PageData 찾아서 할당
            string[] guids = AssetDatabase.FindAssets("t:PageData");
            lm.allPages = new List<PageData>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PageData data = AssetDatabase.LoadAssetAtPath<PageData>(path);
                if (data != null) lm.allPages.Add(data);
            }

            EditorUtility.SetDirty(lm);
            Debug.Log("Lobby Scene Setup Complete!");
        }

        [MenuItem("Tools/TDF/Cleanup UGUI Lobby (Use IMGUI)")]
        public static void CleanupUGUI()
        {
            var canvas = GameObject.Find("LobbyCanvas");
            if (canvas != null) GameObject.DestroyImmediate(canvas);

            var manager = GameObject.Find("LobbyManager");
            if (manager != null) GameObject.DestroyImmediate(manager);

            var eventSystem = GameObject.Find("EventSystem");
            if (eventSystem != null) GameObject.DestroyImmediate(eventSystem);

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("UGUI Lobby elements removed. IMGUI LobbyUIManager will now be visible.");
        }

        [MenuItem("Tools/TDF/Fix Build Settings (Add Scenes)")]
        public static void FixBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Lobby.unity", true));
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true));
            
            // 다른 씬들이 있으면 추가
            if (System.IO.File.Exists("Assets/Scenes/gameplay.unity"))
                scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/gameplay.unity", true));
            if (System.IO.File.Exists("Assets/Scenes/main.unity"))
                scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/main.unity", true));

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Scenes have been successfully added to Build Settings!");
        }

        [MenuItem("Tools/TDF/Split Lobby Into Separate Scenes")]
        public static void SplitLobbyIntoScenes()
        {
            string[] newSceneNames = { "Main", "Lobby_Stage", "Lobby_Shop", "Lobby_Achievement", "Lobby_Leaderboard", "Lobby_Event" };
            string sceneDir = "Assets/Scenes";
            
            // 현재 활성화된 씬 저장
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            
            // 기존 빌드 세팅 가져오기
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // 로비 씬 찾기
            string originalLobbyPath = $"{sceneDir}/Lobby.unity";
            if (!System.IO.File.Exists(originalLobbyPath))
            {
                Debug.LogError("Lobby.unity 씬을 찾을 수 없습니다. 현재 씬을 Lobby로 저장해주세요.");
                return;
            }

            foreach (var name in newSceneNames)
            {
                string newPath = $"{sceneDir}/{name}.unity";
                
                // 씬 복사
                if (AssetDatabase.CopyAsset(originalLobbyPath, newPath))
                {
                    Debug.Log($"씬 복사 성공: {newPath}");
                    
                    // 빌드 세팅에 없으면 추가
                    bool exists = false;
                    foreach (var s in buildScenes)
                    {
                        if (s.path == newPath) exists = true;
                    }
                    if (!exists)
                    {
                        buildScenes.Add(new EditorBuildSettingsScene(newPath, true));
                    }
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.Refresh();
            Debug.Log("로비 하위 메뉴들이 개별 씬으로 성공적으로 분리 및 Build Settings에 등록되었습니다!");
            
            // 메인 로비 씬 열기
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene($"{sceneDir}/Main.unity");
        }

        [MenuItem("Tools/TDF/Apply Lobby UI Data to Current Scene")]
        public static void ApplyLobbyUIData()
        {
            // 1. Canvas 확인
            Canvas canvas = GameObject.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("현재 씬에 Canvas가 없습니다. Tools/TDF/Setup Lobby Scene (Current) 를 먼저 실행해주세요.");
                return;
            }

            // 2. LobbyUI_Root 찾기 또는 생성
            Transform aspectTrans = canvas.transform.Find("AspectContainer");
            Transform parentTrans = aspectTrans != null ? aspectTrans : canvas.transform;

            Transform rootTransform = parentTrans.Find("LobbyUI_Root");
            GameObject rootObj;
            if (rootTransform == null)
            {
                rootObj = new GameObject("LobbyUI_Root", typeof(RectTransform));
                rootObj.transform.SetParent(parentTrans, false);
                
                RectTransform rt = rootObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                rootObj = rootTransform.gameObject;
            }

            // 3. LobbyUIBinder 확인 및 추가
            TDF.Runtime.UI.LobbyUIBinder binder = rootObj.GetComponent<TDF.Runtime.UI.LobbyUIBinder>();
            if (binder == null)
            {
                binder = rootObj.AddComponent<TDF.Runtime.UI.LobbyUIBinder>();
            }

            // 4. LobbyMenuUIData 찾아서 할당
            if (binder.uiData == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:LobbyMenuUIData");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    binder.uiData = AssetDatabase.LoadAssetAtPath<LobbyMenuUIData>(path);
                    Debug.Log($"LobbyMenuUIData 자동 할당됨: {path}");
                }
                else
                {
                    Debug.LogWarning("프로젝트에 LobbyMenuUIData 파일이 없습니다. Lobby UI Editor에서 먼저 생성해주세요.");
                }
            }

            binder.uiRoot = rootObj.GetComponent<RectTransform>();

            // 저장 및 갱신
            EditorUtility.SetDirty(binder);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("Lobby UI Binder가 성공적으로 세팅되었습니다. 플레이(Play)를 누르면 UI가 자동 생성됩니다.");
        }

        [MenuItem("Tools/TDF/Create New Main Scene")]
        public static void CreateNewMainScene()
        {
            string scenePath = "Assets/Scenes/Main.unity";
            
            // 기존 씬 저장
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            
            // 새 씬 생성
            var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            
            // 1. 카메라 세팅
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0, 0, -10);
            Camera cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);

            // 2. Canvas 세팅
            GameObject cvObj = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = cvObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = cvObj.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(2340, 1080);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 1f; // 상하에 꽉 차게 조율 (Height 매칭)

            // 전체 화면을 검은색으로 채울 Background 생성
            GameObject bgObj = new GameObject("BlackBackground", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvas.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bgObj.GetComponent<Image>().color = Color.black;

            // 19.5:9 비율을 유지할 AspectContainer 생성
            GameObject aspectObj = new GameObject("AspectContainer", typeof(RectTransform), typeof(AspectRatioFitter));
            aspectObj.transform.SetParent(canvas.transform, false);
            RectTransform aspectRt = aspectObj.GetComponent<RectTransform>();
            aspectRt.anchorMin = Vector2.zero;
            aspectRt.anchorMax = Vector2.one;
            aspectRt.offsetMin = Vector2.zero;
            aspectRt.offsetMax = Vector2.zero;
            AspectRatioFitter arf = aspectObj.GetComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = 1920f / 1080f; // 기존 UI 규격에 맞추어 16:9 비율 유지 (19.5:9 화면에서 하단 잘림 방지)

            // 3. EventSystem 세팅
            GameObject esObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // 4. LobbyManager 및 UserDataManager 세팅
            GameObject mgrObj = new GameObject("Managers");
            
            LobbyManager lm = mgrObj.AddComponent<LobbyManager>();
            lm.uiRoot = aspectObj.transform;

            if (mgrObj.GetComponent<UserDataManager>() == null)
            {
                mgrObj.AddComponent<UserDataManager>();
            }

            string[] pageGuids = AssetDatabase.FindAssets("t:PageData");
            lm.allPages = new List<PageData>();
            foreach (string guid in pageGuids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                PageData data = AssetDatabase.LoadAssetAtPath<PageData>(p);
                if (data != null) lm.allPages.Add(data);
            }

            // 5. LobbyUIBinder 세팅 (여기에 새 UI 연동)
            GameObject rootObj = new GameObject("LobbyUI_Root", typeof(RectTransform));
            rootObj.transform.SetParent(aspectObj.transform, false);
            RectTransform rt = rootObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            TDF.Runtime.UI.LobbyUIBinder binder = rootObj.AddComponent<TDF.Runtime.UI.LobbyUIBinder>();
            binder.uiRoot = rt;

            string[] uiDataGuids = AssetDatabase.FindAssets("t:LobbyMenuUIData");
            if (uiDataGuids.Length > 0)
            {
                binder.uiData = AssetDatabase.LoadAssetAtPath<LobbyMenuUIData>(AssetDatabase.GUIDToAssetPath(uiDataGuids[0]));
            }
            else
            {
                Debug.LogWarning("LobbyMenuUIData가 없습니다. Lobby UI Editor에서 먼저 생성해주세요.");
            }

            // 씬 저장
            if (!System.IO.Directory.Exists("Assets/Scenes"))
            {
                System.IO.Directory.CreateDirectory("Assets/Scenes");
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);

            // Build Settings에 등록
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool exists = false;
            foreach (var s in buildScenes)
            {
                if (s.path == scenePath) exists = true;
            }
            if (!exists)
            {
                buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true)); // 메인 씬이므로 맨 앞에
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            Debug.Log($"새로운 로비 씬 '{scenePath}'이(가) 완벽하게 구성되었습니다!");
        }
    }
}
