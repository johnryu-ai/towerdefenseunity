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

        [Header("Campaign Data (Worlds)")]
        public List<CampaignData> worlds = new List<CampaignData>();

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

            // UGS 백엔드 매니저 자동 생성
            if (UnityEngine.Object.FindAnyObjectByType<BackendManager>() == null)
            {
                GameObject backendObj = new GameObject("BackendManager_AutoSpawned");
                backendObj.AddComponent<BackendManager>();
            }

            // UGS 경제 매니저 자동 생성
            if (UnityEngine.Object.FindAnyObjectByType<EconomyManager>() == null)
            {
                GameObject economyObj = new GameObject("EconomyManager_AutoSpawned");
                economyObj.AddComponent<EconomyManager>();
            }

#if UNITY_EDITOR
            // 에디터에서 실행 시 worlds 리스트 자동 채우기
            if (worlds == null || worlds.Count == 0)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CampaignData");
                worlds = new List<CampaignData>();
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    CampaignData camp = UnityEditor.AssetDatabase.LoadAssetAtPath<CampaignData>(path);
                    if (camp != null) worlds.Add(camp);
                }
                worlds.Sort((a,b) => a.name.CompareTo(b.name));
            }
#endif
        }

        private async void Start()
        {
            var binder = UnityEngine.Object.FindAnyObjectByType<TDF.Runtime.UI.LobbyUIBinder>();
            if (binder != null && binder.uiData != null && binder.uiRoot != null)
            {
                pageInstances[PageType.Main] = binder.uiRoot.gameObject;
                binder.uiRoot.gameObject.SetActive(false);
            }

            CreateSystemPages();

            if (BackendManager.Instance != null)
            {
                // 백엔드 매니저가 방금 생성되어 초기화 중일 수 있으므로 대기합니다.
                await BackendManager.Instance.WaitUntilInitializedAsync();
                
                // 이미 로그인된 상태(씬 전환으로 돌아온 경우)라면 바로 Main 로비로 갑니다.
                if (BackendManager.Instance.IsSignedIn)
                {
                    if (UserDataManager.Instance != null)
                    {
                        await UserDataManager.Instance.LoadFromCloudAsync();
                    }
                    OpenPage(PageType.Main);
                    return;
                }
            }
            
            // 처음 앱을 켰거나 로그인되어 있지 않다면 무조건 Login 화면부터 보여줍니다.
            OpenPage(PageType.Login);
        }

        private void CreateSystemPages()
        {
            // Login Page
            if (!pageInstances.ContainsKey(PageType.Login))
            {
                GameObject loginObj = new GameObject("LoginPage", typeof(RectTransform));
                loginObj.transform.SetParent(uiRoot, false);
                var rt = loginObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

                var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(loginObj.transform, false);
                var bgRt = bg.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
                bg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 1f);

                var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
                title.transform.SetParent(loginObj.transform, false);
                var titleRt = title.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0.5f, 0.7f); titleRt.anchorMax = new Vector2(0.5f, 0.7f);
                titleRt.sizeDelta = new Vector2(800, 200);
                titleRt.anchoredPosition = Vector2.zero;
                var titleTxt = title.GetComponent<Text>();
                titleTxt.text = "TOWER DEFENSE";
                titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                titleTxt.fontSize = 80;
                titleTxt.alignment = TextAnchor.MiddleCenter;
                titleTxt.color = Color.white;

                // Helper to create buttons
                GameObject CreateLoginBtn(string name, string text, float yOffset, Color col, UnityEngine.Events.UnityAction action)
                {
                    var btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                    btnObj.transform.SetParent(loginObj.transform, false);
                    var btnRt = btnObj.GetComponent<RectTransform>();
                    btnRt.anchorMin = new Vector2(0.5f, 0.5f); btnRt.anchorMax = new Vector2(0.5f, 0.5f);
                    btnRt.sizeDelta = new Vector2(500, 80);
                    btnRt.anchoredPosition = new Vector2(0, yOffset);
                    btnObj.GetComponent<Image>().color = col;
                    
                    var btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                    btnTxtObj.transform.SetParent(btnObj.transform, false);
                    var btnTxtRt = btnTxtObj.GetComponent<RectTransform>();
                    btnTxtRt.anchorMin = Vector2.zero; btnTxtRt.anchorMax = Vector2.one;
                    btnTxtRt.offsetMin = Vector2.zero; btnTxtRt.offsetMax = Vector2.zero;
                    var btnTxt = btnTxtObj.GetComponent<Text>();
                    btnTxt.text = text;
                    btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    btnTxt.fontSize = 35;
                    btnTxt.alignment = TextAnchor.MiddleCenter;
                    btnTxt.color = Color.white;

                    btnObj.GetComponent<Button>().onClick.AddListener(action);
                    return btnObj;
                }

                CreateLoginBtn("UnityAccountBtn", "Sign in with Unity / Google", 60f, new Color(0.1f, 0.6f, 0.4f, 1f), OnUnityPlayerAccountLoginClicked);
                CreateLoginBtn("GuestBtn", "Guest Login", -40f, new Color(0.4f, 0.4f, 0.4f, 1f), OnLoginClicked);

                // START GAME 버튼 추가 (하단에 크게)
                var startBtn = CreateLoginBtn("AutoStartBtn", "START GAME", -180f, new Color(0.8f, 0.2f, 0.2f, 1f), OnAutoStartClicked);
                startBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 100);
                startBtn.GetComponentInChildren<Text>().fontSize = 45;

                pageInstances[PageType.Login] = loginObj;
                loginObj.SetActive(false);
            }

            // Loading Page
            if (!pageInstances.ContainsKey(PageType.Loading))
            {
                GameObject loadingObj = new GameObject("LoadingPage", typeof(RectTransform));
                loadingObj.transform.SetParent(uiRoot, false);
                var rt = loadingObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

                var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(loadingObj.transform, false);
                var bgRt = bg.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
                bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);

                var txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                txtObj.transform.SetParent(loadingObj.transform, false);
                var txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = new Vector2(0.5f, 0.5f); txtRt.anchorMax = new Vector2(0.5f, 0.5f);
                txtRt.sizeDelta = new Vector2(600, 100);
                txtRt.anchoredPosition = Vector2.zero;
                var txt = txtObj.GetComponent<Text>();
                txt.text = "Loading Server Data...";
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 50;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;

                pageInstances[PageType.Loading] = loadingObj;
                loadingObj.SetActive(false);
            }
        }

        private async void OnLoginClicked()
        {
            OpenPage(PageType.Loading);
            
            if (BackendManager.Instance != null)
            {
                await BackendManager.Instance.SignInAnonymouslyAsync();
                
                if (UserDataManager.Instance != null && BackendManager.Instance.IsSignedIn)
                {
                    await UserDataManager.Instance.LoadFromCloudAsync();
                    OpenPage(PageType.Main);
                }
                else
                {
                    OpenPage(PageType.Login);
                }
            }
            else
            {
                OpenPage(PageType.Login);
            }
        }

        private async void OnUnityPlayerAccountLoginClicked()
        {
            Debug.Log("[LobbyManager] Unity Player Account Login Clicked.");
            OpenPage(PageType.Loading);
            
            if (BackendManager.Instance != null)
            {
                await BackendManager.Instance.SignInWithUnityPlayerAccountAsync();
                
                if (UserDataManager.Instance != null && BackendManager.Instance.IsSignedIn)
                {
                    await UserDataManager.Instance.LoadFromCloudAsync();
                    OpenPage(PageType.Main);
                }
                else
                {
                    OpenPage(PageType.Login);
                }
            }
            else
            {
                OpenPage(PageType.Login);
            }
        }

        private async void OnAutoStartClicked()
        {
            Debug.Log("[LobbyManager] Auto Start Game Clicked.");
            OpenPage(PageType.Loading);

            if (BackendManager.Instance != null)
            {
                bool isSigned = BackendManager.Instance.IsSignedIn;
                if (!isSigned)
                {
                    // 로컬 디바이스에 저장된 마지막 로그인 정보(토큰)를 이용해 자동 로그인을 시도합니다.
                    isSigned = await BackendManager.Instance.TrySignInCachedUserAsync();
                }

                if (isSigned)
                {
                    if (UserDataManager.Instance != null)
                    {
                        await UserDataManager.Instance.LoadFromCloudAsync();
                    }
                    OpenPage(PageType.Main);
                }
                else
                {
                    Debug.LogWarning("[LobbyManager] 이전에 로그인한 기록이 없거나 세션이 만료되었습니다.");
                    // 기록이 없으면 다시 로그인 페이지로 돌아갑니다. (또는 알림 팝업을 띄울 수도 있습니다)
                    OpenPage(PageType.Login);
                }
            }
            else
            {
                OpenPage(PageType.Login);
            }
        }

        public void RegisterMainPage(GameObject mainPageObj)
        {
            pageInstances[PageType.Main] = mainPageObj;
            currentPage = PageType.Main;
        }

        public void OpenPage(PageType type)
        {
            Debug.Log($"[LobbyManager] OpenPage requested: {type}");

            PageData data = allPages != null ? allPages.Find(p => p.pageType == type) : null;
            
            // 프리팹이나 레이아웃 에셋이 전혀 없는 상태인지 확인 (데이터는 있지만 알맹이가 없는 경우)
            bool hasNoContent = data == null || (data.visualLayout == null && data.uiPrefab == null);

            // 시스템 전용 페이지(동적 생성)이거나 이미 인스턴스가 존재하면 예외
            if (hasNoContent && type != PageType.Login && type != PageType.Loading && !pageInstances.ContainsKey(type))
            {
                // Fallback: 표시할 UI가 전혀 없으면 해당 씬(Lobby_XXX)으로 직접 전환을 시도합니다. (레거시/서브씬 호환)
                string targetScene = "Main";
                if (type == PageType.StageSelect) targetScene = "Lobby_Stage";
                else if (type == PageType.Shop) targetScene = "Lobby_Shop";
                else if (type == PageType.Achievement) targetScene = "Lobby_Achievement";
                else if (type == PageType.Leaderboard) targetScene = "Lobby_Leaderboard";
                else if (type == PageType.Event) targetScene = "Lobby_Event";

                Debug.Log($"[LobbyManager] No UI content for {type}. Fallback to scene: {targetScene}");

                if (targetScene != "Main")
                {
                    SceneManager.LoadScene(targetScene);
                }
                return;
            }

            // 모든 페이지 숨기기 (단, data.isPopup == true 인 경우는 예외)
            if (data == null || !data.isPopup)
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
                    // Custom 액션 타입 버튼들(로그아웃 등) 처리를 위해 SetupButtonEvents 호출
                    SetupButtonEvents(type, pageRoot);
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

            // 옵션 페이지 특수 버튼 연결
            if (type == PageType.GameOption)
            {
                BindButton(pageObj, "LogoutButton", () => 
                {
                    if (BackendManager.Instance != null)
                    {
                        BackendManager.Instance.SignOut();
                    }
                    OpenPage(PageType.Login);
                });

                BindButton(pageObj, "QuitButton", () => 
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            }
        }

        private void BindButton(GameObject root, string btnName, UnityEngine.Events.UnityAction action)
        {
            // 자식들을 모두 검색하여 이름이 일치하는 버튼 찾기 (중첩 구조 대응)
            Button foundBtn = null;
            Button[] allButtons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                if (b.name == btnName || b.name == $"Btn_{btnName}")
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
            int world = clearedCount / 10;
            int stage = clearedCount % 10;
            StartGameAt(world, stage);
        }

        public void StartGameAt(int worldIndex, int stageIndex)
        {
#if UNITY_EDITOR
            if (worlds == null || worlds.Count == 0)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CampaignData");
                worlds = new List<CampaignData>();
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    CampaignData camp = UnityEditor.AssetDatabase.LoadAssetAtPath<CampaignData>(path);
                    if (camp != null) worlds.Add(camp);
                }
                worlds.Sort((a,b) => a.name.CompareTo(b.name));
            }
#endif
            if (worldIndex >= 0 && worldIndex < worlds.Count)
            {
                TDF.Runtime.Managers.GameManager.staticTestCampaign = worlds[worldIndex];
                TDF.Runtime.Managers.GameManager.staticTestStageIndex = stageIndex;
                Debug.Log($"게임 씬으로 이동합니다: {gameSceneName} | World: {worldIndex+1}, Stage: {stageIndex+1}");
            }
            else
            {
                Debug.LogWarning($"[LobbyManager] Invalid worldIndex: {worldIndex}. Fallback to index 0.");
                if (worlds.Count > 0) TDF.Runtime.Managers.GameManager.staticTestCampaign = worlds[0];
                TDF.Runtime.Managers.GameManager.staticTestStageIndex = 0;
            }
            
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
