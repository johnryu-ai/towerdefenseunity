using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TDF.Core.Data;
using TDF.Runtime.Managers;

namespace TDF.Runtime.UI
{
    public enum LobbyScreen { Main, StageGroups, StageDetail, Shop, Achievement, Leaderboard, Event }

    public class LobbyUIManager : MonoBehaviour
    {
        [Header("Scene")]
        public string gameSceneName = "SampleScene";

        [Header("Worlds (순서대로 1번 월드, 2번 월드...)")]
        public List<CampaignData> worlds = new List<CampaignData>();

        [Header("Shop / Achievement / Event Data")]
        public List<ShopItemData>    shopItems    = new List<ShopItemData>();
        public List<AchievementData> achievements = new List<AchievementData>();
        public List<EventData>       events       = new List<EventData>();
        public List<TowerData>       allTowers    = new List<TowerData>();

        // ── 상태 ──────────────────────────────────────────────────────────
        private LobbyScreen currentScreen   = LobbyScreen.Main;
        private int         selectedGroup   = -1;   // StageDetail 에서 사용
        private Vector2     scroll          = Vector2.zero;
        private List<string> gachaResults   = new List<string>();

        // 상점 탭 구분 (0: 젬 상점, 1: 타워 뽑기, 2: 포인트 상점)
        private int         shopTab         = 0;

        // ── GUI 스타일 ────────────────────────────────────────────────────
        private GUIStyle _title, _btn, _smallBtn, _label, _bar;
        private bool     _stylesReady;

        // ═════════════════════════════════════════════════════════════════
        // ── 스테이지 헬퍼 ──────────────────────────────────────────────────
        bool IsCleared(StageData st)
        {
            if (st == null || UserDataManager.Instance == null) return false;
            var r = UserDataManager.Instance.GetMapRecord(st.name);
            return r != null && r.cleared;
        }

        bool IsStageUnlocked(int wIdx, int sIdx)
        {
            if (wIdx == 0 && sIdx == 0) return true; // 가장 첫 스테이지는 무조건 열림
            
            if (sIdx > 0)
            {
                // 같은 월드의 이전 스테이지 클리어 확인
                var prevSt = worlds[wIdx].stages[sIdx - 1];
                return IsCleared(prevSt);
            }
            else
            {
                // 첫 스테이지라면 이전 월드의 마지막 스테이지 클리어 확인
                if (wIdx > 0 && worlds[wIdx - 1].stages.Count > 0)
                {
                    var prevWorldLastSt = worlds[wIdx - 1].stages[worlds[wIdx - 1].stages.Count - 1];
                    return IsCleared(prevWorldLastSt);
                }
            }
            return false;
        }

        bool IsWorldUnlocked(int wIdx)
        {
            if (wIdx == 0) return true;
            // 이전 월드의 마지막 스테이지를 클리어해야 다음 월드가 열림
            if (wIdx > 0 && worlds[wIdx - 1].stages.Count > 0)
            {
                var prevWorldLastSt = worlds[wIdx - 1].stages[worlds[wIdx - 1].stages.Count - 1];
                return IsCleared(prevWorldLastSt);
            }
            return false;
        }

        void GetFirstUnclearedStage(out int wIdx, out int sIdx)
        {
            wIdx = -1; sIdx = -1;
            for (int i = 0; i < worlds.Count; i++)
            {
                if (worlds[i] == null || worlds[i].stages == null) continue;
                for (int j = 0; j < worlds[i].stages.Count; j++)
                {
                    if (!IsCleared(worlds[i].stages[j]))
                    {
                        wIdx = i; sIdx = j; return;
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        private void Start()
        {
#if UNITY_EDITOR
            // 에디터에서 테스트 중일 때 인스펙터 리스트가 비어있다면 자동 색인 후 할당
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
                worlds.Sort((a, b) => a.name.CompareTo(b.name));
                if (worlds.Count > 0)
                    Debug.Log($"[LobbyUIManager] worlds가 비어 있어 자동으로 {worlds.Count}개 할당 완료!");
            }

            if (shopItems == null || shopItems.Count == 0)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ShopItemData");
                shopItems = new List<ShopItemData>();
                foreach (var g in guids) shopItems.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<ShopItemData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)));
            }

            if (achievements == null || achievements.Count == 0)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AchievementData");
                achievements = new List<AchievementData>();
                foreach (var g in guids) achievements.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<AchievementData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)));
            }

            if (events == null || events.Count == 0)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EventData");
                events = new List<EventData>();
                foreach (var g in guids) events.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<EventData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)));
            }

            if (allTowers == null || allTowers.Count == 0)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TowerData");
                allTowers = new List<TowerData>();
                foreach (var g in guids) allTowers.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<TowerData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)));
            }
#endif

            if (UserDataManager.Instance == null) return;
            foreach (var a in achievements)
                if (a != null) UserDataManager.Instance.RegisterAchievement(a.achievementId, a.achievementName);
            UserDataManager.Instance.Save();

            // 현재 씬 이름에 따라 상태 강제 동기화 (씬 분리 대응)
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "Lobby_Stage") currentScreen = LobbyScreen.StageGroups;
            else if (sceneName == "Lobby_Shop") currentScreen = LobbyScreen.Shop;
            else if (sceneName == "Lobby_Achievement") currentScreen = LobbyScreen.Achievement;
            else if (sceneName == "Lobby_Leaderboard") currentScreen = LobbyScreen.Leaderboard;
            else if (sceneName == "Lobby_Event") currentScreen = LobbyScreen.Event;
            else if (sceneName == "Main" || sceneName == "Lobby") currentScreen = LobbyScreen.Main;
            // StageDetail은 StageGroups에서 내부 상태로 전환됨
        }

        private void InitStyles()
        {
            if (_stylesReady) return;
            _title    = new GUIStyle(GUI.skin.label)  { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _title.normal.textColor = Color.yellow;
            _btn      = new GUIStyle(GUI.skin.button) { fontSize = 40, fontStyle = FontStyle.Bold };
            _smallBtn = new GUIStyle(GUI.skin.button) { fontSize = 28 };
            _label    = new GUIStyle(GUI.skin.label)  { fontSize = 32, wordWrap = true };
            _label.normal.textColor = Color.white;
            _bar      = new GUIStyle(GUI.skin.label)  { fontSize = 34, alignment = TextAnchor.MiddleRight };
            _bar.normal.textColor   = Color.yellow;
            _stylesReady = true;
        }

        private void OnGUI()
        {
            InitStyles();
            var ratio = new Vector2(Screen.width / 1920f, Screen.height / 1080f);
            var old   = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(ratio.x, ratio.y, 1f));

            // 배경
            GUI.color = new Color(0.08f, 0.08f, 0.14f);
            GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
            GUI.color = Color.white;

            switch (currentScreen)
            {
                case LobbyScreen.Main:        DrawMain();        break;
                case LobbyScreen.StageGroups: DrawStageGroups(); break;
                case LobbyScreen.StageDetail: DrawStageDetail(); break;
                case LobbyScreen.Shop:        DrawShop();        break;
                case LobbyScreen.Achievement: DrawAchievement(); break;
                case LobbyScreen.Leaderboard: DrawLeaderboard(); break;
                case LobbyScreen.Event:       DrawEvent();       break;
            }
            GUI.matrix = old;
        }

        // ── 공통 헬퍼 ────────────────────────────────────────────────────
        void Header(string text)
        {
            GUI.color = new Color(0.05f, 0.05f, 0.12f, 0.95f);
            GUI.DrawTexture(new Rect(0, 0, 1920, 110), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, 0, 1920, 110), text, _title);
        }

        void CurrencyBar()
        {
            if (UserDataManager.Instance == null) return;
            string username = BackendManager.Instance != null && BackendManager.Instance.IsSignedIn 
                ? (BackendManager.Instance.PlayerId.Length > 8 ? BackendManager.Instance.PlayerId.Substring(0, 8) + "..." : BackendManager.Instance.PlayerId) 
                : "Guest";
            
            // 행동력 자연 회복 강제 업데이트
            UserDataManager.Instance.UpdateStaminaRecovery();
            
            int stamina = UserDataManager.Instance.CurrentStamina;
            int maxStamina = UserDataManager.MAX_STAMINA;
            int gems = UserDataManager.Instance.PlayerGems;
            int shopPoints = UserDataManager.Instance.PlayerShopPoints;

            GUI.Label(new Rect(300, 0, 1590, 110),
                $"👤 {username}   |   ⚡ {stamina}/{maxStamina}   |   💎 {gems}   |   🪙 {shopPoints}", _bar);
        }

        bool BackBtn()
        {
            GUI.color = new Color(0.75f, 0.18f, 0.18f);
            bool c = GUI.Button(new Rect(30, 1000, 260, 60), "← 메인메뉴", _smallBtn);
            GUI.color = Color.white;
            if (c) 
            {
                // 씬 이동으로 메인 복귀
                SceneManager.LoadScene("Main");
            }
            return c;
        }

        void ColorBtn(Color col, Rect r, string label, System.Action action)
        {
            GUI.color = col;
            if (GUI.Button(r, label, _btn)) action?.Invoke();
            GUI.color = Color.white;
        }

        // ═════════════════════════════════════════════════════════════════
        // 1. 메인 화면
        // ═════════════════════════════════════════════════════════════════
        void DrawMain()
        {
            // 타이틀
            var ts = new GUIStyle(_title) { fontSize = 80 };
            GUI.Label(new Rect(0, 60, 1920, 130), "⚔  TOWER DEFENSE", ts);
            CurrencyBar();

            float bw = 790f, bh = 155f, gap = 40f;
            float cx = (1920f - (bw * 2 + gap)) / 2f;
            float y  = 240f;

            // START (전체 너비)
            ColorBtn(new Color(0.1f, 0.65f, 0.2f), new Rect(cx, y, bw * 2 + gap, bh), "▶  START", GoToNextChallenge);

            y += bh + gap;
            ColorBtn(new Color(0.2f, 0.38f, 0.80f), new Rect(cx,          y, bw, bh), "📋  STAGE",       () => Go(LobbyScreen.StageGroups));
            ColorBtn(new Color(0.72f, 0.48f, 0.08f), new Rect(cx + bw + gap, y, bw, bh), "🛒  SHOP",   () => Go(LobbyScreen.Shop));

            y += bh + gap;
            ColorBtn(new Color(0.55f, 0.18f, 0.70f), new Rect(cx,          y, bw, bh), "🏆  ACHIEVEMENT", () => Go(LobbyScreen.Achievement));
            ColorBtn(new Color(0.18f, 0.58f, 0.58f), new Rect(cx + bw + gap, y, bw, bh), "📊  LEADERBOARD",() => Go(LobbyScreen.Leaderboard));

            y += bh + gap;
            ColorBtn(new Color(0.78f, 0.28f, 0.08f), new Rect(cx, y, bw * 2 + gap, bh), "🎉  EVENT", () => Go(LobbyScreen.Event));
        }

        void Go(LobbyScreen s) 
        { 
            scroll = Vector2.zero; 
            if (s == LobbyScreen.StageDetail) 
            {
                currentScreen = s; // Detail은 Stage 씬 내부에서만 전환
                return;
            }

            string targetScene = "Main";
            switch(s)
            {
                case LobbyScreen.Main: targetScene = "Main"; break;
                case LobbyScreen.StageGroups: targetScene = "Lobby_Stage"; break;
                case LobbyScreen.Shop: targetScene = "Lobby_Shop"; break;
                case LobbyScreen.Achievement: targetScene = "Lobby_Achievement"; break;
                case LobbyScreen.Leaderboard: targetScene = "Lobby_Leaderboard"; break;
                case LobbyScreen.Event: targetScene = "Lobby_Event"; break;
            }
            SceneManager.LoadScene(targetScene);
        }

        void GoToNextChallenge()
        {
            if (worlds == null || worlds.Count == 0) { Debug.LogWarning("[LobbyUIManager] worlds가 비어 있습니다."); return; }
            GetFirstUnclearedStage(out int wIdx, out int sIdx);
            
            if (wIdx != -1 && sIdx != -1)
            {
                LoadStage(wIdx, sIdx);
            }
            else
            {
                // 모두 클리어 → 마지막 월드의 마지막 스테이지
                int lastW = worlds.Count - 1;
                int lastS = worlds[lastW].stages.Count - 1;
                LoadStage(lastW, lastS);
            }
        }

        void LoadStage(int wIdx, int sIdx)
        {
            if (worlds == null || wIdx < 0 || wIdx >= worlds.Count) 
            {
                Debug.LogError("[LobbyUIManager] 유효하지 않은 월드 인덱스입니다.");
                return;
            }

            // 스테이지 입장 시 행동력 10 소모 검증
            if (UserDataManager.Instance != null)
            {
                bool hasStamina = UserDataManager.Instance.SpendStamina(10);
                if (!hasStamina)
                {
                    Debug.LogWarning("[LobbyUIManager] 행동력이 부족하여 스테이지에 입장할 수 없습니다. (필요: 10)");
                    return;
                }
            }

            GameManager.staticTestCampaign   = worlds[wIdx];
            GameManager.staticTestStageIndex = sIdx;
            
            if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                Debug.LogError($"[LobbyUIManager] '{gameSceneName}' 씬을 로드할 수 없습니다. 상단 메뉴 Tools > TDF > Fix Build Settings (Add Scenes) 를 다시 한 번 눌러주세요!");
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // 2. 월드 선택 (World 1 ~ 10)
        // ═════════════════════════════════════════════════════════════════
        void DrawStageGroups()
        {
            Header("📋  월드 선택");
            CurrencyBar();

            float bw = 340f, bh = 120f, gap = 20f;
            int cols = 5;
            float totalW = cols * bw + (cols - 1) * gap;
            float startX = (1920f - totalW) / 2f;

            GetFirstUnclearedStage(out int activeWIdx, out int activeSIdx);

            for (int i = 0; i < 10; i++)
            {
                int row = i / cols, col = i % cols;
                float x = startX + col * (bw + gap);
                float y = 160f + row * (bh + gap);

                bool hasWorld = i < worlds.Count && worlds[i] != null;
                bool isUnlocked = hasWorld && IsWorldUnlocked(i);
                
                int cleared = 0;
                int totalSt = hasWorld ? worlds[i].stages.Count : 10;
                if (hasWorld)
                {
                    for (int s = 0; s < worlds[i].stages.Count; s++)
                        if (IsCleared(worlds[i].stages[s])) cleared++;
                }

                GUI.enabled = hasWorld;
                int captured = i;
                
                Color btnCol;
                if (!isUnlocked)
                {
                    btnCol = new Color(0.15f, 0.15f, 0.2f); // 비활성화 어둡게
                }
                else if (cleared == totalSt && totalSt > 0)
                {
                    btnCol = new Color(0.1f, 0.55f, 0.18f); // 올클리어
                }
                else
                {
                    btnCol = new Color(0.2f, 0.38f, 0.75f); // 진행 중 기본 색상
                }

                string lbl = hasWorld ? $"World {i + 1}\n({cleared}/{totalSt})" : $"World {i + 1}\n(준비 중)";

                // 현재 진행중인 월드 글자 빛나게 표시
                bool isCurrentActive = (i == activeWIdx);
                var prevStyle = GUI.skin.button;
                if (isCurrentActive)
                {
                    var activeStyle = new GUIStyle(_btn);
                    activeStyle.normal.textColor = Color.yellow; // 빛나는 텍스트 효과 (노란색)
                    GUI.skin.button = activeStyle;
                }
                else
                {
                    GUI.skin.button = _btn;
                }

                if (isUnlocked)
                {
                    ColorBtn(btnCol, new Rect(x, y, bw, bh), lbl, () => { selectedGroup = captured; Go(LobbyScreen.StageDetail); });
                }
                else
                {
                    ColorBtn(btnCol, new Rect(x, y, bw, bh), lbl, null);
                }

                GUI.skin.button = prevStyle;
                GUI.enabled = true;
            }
            BackBtn();
        }

        // ═════════════════════════════════════════════════════════════════
        // 3. 스테이지 상세 (월드 내부 스테이지 선택)
        // ═════════════════════════════════════════════════════════════════
        void DrawStageDetail()
        {
            if (selectedGroup < 0 || selectedGroup >= worlds.Count) { Go(LobbyScreen.StageGroups); return; }
            var currentWorld = worlds[selectedGroup];
            Header($"📋  World {selectedGroup + 1}  –  스테이지 선택");
            CurrencyBar();

            float bw = 340f, bh = 120f, gap = 20f;
            int cols = 5;
            float totalW = cols * bw + (cols - 1) * gap;
            float startX = (1920f - totalW) / 2f;

            GetFirstUnclearedStage(out int activeWIdx, out int activeSIdx);

            for (int i = 0; i < 10; i++)
            {
                int row = i / cols, col = i % cols;
                float x = startX + col * (bw + gap);
                float y = 160f + row * (bh + gap);

                bool hasStage = currentWorld != null && currentWorld.stages != null && i < currentWorld.stages.Count;
                var stage = hasStage ? currentWorld.stages[i] : null;
                GUI.enabled = hasStage;

                bool isUnlocked = hasStage && IsStageUnlocked(selectedGroup, i);
                bool done = hasStage && IsCleared(stage);
                
                string scoreTxt = "";
                if (done && UserDataManager.Instance != null)
                {
                    var rec = UserDataManager.Instance.GetMapRecord(stage.name);
                    if (rec != null && rec.hasBestResult)
                        scoreTxt = $"\n{rec.bestResult.score:N0}점";
                }

                Color btnCol;
                if (!isUnlocked)
                {
                    btnCol = new Color(0.15f, 0.15f, 0.2f); // 비활성화 어둡게
                }
                else if (done)
                {
                    btnCol = new Color(0.1f, 0.55f, 0.18f); // 클리어 완료
                }
                else
                {
                    btnCol = new Color(0.2f, 0.38f, 0.75f); // 도전 가능
                }

                string lbl = hasStage
                    ? $"{selectedGroup + 1}-{i + 1}{(done ? " ✓" : "")}{scoreTxt}"
                    : $"{selectedGroup + 1}-{i + 1}\n(준비 중)";

                // 현재 진행중인 스테이지 글자 빛나게 표시
                bool isCurrentActive = (selectedGroup == activeWIdx && i == activeSIdx);
                var prevStyle = GUI.skin.button;
                if (isCurrentActive)
                {
                    var activeStyle = new GUIStyle(_btn);
                    activeStyle.normal.textColor = Color.yellow; // 빛나는 텍스트 효과
                    GUI.skin.button = activeStyle;
                }
                else
                {
                    GUI.skin.button = _btn;
                }

                if (isUnlocked)
                {
                    int capIdx = i;
                    ColorBtn(btnCol, new Rect(x, y, bw, bh), lbl, () => LoadStage(selectedGroup, capIdx));
                }
                else
                {
                    ColorBtn(btnCol, new Rect(x, y, bw, bh), lbl, null);
                }

                GUI.skin.button = prevStyle;
                GUI.enabled = true;
            }

            GUI.color = new Color(0.3f, 0.3f, 0.7f);
            if (GUI.Button(new Rect(310, 1000, 280, 60), "← 월드 선택", _smallBtn)) Go(LobbyScreen.StageGroups);
            GUI.color = Color.white;
            BackBtn();
        }

        // ═════════════════════════════════════════════════════════════════
        // 4. 상점
        // ═════════════════════════════════════════════════════════════════
        void DrawShop()
        {
            Header("🛒  상점 & 소환");
            CurrencyBar();

            float padX = 160f, startY = 130f, panelW = 1600f, panelH = 830f;
            
            // ── 상단 탭 버튼 배치 ──────────────────────────────────
            float tabW = panelW / 3f;
            string[] tabs = { "젬 상점", "타워 뽑기", "포인트 상점" };
            for (int i = 0; i < 3; i++)
            {
                var tabStyle = new GUIStyle(GUI.skin.button) { fontSize = 28, fontStyle = FontStyle.Bold };
                if (shopTab == i)
                {
                    GUI.color = new Color(0.3f, 0.7f, 1f);
                    tabStyle.normal.textColor = Color.white;
                }
                else
                {
                    GUI.color = new Color(0.15f, 0.15f, 0.25f);
                }

                if (GUI.Button(new Rect(padX + i * tabW, startY, tabW - 5f, 60f), tabs[i], tabStyle))
                {
                    shopTab = i;
                    scroll = Vector2.zero;
                }
            }
            GUI.color = Color.white;

            float contentY = startY + 80f;
            float contentH = panelH - 80f;

            if (shopTab == 0)
            {
                // ── [젬 상점] ──────────────────────────────────────────
                GUI.Box(new Rect(padX, contentY, panelW, contentH), "젬 상점", new GUIStyle(GUI.skin.box) { fontSize = 24, fontStyle = FontStyle.Bold });

                float rowH = 130f;
                int itemCount = 3;
                scroll = GUI.BeginScrollView(new Rect(padX + 20f, contentY + 40f, panelW - 40f, contentH - 60f),
                    scroll, new Rect(0, 0, panelW - 70f, itemCount * (rowH + 10f)));

                for (int i = 0; i < itemCount; i++)
                {
                    float y = i * (rowH + 10f);
                    GUI.color = new Color(0.14f, 0.14f, 0.22f);
                    GUI.DrawTexture(new Rect(0, y, panelW - 70f, rowH), Texture2D.whiteTexture);
                    GUI.color = Color.white;

                    string title = "";
                    string desc = "";
                    int cost = 0;
                    string productKey = "";

                    if (i == 0)
                    {
                        title = "💎 보석 묶음 구매";
                        desc = "로비 상점에서 사용할 수 있는 프리미엄 보석을 구매합니다. (보석 2,000개 지급)";
                        cost = 0;
                        productKey = "gem_pack_01";
                    }
                    else if (i == 1)
                    {
                        title = "🔋 행동력 100개 충전";
                        desc = "모험 플레이에 소모되는 행동력을 즉시 충전합니다.";
                        cost = 100;
                        productKey = "stamina_charge_01";
                    }
                    else
                    {
                        title = "📅 월간 특별 패키지";
                        desc = "풍성한 혜택이 담긴 월간 고효율 성장 패키지 상품입니다.";
                        cost = 500;
                        productKey = "monthly_pack_01";
                    }

                    GUI.Label(new Rect(20, y + 15, 800, 50), title, _label);
                    GUI.Label(new Rect(20, y + 65, 800, 50), desc, new GUIStyle(_label) { fontSize = 22, normal = { textColor = Color.gray } });

                    bool bought = UserDataManager.Instance?.HasPurchased(productKey) ?? false;
                    string costLabel = cost > 0 ? $"💎 {cost}" : "무료 충전";

                    if (bought && productKey.Contains("monthly"))
                    {
                        GUI.color = Color.gray;
                        GUI.Button(new Rect(panelW - 380f, y + 30, 280, 70), "구매 완료", _smallBtn);
                        GUI.color = Color.white;
                    }
                    else
                    {
                        GUI.color = new Color(0.7f, 0.5f, 0.1f);
                        if (GUI.Button(new Rect(panelW - 380f, y + 30, 280, 70), $"구매  {costLabel}", _smallBtn))
                        {
                            if (cost > 0)
                            {
                                bool success = UserDataManager.Instance.SpendGems(cost);
                                if (success)
                                {
                                    UserDataManager.Instance.AddPurchase(productKey, title, 0, cost);
                                    if (productKey.Contains("stamina"))
                                    {
                                        UserDataManager.Instance.AddStamina(100);
                                        Debug.Log("[Shop] 행동력 100 충전 완료!");
                                    }
                                    UserDataManager.Instance.Save();
                                }
                                else
                                {
                                    Debug.LogWarning("[Shop] 보석이 부족합니다.");
                                }
                            }
                            else
                            {
                                UserDataManager.Instance.AddCurrency(gems: 2000);
                                UserDataManager.Instance.AddPurchase(productKey, title, 0, 0);
                                UserDataManager.Instance.Save();
                            }
                        }
                        GUI.color = Color.white;
                    }
                }
                GUI.EndScrollView();
            }
            else if (shopTab == 1)
            {
                // ── [타워 뽑기] ────────────────────────────────────────
                GUI.color = new Color(0.12f, 0.10f, 0.22f, 0.95f);
                GUI.DrawTexture(new Rect(padX, contentY, panelW, contentH), Texture2D.whiteTexture);
                GUI.color = Color.white;

                var gachaTitleStyle = new GUIStyle(_label) { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                gachaTitleStyle.normal.textColor = Color.yellow;
                GUI.Label(new Rect(padX, contentY + 20f, panelW, 60f), "🔮  타워 소환 (Gacha)", gachaTitleStyle);

                var infoStyle = new GUIStyle(_label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
                infoStyle.normal.textColor = new Color(0.7f, 0.7f, 0.9f);
                GUI.Label(new Rect(padX, contentY + 85f, panelW, 50f), "기본 확률: R 90% | SR 8% | SSR 1% | SP 1%\n(10회 연속 소환 마지막 카드 보증 확률: SR 80% | SSR 10% | SP 10%)", infoStyle);

                ColorBtn(new Color(0.2f, 0.5f, 0.8f), new Rect(padX + 280f, contentY + 160f, 480f, 85f), "1회 소환 (💎 100)", () => TryPullGacha(false));
                ColorBtn(new Color(0.6f, 0.2f, 0.7f), new Rect(padX + 840f, contentY + 160f, 480f, 85f), "10회 소환 (💎 1000)", () => TryPullGacha(true));

                GUI.Box(new Rect(padX + 80f, contentY + 270f, panelW - 160f, contentH - 290f), "소환 결과", new GUIStyle(GUI.skin.box) { fontSize = 22, fontStyle = FontStyle.Bold });

                var resultStyle = new GUIStyle(_label) { fontSize = 26, richText = true };
                resultStyle.normal.textColor = Color.white;

                if (gachaResults.Count == 0)
                {
                    var emptyStyle = new GUIStyle(resultStyle) { alignment = TextAnchor.MiddleCenter };
                    emptyStyle.normal.textColor = Color.gray;
                    GUI.Label(new Rect(padX + 90f, contentY + 310f, panelW - 180f, contentH - 350f), "소환 버튼을 눌러 타워를 획득하세요!", emptyStyle);
                }
                else
                {
                    float resultStartY = contentY + 320f;
                    for (int idx = 0; idx < gachaResults.Count; idx++)
                    {
                        if (gachaResults.Count > 1)
                        {
                            int col = idx % 2;
                            int row = idx / 2;
                            GUI.Label(new Rect(padX + 180f + col * 640f, resultStartY + row * 65f, 600f, 60f), gachaResults[idx], resultStyle);
                        }
                        else
                        {
                            var singleResultStyle = new GUIStyle(resultStyle) { alignment = TextAnchor.MiddleCenter };
                            GUI.Label(new Rect(padX + 90f, resultStartY + 60f, panelW - 180f, 150f), $"🎉 소환 성공! 🎉\n\n{gachaResults[idx]}", singleResultStyle);
                        }
                    }
                }
            }
            else if (shopTab == 2)
            {
                // ── [포인트 상점] ───────────────────────────────────────
                GUI.Box(new Rect(padX, contentY, panelW, contentH), "포인트 상점", new GUIStyle(GUI.skin.box) { fontSize = 24, fontStyle = FontStyle.Bold });

                float rowH = 130f;
                scroll = GUI.BeginScrollView(new Rect(padX + 20f, contentY + 40f, panelW - 40f, contentH - 60f),
                    scroll, new Rect(0, 0, panelW - 70f, Mathf.Max(contentH - 60f, allTowers.Count * (rowH + 10f))));

                for (int i = 0; i < allTowers.Count; i++)
                {
                    var tower = allTowers[i];
                    if (tower == null) continue;
                    float y = i * (rowH + 10f);

                    GUI.color = new Color(0.12f, 0.12f, 0.18f);
                    GUI.DrawTexture(new Rect(0, y, panelW - 70f, rowH), Texture2D.whiteTexture);
                    GUI.color = Color.white;

                    string rarityTag = tower.rarity == TowerRarity.SP ? "<color=red>[SP]</color>" : 
                                       tower.rarity == TowerRarity.SSR ? "<color=orange>[SSR]</color>" :
                                       tower.rarity == TowerRarity.SR ? "<color=cyan>[SR]</color>" : "[R]";
                    
                    bool isUnlocked = UserDataManager.Instance?.IsTowerUnlocked(tower.towerId) ?? false;
                    int towerPt = UserDataManager.Instance != null ? UserDataManager.Instance.GetTowerPoint(tower.towerId) : 1;

                    string statusLabel = isUnlocked ? $"보유 중 (성장 Pt: {towerPt}/7)" : "<color=yellow>미획득</color>";

                    GUI.Label(new Rect(20, y + 15, 600, 50), $"{rarityTag} {tower.towerName}", _label);
                    GUI.Label(new Rect(20, y + 65, 600, 50), $"상태: {statusLabel}", new GUIStyle(_label) { fontSize = 22, richText = true });

                    int costPoints = 10;
                    bool canAfford = UserDataManager.Instance != null && UserDataManager.Instance.PlayerShopPoints >= costPoints;

                    if (isUnlocked && towerPt >= 7)
                    {
                        GUI.color = Color.gray;
                        GUI.Button(new Rect(panelW - 380f, y + 30, 280, 70), "성장 완료 (최대 Pt)", _smallBtn);
                        GUI.color = Color.white;
                    }
                    else
                    {
                        string btnLabel = isUnlocked ? $"성장 Pt +1 (🪙 {costPoints})" : $"타워 구매 (🪙 {costPoints})";
                        GUI.color = canAfford ? new Color(0.2f, 0.6f, 0.4f) : new Color(0.3f, 0.3f, 0.3f);
                        if (GUI.Button(new Rect(panelW - 380f, y + 30, 280, 70), btnLabel, _smallBtn))
                        {
                            if (canAfford)
                            {
                                TryBuyTowerWithPoints(tower, costPoints);
                            }
                            else
                            {
                                Debug.LogWarning("[Point Shop] 상점 포인트가 부족합니다.");
                            }
                        }
                        GUI.color = Color.white;
                    }
                }
                GUI.EndScrollView();
            }

            BackBtn();
        }

        void TryBuyItem(ShopItemData item)
        {
            if (UserDataManager.Instance == null) return;
            bool ok = UserDataManager.Instance.SpendGems(item.costGems);

            if (!ok) { Debug.Log("[Shop] 재화가 부족합니다."); return; }

            UserDataManager.Instance.AddPurchase(item.itemId, item.itemName, 0, item.costGems);
            // 보상 지급
            if (item.rewardGems > 0) UserDataManager.Instance.AddCurrency(gems: item.rewardGems);
        }

        void TryPullGacha(bool isTen)
        {
            if (UserDataManager.Instance == null) return;
            if (allTowers == null || allTowers.Count == 0)
            {
                Debug.LogWarning("[Gacha] 등록된 타워가 없어 뽑기를 진행할 수 없습니다.");
                return;
            }

            int cost = isTen ? 1000 : 100;
            bool success = UserDataManager.Instance.SpendGems(cost);
            if (!success)
            {
                Debug.LogWarning("[Gacha] 젬이 부족합니다.");
                return;
            }

            gachaResults.Clear();
            if (isTen)
            {
                for (int i = 0; i < 9; i++)
                {
                    PullGacha(false);
                }
                // 10번째는 보증 확률 적용: SR 80%, SSR 10%, SP 10%
                PullGacha(true);
            }
            else
            {
                PullGacha(false);
            }
            UserDataManager.Instance.Save();
        }

        void PullGacha(bool isGuaranteed)
        {
            List<TowerData> poolR = allTowers.FindAll(t => t.rarity == TowerRarity.R);
            List<TowerData> poolSR = allTowers.FindAll(t => t.rarity == TowerRarity.SR);
            List<TowerData> poolSSR = allTowers.FindAll(t => t.rarity == TowerRarity.SSR);
            List<TowerData> poolSP = allTowers.FindAll(t => t.rarity == TowerRarity.SP);

            TowerRarity selectedRarity = TowerRarity.R;
            float rand = Random.Range(0f, 100f);

            if (isGuaranteed)
            {
                // 보증 확률: SR 80%, SSR 10%, SP 10%
                if (rand < 80f) selectedRarity = TowerRarity.SR;
                else if (rand < 90f) selectedRarity = TowerRarity.SSR;
                else selectedRarity = TowerRarity.SP;
            }
            else
            {
                // 일반 확률: R 90%, SR 8%, SSR 1%, SP 1%
                if (rand < 90f) selectedRarity = TowerRarity.R;
                else if (rand < 98f) selectedRarity = TowerRarity.SR;
                else if (rand < 99f) selectedRarity = TowerRarity.SSR;
                else selectedRarity = TowerRarity.SP;
            }

            List<TowerData> activePool = GetActivePool(selectedRarity, poolR, poolSR, poolSSR, poolSP);
            TowerData rolledTower = activePool.Count > 0 ? activePool[Random.Range(0, activePool.Count)] : allTowers[Random.Range(0, allTowers.Count)];
            
            string rarityTag = rolledTower.rarity == TowerRarity.SP ? "<color=red>SP</color>" : 
                               rolledTower.rarity == TowerRarity.SSR ? "<color=orange>SSR</color>" :
                               rolledTower.rarity == TowerRarity.SR ? "<color=cyan>SR</color>" : "R";
            
            bool alreadyUnlocked = UserDataManager.Instance.IsTowerUnlocked(rolledTower.towerId);
            if (!alreadyUnlocked)
            {
                // 신규 획득: 언락 및 towerPoint = 1로 초기화 (UnlockTower 내부에서 1로 세팅됨)
                UserDataManager.Instance.UnlockTower(rolledTower.towerId, TowerUnlockSource.Purchase, "Gacha");
                gachaResults.Add($"[{rarityTag}] {rolledTower.towerName} <color=yellow>(New!)</color>");
            }
            else
            {
                // 중복 획득 분기
                int currentPt = UserDataManager.Instance.GetTowerPoint(rolledTower.towerId);
                if (currentPt < 7)
                {
                    int nextPt = currentPt + 1;
                    UserDataManager.Instance.SetTowerPoint(rolledTower.towerId, nextPt);
                    gachaResults.Add($"[{rarityTag}] {rolledTower.towerName} <color=green>(Pt {currentPt}→{nextPt})</color>");
                }
                else
                {
                    // 7포인트 도달 완료 시: 등급에 맞춰 상점 포인트 지급 (R: 1, SR: 3, SSR/SP: 5)
                    int shopPointsReward = 0;
                    switch (rolledTower.rarity)
                    {
                        case TowerRarity.R: shopPointsReward = 1; break;
                        case TowerRarity.SR: shopPointsReward = 3; break;
                        case TowerRarity.SSR:
                        case TowerRarity.SP:
                            shopPointsReward = 5;
                            break;
                    }
                    UserDataManager.Instance.AddShopPoints(shopPointsReward);
                    gachaResults.Add($"[{rarityTag}] {rolledTower.towerName} <color=magenta>(🪙+{shopPointsReward})</color>");
                }
            }
        }

        void TryBuyTowerWithPoints(TowerData tower, int cost)
        {
            if (UserDataManager.Instance == null) return;
            if (UserDataManager.Instance.PlayerShopPoints < cost) return;

            // 포인트 차감
            UserDataManager.Instance.AddShopPoints(-cost);

            bool isUnlocked = UserDataManager.Instance.IsTowerUnlocked(tower.towerId);
            if (!isUnlocked)
            {
                UserDataManager.Instance.UnlockTower(tower.towerId, TowerUnlockSource.Purchase, "PointShop");
                Debug.Log($"[Point Shop] 상점 포인트로 타워 구매 성공: {tower.towerName}");
            }
            else
            {
                int currentPt = UserDataManager.Instance.GetTowerPoint(tower.towerId);
                UserDataManager.Instance.SetTowerPoint(tower.towerId, currentPt + 1);
                Debug.Log($"[Point Shop] 상점 포인트로 타워 성장 성공: {tower.towerName} (Pt {currentPt}→{currentPt + 1})");
            }
        }

        List<TowerData> GetActivePool(TowerRarity rarity, List<TowerData> r, List<TowerData> sr, List<TowerData> ssr, List<TowerData> sp)
        {
            switch (rarity)
            {
                case TowerRarity.R: return r.Count > 0 ? r : (sr.Count > 0 ? sr : allTowers);
                case TowerRarity.SR: return sr.Count > 0 ? sr : (ssr.Count > 0 ? ssr : allTowers);
                case TowerRarity.SSR: return ssr.Count > 0 ? ssr : (sp.Count > 0 ? sp : allTowers);
                case TowerRarity.SP: return sp.Count > 0 ? sp : (ssr.Count > 0 ? ssr : allTowers);
                default: return allTowers;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // 5. 업적
        // ═════════════════════════════════════════════════════════════════
        void DrawAchievement()
        {
            Header("🏆  업적");
            CurrencyBar();

            float panelW = 1600f, rowH = 140f, padX = 160f, startY = 130f;
            scroll = GUI.BeginScrollView(new Rect(padX, startY, panelW, 830f),
                scroll, new Rect(0, 0, panelW - 30f, Mathf.Max(830f, achievements.Count * (rowH + 10f))));

            for (int i = 0; i < achievements.Count; i++)
            {
                var def = achievements[i];
                if (def == null) continue;
                float y = i * (rowH + 10f);

                GUI.color = new Color(0.14f, 0.12f, 0.22f);
                GUI.DrawTexture(new Rect(0, y, panelW - 30f, rowH), Texture2D.whiteTexture);
                GUI.color = Color.white;

                var prog = UserDataManager.Instance?.GetAchievementProgress(def.achievementId);
                int cur = prog?.currentProgress ?? 0;
                bool done = prog?.completed ?? false;
                bool claimed = prog?.rewardClaimed ?? false;

                GUI.Label(new Rect(20, y + 8,  700, 50), def.achievementName, _label);
                GUI.Label(new Rect(20, y + 60, 700, 40), def.description,
                    new GUIStyle(_label) { fontSize = 24 });

                // 진행 바
                float barW = 500f;
                float pct = def.targetValue > 0 ? Mathf.Clamp01((float)cur / def.targetValue) : 0f;
                GUI.color = new Color(0.2f, 0.2f, 0.3f);
                GUI.DrawTexture(new Rect(740, y + 40, barW, 30), Texture2D.whiteTexture);
                GUI.color = done ? new Color(0.2f, 0.8f, 0.3f) : new Color(0.3f, 0.5f, 1f);
                GUI.DrawTexture(new Rect(740, y + 40, barW * pct, 30), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(740, y + 75, barW, 30),
                    $"{cur} / {def.targetValue}", new GUIStyle(_label) { fontSize = 22 });

                // 보상 버튼
                string rewardText = $"💎 +{def.rewardGems}";
                if (done && !claimed)
                {
                    GUI.color = new Color(0.55f, 0.18f, 0.70f);
                    if (GUI.Button(new Rect(panelW - 290f, y + 35, 260, 70), $"수령\n{rewardText}", _smallBtn))
                    {
                        UserDataManager.Instance?.ClaimAchievementReward(def.achievementId);
                        if (def.rewardGems > 0) UserDataManager.Instance?.AddCurrency(gems: def.rewardGems);
                    }
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = Color.gray;
                    GUI.Button(new Rect(panelW - 290f, y + 35, 260, 70),
                        claimed ? $"수령 완료\n{rewardText}" : rewardText, _smallBtn);
                    GUI.color = Color.white;
                }
            }
            GUI.EndScrollView();
            BackBtn();
        }

        // ═════════════════════════════════════════════════════════════════
        // 6. 리더보드
        // ═════════════════════════════════════════════════════════════════
        void DrawLeaderboard()
        {
            Header("📊  리더보드 – 스테이지 최고 기록");
            CurrencyBar();

            var records = UserDataManager.Instance?.Data.mapClearRecords;
            if (records == null || records.Count == 0)
            {
                GUI.Label(new Rect(0, 500, 1920, 80), "아직 클리어한 스테이지가 없습니다.",
                    new GUIStyle(_label) { alignment = TextAnchor.MiddleCenter, fontSize = 40 });
                BackBtn(); return;
            }

            float panelW = 1600f, rowH = 90f, padX = 160f, startY = 130f;
            // 헤더 행
            GUI.color = new Color(0.1f, 0.1f, 0.2f);
            GUI.DrawTexture(new Rect(padX, startY, panelW, 50f), Texture2D.whiteTexture);
            GUI.color = Color.yellow;
            var hStyle = new GUIStyle(_label) { fontSize = 28, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(padX + 10, startY + 5, 500, 45), "스테이지", hStyle);
            GUI.Label(new Rect(padX + 500, startY + 5, 260, 45), "점수", hStyle);
            GUI.Label(new Rect(padX + 760, startY + 5, 220, 45), "클리어 타임", hStyle);
            GUI.Label(new Rect(padX + 980, startY + 5, 160, 45), "체력", hStyle);
            GUI.Label(new Rect(padX + 1140, startY + 5, 160, 45), "골드", hStyle);
            GUI.color = Color.white;

            scroll = GUI.BeginScrollView(new Rect(padX, startY + 52f, panelW, 778f),
                scroll, new Rect(0, 0, panelW - 30f, Mathf.Max(778f, records.Count * (rowH + 6f))));

            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                float y = i * (rowH + 6f);
                GUI.color = (i % 2 == 0) ? new Color(0.13f, 0.13f, 0.2f) : new Color(0.1f, 0.1f, 0.16f);
                GUI.DrawTexture(new Rect(0, y, panelW - 30f, rowH), Texture2D.whiteTexture);
                GUI.color = Color.white;

                string best = r.hasBestResult
                    ? $"{r.bestResult.score:N0}   {r.bestResult.clearTimeSeconds}초   ❤{r.bestResult.livesRemaining}   💰{r.bestResult.goldRemaining}"
                    : "-";
                GUI.Label(new Rect(10, y + 20, 490, 50), r.stageName, _label);
                if (r.hasBestResult)
                {
                    var b = r.bestResult;
                    GUI.Label(new Rect(500, y + 20, 260, 50), $"{b.score:N0}", _label);
                    GUI.Label(new Rect(760, y + 20, 220, 50), $"{b.clearTimeSeconds}초", _label);
                    GUI.Label(new Rect(980, y + 20, 160, 50), $"❤ {b.livesRemaining}", _label);
                    GUI.Label(new Rect(1140, y + 20, 160, 50), $"💰 {b.goldRemaining}", _label);
                }
                else
                    GUI.Label(new Rect(500, y + 20, 800, 50), "기록 없음", _label);
            }
            GUI.EndScrollView();
            BackBtn();
        }

        // ═════════════════════════════════════════════════════════════════
        // 7. 이벤트
        // ═════════════════════════════════════════════════════════════════
        void DrawEvent()
        {
            Header("🎉  이벤트");
            CurrencyBar();

            if (events == null || events.Count == 0)
            {
                GUI.Label(new Rect(0, 500, 1920, 80), "진행 중인 이벤트가 없습니다.",
                    new GUIStyle(_label) { alignment = TextAnchor.MiddleCenter, fontSize = 40 });
                BackBtn(); return;
            }

            float panelW = 1600f, rowH = 160f, padX = 160f, startY = 130f;
            scroll = GUI.BeginScrollView(new Rect(padX, startY, panelW, 830f),
                scroll, new Rect(0, 0, panelW - 30f, Mathf.Max(830f, events.Count * (rowH + 14f))));

            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                if (ev == null) continue;
                float y = i * (rowH + 14f);

                GUI.color = new Color(0.16f, 0.10f, 0.10f);
                GUI.DrawTexture(new Rect(0, y, panelW - 30f, rowH), Texture2D.whiteTexture);
                GUI.color = Color.white;

                GUI.Label(new Rect(20, y + 10, 900, 50), ev.eventName, _label);
                string period = (!string.IsNullOrEmpty(ev.startDate) && !string.IsNullOrEmpty(ev.endDate))
                    ? $"{ev.startDate}  ~  {ev.endDate}"
                    : "상시 이벤트";
                GUI.Label(new Rect(20, y + 65, 900, 40), period, new GUIStyle(_label) { fontSize = 24 });

                var prog = UserDataManager.Instance?.GetEventProgress(ev.eventId);
                string btnLabel = (prog != null && prog.completed && !prog.rewardClaimed)
                    ? $"보상 수령\n💎+{ev.rewardGems}"
                    : (prog != null && prog.rewardClaimed ? "수령 완료"
                    : $"참여하기\n💎+{ev.rewardGems}");

                Color btnCol = (prog != null && prog.rewardClaimed) ? Color.gray : new Color(0.75f, 0.28f, 0.08f);
                GUI.color = btnCol;
                if (GUI.Button(new Rect(panelW - 300f, y + 40, 270, 80), btnLabel, _smallBtn))
                    HandleEventBtn(ev);
                GUI.color = Color.white;
            }
            GUI.EndScrollView();
            BackBtn();
        }

        void HandleEventBtn(EventData ev)
        {
            if (UserDataManager.Instance == null) return;
            var prog = UserDataManager.Instance.GetEventProgress(ev.eventId);

            if (prog == null)
            {
                UserDataManager.Instance.JoinEvent(ev.eventId, ev.eventName);
                UserDataManager.Instance.CompleteEvent(ev.eventId);
            }
            else if (prog.completed && !prog.rewardClaimed)
            {
                UserDataManager.Instance.ClaimEventReward(ev.eventId);
                if (ev.rewardGems > 0) UserDataManager.Instance.AddCurrency(gems: ev.rewardGems);
            }
        }
    }
}
