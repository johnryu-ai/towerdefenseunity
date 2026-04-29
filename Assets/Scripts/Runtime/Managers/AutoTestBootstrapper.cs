using UnityEngine;
using UnityEngine.SceneManagement;
using TDF.Runtime.Map;
using TDF.Runtime.UI;

namespace TDF.Runtime.Managers
{
    /// <summary>
    /// SampleScene을 열고 바로 Play 버튼을 눌렀을 때, 
    /// 씬에 GameManager 등이 없다면 자동으로 런타임에 셋업해주는 부트스트래퍼입니다.
    /// </summary>
    public class AutoTestBootstrapper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            // 게임이 시작될 때 씬 로드 이벤트를 구독하여, 
            // 로비에서 넘어오든 직접 플레이하든 상관없이 빈 씬을 감지하도록 합니다.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string sceneName = scene.name;
            // 샘플씬이나 게임플레이 씬에서만 동작
            if (sceneName == "SampleScene" || sceneName == "gameplay")
            {
                // 이미 GameManager가 있다면 무시 (사용자가 직접 툴로 세팅한 경우)
                if (Object.FindAnyObjectByType<GameManager>() == null)
                {
                    Debug.Log($"[AutoTestBootstrapper] 빈 씬({sceneName}) 감지. 인게임 환경을 자동 생성합니다.");
                    CreateTestEnvironment();
                }
            }
        }

        static void CreateTestEnvironment()
        {
            // 1. 카메라 세팅
            if (Camera.main == null && Object.FindAnyObjectByType<Camera>() == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 4f; 
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.2f, 0.2f, 0.2f); 
                camObj.transform.position = new Vector3(0, 0, -10);
            }
            
            // 2. 핵심 매니저 생성
            GameObject coreObj = new GameObject("[Core Managers (Auto)]");
            GameManager gm = coreObj.AddComponent<GameManager>();
            coreObj.AddComponent<ObjectPoolManager>();
            coreObj.AddComponent<WaveManager>();
            coreObj.AddComponent<BuildManager>();

            // 3. 맵 컨트롤러
            GameObject mapObj = new GameObject("[Map Environment (Auto)]");
            mapObj.AddComponent<MapController>();

            // 4. UI 캔버스
            GameObject canvasObj = new GameObject("[Canvas - InGameUI (Auto)]");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvasObj.AddComponent<InGameUIManager>();

            // 5. 이벤트 시스템 (InputSystem 지원)
            var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem (Auto)");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 6. [에디터 전용] 테스트를 위한 임시 맵/캠페인 데이터 자동 주입
#if UNITY_EDITOR
            if (GameManager.staticTestCampaign == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CampaignData");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    var campaign = UnityEditor.AssetDatabase.LoadAssetAtPath<TDF.Core.Data.CampaignData>(path);
                    if (campaign != null)
                    {
                        gm.currentCampaign = campaign;
                        gm.currentStageIndex = 0;
                        Debug.Log($"[AutoTestBootstrapper] 에디터 테스트용으로 Campaign Data({campaign.name})를 자동 할당했습니다.");
                    }
                }
                else
                {
                    // 캠페인이 없을 경우 단일 MapData라도 찾아서 할당 시도
                    string[] mapGuids = UnityEditor.AssetDatabase.FindAssets("t:MapData");
                    if (mapGuids.Length > 0)
                    {
                        string mapPath = UnityEditor.AssetDatabase.GUIDToAssetPath(mapGuids[0]);
                        var map = UnityEditor.AssetDatabase.LoadAssetAtPath<TDF.Core.Data.MapData>(mapPath);
                        gm.currentMapData = map;
                        Debug.Log($"[AutoTestBootstrapper] Campaign이 없어 Map Data({map.name})를 강제 할당했습니다.");
                    }
                }
            }
#endif
        }
    }
}
