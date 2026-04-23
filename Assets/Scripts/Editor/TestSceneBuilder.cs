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
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Debug.Log("테스트 씬 하이라키 구조가 성공적으로 생성되었습니다. GameManager에 맵 데이터를 할당하고 Play를 눌러주세요.");
        }
    }
}
