using UnityEngine;
using UnityEngine.UI;
using TDF.Core.Data;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace TDF.Runtime.Managers
{
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        [Header("UI Root")]
        public Transform uiRoot;
        
        [Header("Page Data")]
        public List<PageData> allPages;
        
        [Header("Scene to Load")]
        public string gameSceneName = "SampleScene";

        [Header("Campaign Data")]
        public CampaignData currentCampaign;

        private Dictionary<PageType, GameObject> pageInstances = new Dictionary<PageType, GameObject>();
        private PageType currentPage = PageType.Main;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // 런타임 동적 처리를 위해 UserDataManager가 필수이므로, 없다면 자동 생성합니다.
            if (UnityEngine.Object.FindAnyObjectByType<UserDataManager>() == null)
            {
                GameObject udmObj = new GameObject("UserDataManager_AutoSpawned");
                udmObj.AddComponent<UserDataManager>();
            }
        }

        private void Start()
        {
            // 만약 씬에 LobbyUIBinder가 존재하고 유효하다면, 구형 프리팹을 스폰하지 않고 바인더의 UI를 메인으로 삼습니다.
            var binder = UnityEngine.Object.FindAnyObjectByType<TDF.Runtime.UI.LobbyUIBinder>();
            if (binder != null && binder.uiData != null && binder.uiRoot != null)
            {
                pageInstances[PageType.Main] = binder.uiRoot.gameObject;
                currentPage = PageType.Main;
            }
            // 그 외의 경우 기존 방식대로 메인 페이지 열기
            else if (!pageInstances.ContainsKey(PageType.Main))
            {
                OpenPage(PageType.Main);
            }
        }

        public void RegisterMainPage(GameObject mainPageObj)
        {
            pageInstances[PageType.Main] = mainPageObj;
            currentPage = PageType.Main;
        }

        public void OpenPage(PageType type)
        {
            // 팝업이 아닌 경우 기존 페이지 숨기기
            PageData data = allPages.Find(p => p.pageType == type);
            if (data == null) return;

            if (!data.isPopup)
            {
                foreach (var kvp in pageInstances)
                {
                    if (kvp.Value != null) kvp.Value.SetActive(false);
                }
            }

            // 페이지 생성 또는 활성화
            if (!pageInstances.ContainsKey(type) || pageInstances[type] == null)
            {
                if (data.visualLayout != null)
                {
                    // Create an empty root for the generated UI
                    GameObject pageRoot = new GameObject($"{type}_GeneratedPage", typeof(RectTransform));
                    pageRoot.transform.SetParent(uiRoot, false);
                    RectTransform rt = pageRoot.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    // Attach Binder and Apply
                    var dynamicBinder = pageRoot.AddComponent<TDF.Runtime.UI.LobbyUIBinder>();
                    dynamicBinder.uiData = data.visualLayout;
                    dynamicBinder.uiRoot = rt;
                    dynamicBinder.ApplyUI();

                    pageInstances[type] = pageRoot;
                    // Note: SetupButtonEvents is not called here because LobbyUIBinder handles its own button clicks via ActionType
                }
                else if (data.uiPrefab != null)
                {
                    GameObject go = Instantiate(data.uiPrefab, uiRoot);
                    pageInstances[type] = go;
                    SetupButtonEvents(type, go);
                }
            }
            else
            {
                pageInstances[type].SetActive(true);
            }

            currentPage = type;
        }

        private void SetupButtonEvents(PageType type, GameObject pageObj)
        {
            // 메인 페이지 버튼 연결
            if (type == PageType.Main)
            {
                BindButton(pageObj, "STARTButton", () => StartGame());
                BindButton(pageObj, "STAGEButton", () => OpenPage(PageType.StageSelect));
                BindButton(pageObj, "SHOPButton", () => OpenPage(PageType.Shop));
                BindButton(pageObj, "ACHIEVEMENTButton", () => OpenPage(PageType.Achievement));
                BindButton(pageObj, "LEADERBOARDButton", () => OpenPage(PageType.Leaderboard));
                BindButton(pageObj, "EVENTButton", () => OpenPage(PageType.Event));
            }
            
            // 모든 페이지의 CloseButton 연결
            BindButton(pageObj, "CloseButton", () => CloseCurrentPage(pageObj));
        }

        private void BindButton(GameObject root, string btnName, UnityEngine.Events.UnityAction action)
        {
            // 자식들을 모두 검색하여 이름이 일치하는 버튼 찾기 (중첩 구조 대응)
            Button foundBtn = null;
            Button[] allButtons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                if (b.name == btnName)
                {
                    foundBtn = b;
                    break;
                }
            }

            if (foundBtn != null)
            {
                foundBtn.onClick.AddListener(action);
            }
        }

        public void CloseCurrentPage(GameObject pageObj)
        {
            pageObj.SetActive(false);
            // 메인으로 돌아가기 (팝업인 경우 굳이 안 해도 되지만 안전장치)
            if (currentPage != PageType.Main) OpenPage(PageType.Main);
        }

        public void StartGame()
        {
            int clearedCount = 0;
            if (UserDataManager.Instance != null && UserDataManager.Instance.Data.mapClearRecords != null)
            {
                clearedCount = UserDataManager.Instance.Data.mapClearRecords.Count;
            }
            StartGameAt(clearedCount);
        }

        public void StartGameAt(int stageIndex)
        {
#if UNITY_EDITOR
            if (currentCampaign == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CampaignData");
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    CampaignData camp = UnityEditor.AssetDatabase.LoadAssetAtPath<CampaignData>(path);
                    if (camp != null && camp.stages != null && camp.stages.Count > 0)
                    {
                        currentCampaign = camp;
                        break;
                    }
                }
            }
#endif
            if (currentCampaign != null)
            {
                TDF.Runtime.Managers.GameManager.staticTestCampaign = currentCampaign;
            }
            // staticTestCampaign이 null이면 GameManager의 인스펙터 참조를 그대로 사용하게 됨
            TDF.Runtime.Managers.GameManager.staticTestStageIndex = stageIndex;
            Debug.Log($"게임 씬으로 이동합니다: {gameSceneName} | Stage Index: {stageIndex}");
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
