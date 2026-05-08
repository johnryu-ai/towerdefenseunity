using UnityEngine;

namespace TDF.Core.Data
{
    [CreateAssetMenu(fileName = "NewPageData", menuName = "TDF/Data/PageData")]
    public class PageData : ScriptableObject
    {
        [Header("Page Settings")]
        public string pageId;
        public PageType pageType;
        
        [Tooltip("이 화면에 사용될 실제 UI 프리팹 (Canvas 포함 또는 내부 패널)")]
        public GameObject uiPrefab;

        [Tooltip("비주얼 에디터로 구성한 UI 데이터 (있을 경우 uiPrefab보다 우선 적용됨)")]
        public LobbyMenuUIData visualLayout;

        [Tooltip("팝업 형태로 이전 화면 위에 덧씌워질지 여부")]
        public bool isPopup = false;
    }
}
