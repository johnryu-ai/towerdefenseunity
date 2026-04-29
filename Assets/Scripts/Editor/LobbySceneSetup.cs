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
                
                CanvasScaler cs = cvObj.GetComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
            }

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

            lm.uiRoot = canvas.transform;

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
            string[] newSceneNames = { "Lobby_Main", "Lobby_Stage", "Lobby_Shop", "Lobby_Achievement", "Lobby_Leaderboard", "Lobby_Event" };
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
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene($"{sceneDir}/Lobby_Main.unity");
        }
    }
}
