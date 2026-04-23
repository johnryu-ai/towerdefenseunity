using UnityEditor;
using UnityEngine;
using TDF.Runtime.Managers;
using TDF.Runtime.Map;
using TDF.Runtime.UI;

namespace TDF.Editor
{
    public class TestSceneBuilder
    {
        [MenuItem("Tools/TDF/Build Test Scene")]
        public static void CreateTestSceneSetup()
        {
            // 빈 게임오브젝트를 활용해 씬 구조 자동 생성

            // 0. 메인 카메라 생성 (빈 씬일 경우 대비)
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                
                // 세로 해상도가 1080일 때 타일 1개가 135px이 되려면: 1080 / 135 = 8칸.
                // orthographicSize는 세로 길이의 절반이므로 4로 설정.
                cam.orthographicSize = 4f; 
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.2f, 0.2f, 0.2f); // 회색 여백
                
                // 카메라를 화면 약간 아래로 내려서 (UI가 135px 차지) 맵을 정중앙에 맞출 수도 있지만, 
                // 일단 정중앙 (0,0)을 바라보게 합니다.
                camObj.transform.position = new Vector3(0, 0, -10);
            }
            
            // 1. 코어 매니저 컨테이너
            GameObject coreObj = new GameObject("[Core Managers]");
            coreObj.AddComponent<GameManager>();
            coreObj.AddComponent<ObjectPoolManager>();
            coreObj.AddComponent<WaveManager>();
            coreObj.AddComponent<BuildManager>();

            // 2. 맵 환경
            GameObject mapObj = new GameObject("[Map Environment]");
            mapObj.AddComponent<MapController>();

            // 3. UI 캔버스 (간단 구조)
            GameObject canvasObj = new GameObject("[Canvas - InGameUI]");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvasObj.AddComponent<InGameUIManager>();

            // 4. 이벤트 시스템 (UI용)
            var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            }

            // 만약 구형 StandaloneInputModule이 있다면 삭제하고 새 모듈로 교체
            var oldModule = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null)
            {
                Object.DestroyImmediate(oldModule);
            }

            var newModule = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (newModule == null)
            {
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            Debug.Log("테스트 씬 하이라키 구조가 성공적으로 생성되었습니다. GameManager에 맵 데이터를 할당하고 Play를 눌러주세요.");
        }
    }
}
