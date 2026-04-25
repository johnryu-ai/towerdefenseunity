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

        [Header("All Stages (순서대로. 10개씩 자동 그룹화: 0~9=Stage1, 10~19=Stage2 …)")]
        public CampaignData allStages;

        [Header("Shop / Achievement / Event Data")]
        public List<ShopItemData>    shopItems    = new List<ShopItemData>();
        public List<AchievementData> achievements = new List<AchievementData>();
        public List<EventData>       events       = new List<EventData>();

        // ── 상태 ──────────────────────────────────────────────────────────
        private LobbyScreen currentScreen   = LobbyScreen.Main;
        private int         selectedGroup   = -1;   // StageDetail 에서 사용
        private Vector2     scroll          = Vector2.zero;

        // ── GUI 스타일 ────────────────────────────────────────────────────
        private GUIStyle _title, _btn, _smallBtn, _label, _bar;
        private bool     _stylesReady;

        // ═════════════════════════════════════════════════════════════════
        // ── 스테이지 헬퍼 ──────────────────────────────────────────────────
        int TotalStages  => allStages?.stages?.Count ?? 0;
        int GroupCount   => Mathf.CeilToInt(TotalStages / 10f);

        StageData GetStage(int groupIdx, int subIdx)
        {
            int flat = groupIdx * 10 + subIdx;
            return (allStages?.stages != null && flat < allStages.stages.Count) ? allStages.stages[flat] : null;
        }

        bool IsCleared(StageData st)
        {
            if (st == null || UserDataManager.Instance == null) return false;
            var r = UserDataManager.Instance.GetMapRecord(st.name);
            return r != null && r.cleared;
        }

        // ─────────────────────────────────────────────────────────────────
        private void Start()
        {
            if (UserDataManager.Instance == null) return;
            foreach (var a in achievements)
                if (a != null) UserDataManager.Instance.RegisterAchievement(a.achievementId, a.achievementName);
            UserDataManager.Instance.Save();
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
            GUI.Label(new Rect(1380, 0, 510, 110),
                $"💰 {UserDataManager.Instance.PlayerGold}   💎 {UserDataManager.Instance.PlayerGems}", _bar);
        }

        bool BackBtn()
        {
            GUI.color = new Color(0.75f, 0.18f, 0.18f);
            bool c = GUI.Button(new Rect(30, 1000, 260, 60), "← 메인메뉴", _smallBtn);
            GUI.color = Color.white;
            if (c) { currentScreen = LobbyScreen.Main; scroll = Vector2.zero; }
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

        void Go(LobbyScreen s) { currentScreen = s; scroll = Vector2.zero; }

        void GoToNextChallenge()
        {
            if (allStages?.stages == null) { Debug.LogWarning("[LobbyUIManager] allStages가 비어 있습니다."); return; }
            for (int i = 0; i < allStages.stages.Count; i++)
            {
                var st = allStages.stages[i];
                if (st == null) continue;
                if (!IsCleared(st)) { LoadStage(i); return; }
            }
            // 모두 클리어 → 마지막 스테이지
            LoadStage(allStages.stages.Count - 1);
        }

        void LoadStage(int flatIndex)
        {
            if (allStages == null) return;
            GameManager.staticTestCampaign   = allStages;
            GameManager.staticTestStageIndex = flatIndex;
            SceneManager.LoadScene(gameSceneName);
        }

        // ═════════════════════════════════════════════════════════════════
        // 2. 스테이지 그룹 선택 (Stage 1 ~ 10)
        // ═════════════════════════════════════════════════════════════════
        void DrawStageGroups()
        {
            Header("📋  스테이지 선택");
            CurrencyBar();

            int totalGroups = Mathf.Min(GroupCount, 10);
            float bw = 340f, bh = 120f, gap = 20f;
            int cols = 5;
            float totalW = cols * bw + (cols - 1) * gap;
            float startX = (1920f - totalW) / 2f;

            for (int i = 0; i < 10; i++)
            {
                int row = i / cols, col = i % cols;
                float x = startX + col * (bw + gap);
                float y = 160f + row * (bh + gap);

                bool hasGroup = i < totalGroups;
                // 그룹 내 클리어 수 계산
                int cleared = 0;
                if (hasGroup)
                    for (int s = 0; s < 10; s++)
                        if (IsCleared(GetStage(i, s))) cleared++;

                GUI.enabled = hasGroup;
                int captured = i;
                var col2 = cleared == 10 ? new Color(0.1f, 0.55f, 0.18f)
                         : cleared > 0   ? new Color(0.55f, 0.38f, 0.08f)
                                         : new Color(0.2f, 0.38f, 0.75f);
                string lbl = hasGroup ? $"Stage {i + 1}\n({cleared}/10)" : $"Stage {i + 1}\n(준비 중)";
                ColorBtn(col2, new Rect(x, y, bw, bh), lbl, () => { selectedGroup = captured; Go(LobbyScreen.StageDetail); });
                GUI.enabled = true;
            }
            BackBtn();
        }

        // ═════════════════════════════════════════════════════════════════
        // 3. 스테이지 상세 (1-1 ~ 1-10)
        // ═════════════════════════════════════════════════════════════════
        void DrawStageDetail()
        {
            if (selectedGroup < 0 || selectedGroup >= GroupCount) { Go(LobbyScreen.StageGroups); return; }
            Header($"📋  Stage {selectedGroup + 1}  –  스테이지 선택");
            CurrencyBar();

            float bw = 340f, bh = 120f, gap = 20f;
            int cols = 5;
            float totalW = cols * bw + (cols - 1) * gap;
            float startX = (1920f - totalW) / 2f;

            for (int i = 0; i < 10; i++)
            {
                int row = i / cols, col = i % cols;
                float x = startX + col * (bw + gap);
                float y = 160f + row * (bh + gap);

                var stage = GetStage(selectedGroup, i);
                bool hasStage = stage != null;
                GUI.enabled = hasStage;

                bool done = IsCleared(stage);
                // 최고 점수 표시
                string scoreTxt = "";
                if (done && UserDataManager.Instance != null)
                {
                    var rec = UserDataManager.Instance.GetMapRecord(stage.name);
                    if (rec != null && rec.hasBestResult)
                        scoreTxt = $"\n{rec.bestResult.score:N0}점";
                }

                int flatIdx = selectedGroup * 10 + i;
                var btnCol = done ? new Color(0.1f, 0.55f, 0.18f) : new Color(0.2f, 0.38f, 0.75f);
                string lbl = hasStage
                    ? $"{selectedGroup + 1}-{i + 1}{(done ? " ✓" : "")}{scoreTxt}"
                    : $"{selectedGroup + 1}-{i + 1}\n(준비 중)";

                ColorBtn(btnCol, new Rect(x, y, bw, bh), lbl, () => LoadStage(flatIdx));
                GUI.enabled = true;
            }

            GUI.color = new Color(0.3f, 0.3f, 0.7f);
            if (GUI.Button(new Rect(310, 1000, 280, 60), "← 그룹 선택", _smallBtn)) Go(LobbyScreen.StageGroups);
            GUI.color = Color.white;
            BackBtn();
        }

        // ═════════════════════════════════════════════════════════════════
        // 4. 상점
        // ═════════════════════════════════════════════════════════════════
        void DrawShop()
        {
            Header("🛒  상점");
            CurrencyBar();

            float panelW = 1600f, rowH = 130f, padX = 160f, startY = 130f;
            scroll = GUI.BeginScrollView(new Rect(padX, startY, panelW, 830f),
                scroll, new Rect(0, 0, panelW - 30f, Mathf.Max(830f, shopItems.Count * (rowH + 10f))));

            for (int i = 0; i < shopItems.Count; i++)
            {
                var item = shopItems[i];
                if (item == null) continue;
                float y = i * (rowH + 10f);

                GUI.color = new Color(0.14f, 0.14f, 0.22f);
                GUI.DrawTexture(new Rect(0, y, panelW - 30f, rowH), Texture2D.whiteTexture);
                GUI.color = Color.white;

                GUI.Label(new Rect(20, y + 10, 700, 50),  item.itemName,    _label);
                GUI.Label(new Rect(20, y + 60, 700, 50),  item.description, new GUIStyle(_label) { fontSize = 24 });

                bool bought = UserDataManager.Instance?.HasPurchased(item.itemId) ?? false;
                string costLabel = item.costGold > 0 ? $"💰{item.costGold}" : $"💎{item.costGems}";

                if (bought)
                {
                    GUI.color = Color.gray;
                    GUI.Button(new Rect(panelW - 290f, y + 30, 260, 70), "구매 완료", _smallBtn);
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = new Color(0.7f, 0.5f, 0.1f);
                    if (GUI.Button(new Rect(panelW - 290f, y + 30, 260, 70), $"구매  {costLabel}", _smallBtn))
                        TryBuyItem(item);
                    GUI.color = Color.white;
                }
            }
            GUI.EndScrollView();
            BackBtn();
        }

        void TryBuyItem(ShopItemData item)
        {
            if (UserDataManager.Instance == null) return;
            bool ok = item.costGold > 0
                ? UserDataManager.Instance.SpendGold(item.costGold)
                : UserDataManager.Instance.SpendGems(item.costGems);

            if (!ok) { Debug.Log("[Shop] 재화가 부족합니다."); return; }

            UserDataManager.Instance.AddPurchase(item.itemId, item.itemName, item.costGold, item.costGems);
            // 보상 지급
            if (item.rewardGold > 0) UserDataManager.Instance.AddCurrency(gold: item.rewardGold);
            if (item.rewardGems > 0) UserDataManager.Instance.AddCurrency(gems: item.rewardGems);
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
                        if (def.rewardGold > 0) UserDataManager.Instance?.AddCurrency(gold: def.rewardGold);
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
                    ? $"보상 수령\n💰+{ev.rewardGold}  💎+{ev.rewardGems}"
                    : (prog != null && prog.rewardClaimed ? "수령 완료"
                    : $"참여하기\n💰+{ev.rewardGold}  💎+{ev.rewardGems}");

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
                if (ev.rewardGold > 0) UserDataManager.Instance.AddCurrency(gold: ev.rewardGold);
                if (ev.rewardGems > 0) UserDataManager.Instance.AddCurrency(gems: ev.rewardGems);
            }
        }
    }
}
