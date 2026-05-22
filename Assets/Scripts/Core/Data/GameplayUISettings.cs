using UnityEngine;

namespace TDF.Core.Data
{
    [CreateAssetMenu(fileName = "GameplayUISettings", menuName = "TDF/Gameplay UI Settings")]
    public class GameplayUISettings : ScriptableObject
    {
        private static GameplayUISettings instance;
        public static GameplayUISettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameplayUISettings>("Settings/GameplayUISettings");
                    if (instance == null)
                    {
                        // 에셋이 없거나 에디터 밖 런타임인 경우의 예외 처리를 위해 기본 임시 인스턴스 생성
                        instance = CreateInstance<GameplayUISettings>();
                    }
                }
                return instance;
            }
        }

        [Header("Background Config")]
        [Tooltip("인게임 전체 배경으로 출력할 이미지입니다.")]
        public Texture2D backgroundImage;

        [Header("UI Rect Config (2340x1080 Native System)")]
        [Tooltip("상단 골드, 웨이브, 기지 생명력 등의 정보가 표시되는 상단 바의 위치 및 영역 크기입니다. (기본값: X=0, Y=0, W=2340, H=135)")]
        public Rect topBarRect = new Rect(0, 0, 2340, 135);

        [Tooltip("타워 건설 버튼들이 표시되는 왼쪽 패널의 위치 및 영역 크기입니다. (기본값: X=0, Y=135, W=150, H=945)")]
        public Rect leftPanelRect = new Rect(0, 135, 150, 945);

        [Tooltip("건설된 타워의 업그레이드, 판매, 타겟 설정을 조작하는 오른쪽 패널의 위치 및 영역 크기입니다. (기본값: X=2190, Y=135, W=150, H=945)")]
        public Rect rightPanelRect = new Rect(2190, 135, 150, 945);

        #if UNITY_EDITOR
        public static void ResetInstance()
        {
            instance = null;
        }
        #endif
    }
}
