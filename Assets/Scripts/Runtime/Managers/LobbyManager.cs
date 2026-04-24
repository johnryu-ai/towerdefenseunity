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

        private Dictionary<PageType, GameObject> pageInstances = new Dictionary<PageType, GameObject>();
        private PageType currentPage = PageType.Main;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // 처음에 메인 페이지 열기
            OpenPage(PageType.Main);
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
                if (data.uiPrefab != null)
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
            Debug.Log("게임 씬으로 이동합니다: " + gameSceneName);
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
