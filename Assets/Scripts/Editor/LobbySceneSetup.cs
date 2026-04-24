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
    }
}
