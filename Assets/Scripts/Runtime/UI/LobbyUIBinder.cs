using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TDF.Core.Data;
using TDF.Runtime.Managers;

namespace TDF.Runtime.UI
{
    public class LobbyUIBinder : MonoBehaviour
    {
        [Header("Data Source")]
        public LobbyMenuUIData uiData;
        
        [Header("Target Container")]
        public RectTransform uiRoot; // The parent where UI elements will be generated

        private GameObject backgroundObj;
        private GameObject bannerObj;

        private void Awake()
        {
            // Auto-assign uiRoot if null
            if (uiRoot == null)
            {
                uiRoot = GetComponent<RectTransform>();
            }

            // Auto-assign uiData if null (find first available in Resources or rely on Editor script)
            // But since this is runtime, we can't use AssetDatabase. 
            // We'll just check if it's missing and warn.
            if (uiData == null)
            {
                // uiData는 생성 직후 LobbyManager에서 할당하므로 Awake에서의 에러 로그는 제거합니다.
                return;
            }
        }

        // ── 동적 스테이지 선택용 상태 ──
        private int selectedWorldIdx = -1; // -1이면 World 선택 화면, 그 이상이면 해당 World의 Stage 선택 화면

        private void OnEnable()
        {
            selectedWorldIdx = -1; // 진입 시 항상 World 화면으로 초기화
            if (uiRoot != null && uiData != null)
            {
                ApplyUI();
            }
        }

        public void ApplyUI()
        {
            if (uiData == null)
            {
                Debug.LogError("[LobbyUIBinder] uiData가 할당되지 않아 UI를 생성할 수 없습니다.");
                return;
            }

            if (uiRoot == null) uiRoot = GetComponent<RectTransform>();
            if (uiRoot == null) return;

            // Clear existing generated children
            foreach (Transform child in uiRoot) 
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            // 1. Create Background
            if (uiData.backgroundImage != null)
            {
                backgroundObj = new GameObject("Background_Generated", typeof(RectTransform), typeof(Image));
                backgroundObj.transform.SetParent(uiRoot, false);
                backgroundObj.transform.SetAsFirstSibling();
                
                RectTransform rt = backgroundObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                
                Image img = backgroundObj.GetComponent<Image>();
                img.sprite = uiData.backgroundImage;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
            }

            // 2. Create Top Banner
            if (uiData.topBannerImage != null)
            {
                bannerObj = new GameObject("TopBanner_Generated", typeof(RectTransform), typeof(Image));
                bannerObj.transform.SetParent(uiRoot, false);
                
                RectTransform rt = bannerObj.GetComponent<RectTransform>();
                ApplyRectToRectTransform(uiData.topBannerRect, rt);
                
                Image img = bannerObj.GetComponent<Image>();
                img.sprite = uiData.topBannerImage;
            }

            // 3. Create Buttons
            if (uiData.buttons != null)
            {
                List<LobbyButtonData> buttonsToProcess = new List<LobbyButtonData>();

                // [Dynamic Reordering for Achievements]
                bool isAchievementPage = false;
                bool isStagePage = false;
                foreach (var b in uiData.buttons)
                {
                    if (b.buttonName.StartsWith("Achieve_")) isAchievementPage = true;
                    if (b.buttonName.StartsWith("World_") || b.buttonName.StartsWith("Stage_")) isStagePage = true;
                }

                if (isAchievementPage && UserDataManager.Instance != null)
                {
                    HashSet<string> achIds = new HashSet<string>();
                    List<float> yPositions = new List<float>();

                    foreach (var b in uiData.buttons)
                    {
                        if (b.buttonName.StartsWith("Achieve_"))
                        {
                            string id = b.buttonName.Split('_')[1];
                            achIds.Add(id);
                        }
                    }

                    foreach (var b in uiData.buttons)
                    {
                        if (b.buttonName.EndsWith("_Title"))
                        {
                            if (!yPositions.Contains(b.buttonRect.y)) yPositions.Add(b.buttonRect.y);
                        }
                    }
                    yPositions.Sort();

                    List<string> sortedIds = new List<string>(achIds);
                    sortedIds.Sort((a, b) => {
                        var progA = UserDataManager.Instance.GetAchievementProgress(a);
                        var progB = UserDataManager.Instance.GetAchievementProgress(b);
                        bool claimedA = progA != null && progA.completed && progA.rewardClaimed;
                        bool claimedB = progB != null && progB.completed && progB.rewardClaimed;
                        
                        if (claimedA == claimedB) return a.CompareTo(b);
                        return claimedA ? 1 : -1;
                    });

                    foreach (var b in uiData.buttons)
                    {
                        LobbyButtonData clone = new LobbyButtonData();
                        clone.buttonName = b.buttonName;
                        clone.buttonText = b.buttonText;
                        clone.buttonRect = b.buttonRect;
                        clone.actionType = b.actionType;
                        clone.targetPageAsset = b.targetPageAsset;
                        clone.textColor = b.textColor;
                        clone.fontSize = b.fontSize;
                        clone.fontStyle = b.fontStyle;
                        clone.fontAsset = b.fontAsset;
                        clone.buttonImage = b.buttonImage;

                        if (b.buttonName.StartsWith("Achieve_"))
                        {
                            string id = b.buttonName.Split('_')[1];
                            int sortedIndex = sortedIds.IndexOf(id);
                            if (sortedIndex >= 0 && sortedIndex < yPositions.Count)
                            {
                                clone.buttonRect = new Rect(clone.buttonRect.x, yPositions[sortedIndex], clone.buttonRect.width, clone.buttonRect.height);
                            }
                        }
                        buttonsToProcess.Add(clone);
                    }
                }
                else
                {
                    foreach (var b in uiData.buttons)
                    {
                        // 2단계 스테이지 구성에 따른 버튼 필터링
                        if (isStagePage)
                        {
                            if (selectedWorldIdx == -1) // World 화면
                            {
                                if (b.buttonName.StartsWith("Stage_") || b.buttonName == "StageBackButton") continue; // 숨김
                            }
                            else // Stage 화면
                            {
                                if (b.buttonName.StartsWith("World_")) continue; // 숨김
                            }
                        }
                        buttonsToProcess.Add(b);
                    }
                }

                foreach (var btnData in buttonsToProcess)
                {
                    GameObject btnObj = new GameObject($"Btn_{btnData.buttonName}", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnObj.transform.SetParent(uiRoot, false);
                    
                    RectTransform rt = btnObj.GetComponent<RectTransform>();
                    ApplyRectToRectTransform(btnData.buttonRect, rt);
                    
                    Image img = btnObj.GetComponent<Image>();
                    if (btnData.buttonImage != null)
                        img.sprite = btnData.buttonImage;
                    else
                        img.color = new Color(1, 1, 1, 0.5f);
                    
                    Button btn = btnObj.GetComponent<Button>();
                    
                    if (!string.IsNullOrEmpty(btnData.buttonText))
                    {
                        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                        textObj.transform.SetParent(btnObj.transform, false);
                        
                        RectTransform txtRt = textObj.GetComponent<RectTransform>();
                        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                        txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
                        
                        Text txt = textObj.GetComponent<Text>();
                        txt.text = btnData.buttonText;
                        txt.color = btnData.textColor;
                        txt.fontSize = btnData.fontSize;
                        txt.alignment = TextAnchor.MiddleCenter;
                        txt.fontStyle = btnData.fontStyle;
                        
                        if (btnData.fontAsset != null)
                            txt.font = btnData.fontAsset;
                        else if (FontSettings.Instance != null && FontSettings.Instance.defaultFont != null)
                            txt.font = FontSettings.Instance.defaultFont;
                        else
                            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                    
                    btn.onClick.AddListener(() => OnButtonClicked(btnData));

                    Text btnTxt = btnObj.GetComponentInChildren<Text>();
                    ProcessDynamicData(btnData, btn, btnTxt, img);
                }
            }
        }

        private bool IsStageCleared(string stageName)
        {
            if (UserDataManager.Instance == null) return false;
            var r = UserDataManager.Instance.GetMapRecord(stageName);
            return r != null && r.cleared;
        }

        private void ProcessDynamicData(LobbyButtonData data, Button btn, Text txt, Image img)
        {
            if (UserDataManager.Instance == null) return;

            if (data.buttonName.StartsWith("ShopItem_"))
            {
                string itemId = data.buttonName.Replace("ShopItem_", "");
                if (UserDataManager.Instance.HasPurchased(itemId))
                {
                    if (txt != null) txt.text += "\n\n[ Purchased ]";
                    btn.interactable = false;
                    img.color = Color.gray;
                }
                else
                {
                    btn.onClick.AddListener(() => {
                        Debug.Log($"[Shop] Buy item {itemId}");
                    });
                }
            }
            else if (data.buttonName == "GemCountLabel" && txt != null)
            {
                txt.text = $"💎 GEMS : {UserDataManager.Instance.PlayerGems}";
            }
            else if (data.buttonName.StartsWith("Achieve_") && data.buttonName.EndsWith("_Claim"))
            {
                string[] parts = data.buttonName.Split('_');
                string achId = parts[1];
                string gems = parts.Length > 3 ? parts[2] : "0";

                var prog = UserDataManager.Instance.GetAchievementProgress(achId);
                
                if (prog != null)
                {
                    if (prog.completed)
                    {
                        if (prog.rewardClaimed)
                        {
                            if (txt != null) txt.text = "Claimed";
                            btn.interactable = false;
                            img.color = Color.gray;
                        }
                        else
                        {
                            if (txt != null) txt.text = $"💎 {gems}";
                            btn.onClick.AddListener(() => {
                                UserDataManager.Instance.ClaimAchievementReward(achId);
                                ApplyUI(); // 리프레시
                            });
                        }
                    }
                    else
                    {
                        if (txt != null) txt.text = $"💎 {gems}";
                        btn.interactable = false;
                        img.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    }
                }
                else
                {
                    if (txt != null) txt.text = $"💎 {gems}";
                    btn.interactable = false;
                    img.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }
            else if (data.buttonName == "AchieveClaimAllButton")
            {
                btn.onClick.AddListener(() => {
                    bool claimedAny = false;
                    var progressList = UserDataManager.Instance.Data.achievementProgresses;
                    if (progressList != null)
                    {
                        List<string> keysToClaim = new List<string>();
                        foreach (var prog in progressList)
                        {
                            if (prog != null && prog.completed && !prog.rewardClaimed)
                            {
                                keysToClaim.Add(prog.achievementId);
                            }
                        }
                        foreach (string id in keysToClaim)
                        {
                            UserDataManager.Instance.ClaimAchievementReward(id);
                            claimedAny = true;
                        }
                    }
                    if (claimedAny)
                    {
                        ApplyUI(); // 리프레시
                    }
                });
            }
            else if (data.buttonName.StartsWith("Rank_") && txt != null)
            {
                int rankIdx = int.Parse(data.buttonName.Replace("Rank_", ""));
                var records = UserDataManager.Instance.Data.mapClearRecords;
                if (records != null && records.Count >= rankIdx)
                {
                    var r = records[rankIdx - 1];
                    if (r.hasBestResult)
                        txt.text = $"{rankIdx}. {r.stageName}\nScore: {r.bestResult.score}";
                    else
                        txt.text = $"{rankIdx}. {r.stageName}\nNo Record";
                }
                else
                {
                    txt.text = $"{rankIdx}. -";
                }
            }
            // 4. 월드 & 스테이지 (LobbyManager.worlds 기반 2단계 메뉴)
            else if (data.buttonName.StartsWith("World_"))
            {
                int wIdx = int.Parse(data.buttonName.Replace("World_", "")) - 1; // 0-indexed
                var worlds = LobbyManager.Instance.worlds;
                bool hasWorld = (worlds != null && wIdx >= 0 && wIdx < worlds.Count && worlds[wIdx] != null);

                bool isUnlocked = true;
                if (wIdx > 0 && hasWorld)
                {
                    var prevWorld = worlds[wIdx - 1];
                    if (prevWorld != null && prevWorld.stages.Count > 0)
                    {
                        isUnlocked = IsStageCleared(prevWorld.stages[prevWorld.stages.Count - 1].name);
                    }
                }

                if (!hasWorld)
                {
                    if (txt != null) txt.text = $"World {wIdx + 1}\n(준비 중)";
                    btn.interactable = false;
                    img.color = new Color(0.2f, 0.2f, 0.2f);
                }
                else if (!isUnlocked)
                {
                    if (txt != null) txt.text = $"World {wIdx + 1}\n(잠김)";
                    btn.interactable = false;
                    img.color = new Color(0.15f, 0.15f, 0.2f);
                }
                else
                {
                    int cleared = 0;
                    int total = worlds[wIdx].stages.Count;
                    for (int i = 0; i < total; i++)
                    {
                        if (IsStageCleared(worlds[wIdx].stages[i].name)) cleared++;
                    }

                    if (txt != null) txt.text = $"World {wIdx + 1}\n({cleared}/{total})";
                    img.color = cleared == total && total > 0 ? new Color(0.1f, 0.55f, 0.18f) : new Color(0.2f, 0.38f, 0.75f);
                    
                    // 현재 플레이해야 할 월드인지 계산 (가장 첫 번째 미클리어 스테이지가 있는 곳)
                    bool isActiveWorld = false;
                    for (int i = 0; i < worlds.Count; i++)
                    {
                        if (worlds[i] == null) continue;
                        bool foundUncleared = false;
                        for (int j = 0; j < worlds[i].stages.Count; j++)
                        {
                            if (!IsStageCleared(worlds[i].stages[j].name))
                            {
                                if (i == wIdx) isActiveWorld = true;
                                foundUncleared = true;
                                break;
                            }
                        }
                        if (foundUncleared) break;
                    }

                    if (isActiveWorld && txt != null) txt.color = Color.yellow; // 반짝임 효과

                    btn.onClick.AddListener(() => {
                        selectedWorldIdx = wIdx;
                        ApplyUI(); // 스테이지 뷰로 갱신
                    });
                }
            }
            else if (data.buttonName.StartsWith("Stage_"))
            {
                int sIdx = int.Parse(data.buttonName.Replace("Stage_", "")) - 1; // 0-indexed
                var worlds = LobbyManager.Instance.worlds;
                bool hasStage = (selectedWorldIdx >= 0 && selectedWorldIdx < worlds.Count && worlds[selectedWorldIdx] != null && sIdx >= 0 && sIdx < worlds[selectedWorldIdx].stages.Count);

                bool isUnlocked = true;
                if (sIdx > 0 && hasStage)
                {
                    isUnlocked = IsStageCleared(worlds[selectedWorldIdx].stages[sIdx - 1].name);
                }

                if (!hasStage)
                {
                    if (txt != null) txt.text = $"Stage {selectedWorldIdx + 1}-{sIdx + 1}\n(준비 중)";
                    btn.interactable = false;
                    img.color = new Color(0.2f, 0.2f, 0.2f);
                }
                else if (!isUnlocked)
                {
                    if (txt != null) txt.text = $"Stage {selectedWorldIdx + 1}-{sIdx + 1}\n(잠김)";
                    btn.interactable = false;
                    img.color = new Color(0.15f, 0.15f, 0.2f);
                }
                else
                {
                    bool isCleared = IsStageCleared(worlds[selectedWorldIdx].stages[sIdx].name);
                    
                    string scoreTxt = "";
                    var rec = UserDataManager.Instance.GetMapRecord(worlds[selectedWorldIdx].stages[sIdx].name);
                    if (rec != null && rec.hasBestResult) scoreTxt = $"\n{rec.bestResult.score:N0}점";

                    if (txt != null) txt.text = $"Stage {selectedWorldIdx + 1}-{sIdx + 1}{(isCleared ? " ✓" : "")}{scoreTxt}";
                    img.color = isCleared ? new Color(0.1f, 0.55f, 0.18f) : new Color(0.2f, 0.38f, 0.75f);
                    
                    // 가장 첫 번째 미클리어 스테이지인지 확인
                    bool isActiveStage = false;
                    if (!isCleared)
                    {
                        isActiveStage = true;
                        // 나보다 앞선 월드에 미클리어가 있으면 나는 액티브가 아님
                        for (int i = 0; i <= selectedWorldIdx; i++)
                        {
                            if (worlds[i] == null) continue;
                            for (int j = 0; j < (i == selectedWorldIdx ? sIdx : worlds[i].stages.Count); j++)
                            {
                                if (!IsStageCleared(worlds[i].stages[j].name))
                                {
                                    isActiveStage = false; break;
                                }
                            }
                            if (!isActiveStage) break;
                        }
                    }

                    if (isActiveStage && txt != null) txt.color = Color.yellow; // 반짝임 효과

                    int capturedWIdx = selectedWorldIdx;
                    int capturedSIdx = sIdx;
                    btn.onClick.AddListener(() => {
                        LobbyManager.Instance.StartGameAt(capturedWIdx, capturedSIdx);
                    });
                }
            }
            else if (data.buttonName == "StageBackButton")
            {
                if (txt != null) txt.text = "← 돌아가기";
                btn.onClick.AddListener(() => {
                    selectedWorldIdx = -1;
                    ApplyUI();
                });
            }
        }

        private void OnButtonClicked(LobbyButtonData data)
        {
            if (LobbyManager.Instance == null)
            {
                Debug.LogWarning("[LobbyUIBinder] LobbyManager.Instance is null. Cannot perform action.");
                return;
            }

            // 동적으로 특수 기능을 수행하는 버튼들은 기본 액션(OpenPage, ClosePage 등)을 무시합니다.
            if (data.buttonName.StartsWith("World_") || 
                data.buttonName.StartsWith("Stage_") || 
                data.buttonName.StartsWith("Achieve_") ||
                data.buttonName.StartsWith("ShopItem_") ||
                data.buttonName == "StageBackButton" ||
                data.buttonName == "AchieveClaimAllButton")
            {
                return;
            }

            Debug.Log($"[LobbyUIBinder] Button Clicked: Name='{data.buttonName}', Text='{data.buttonText}', Action='{data.actionType}'");

            switch (data.actionType)
            {
                case ButtonActionType.OpenPage:
                    if (data.targetPageAsset != null)
                    {
                        Debug.Log($"[LobbyUIBinder] Found targetPageAsset. Opening PageType: {data.targetPageAsset.pageType}");
                        LobbyManager.Instance.OpenPage(data.targetPageAsset.pageType);
                    }
                    else
                    {
                        // Fallback: 버튼 이름/텍스트 기반으로 PageType 추론 (에셋 미할당 시 레거시 서브씬 지원)
                        PageType fallbackType = PageType.Main;
                        string bName = data.buttonName != null ? data.buttonName.ToUpper() : "";
                        string bText = data.buttonText != null ? data.buttonText.ToUpper() : "";
                        
                        if (bName.Contains("STAGE") || bText.Contains("STAGE")) fallbackType = PageType.StageSelect;
                        else if (bName.Contains("SHOP") || bText.Contains("SHOP")) fallbackType = PageType.Shop;
                        else if (bName.Contains("ACHIEVEMENT") || bText.Contains("ACHIEVE") || bName.Contains("ACHIEVE") || bText.Contains("ACHIEVEMENT")) fallbackType = PageType.Achievement;
                        else if (bName.Contains("LEADERBOARD") || bText.Contains("LEADERBOARD") || bName.Contains("RANK") || bText.Contains("RANK")) fallbackType = PageType.Leaderboard;
                        else if (bName.Contains("EVENT") || bText.Contains("EVENT")) fallbackType = PageType.Event;

                        Debug.Log($"[LobbyUIBinder] Target asset is null. Inferred Fallback PageType: {fallbackType}");

                        if (fallbackType != PageType.Main)
                        {
                            LobbyManager.Instance.OpenPage(fallbackType);
                        }
                        else
                        {
                            Debug.LogWarning($"[LobbyUIBinder] Target Page Asset is not assigned and cannot infer fallback for button: {data.buttonName}");
                        }
                    }
                    break;
                case ButtonActionType.StartGame:
                    Debug.Log("[LobbyUIBinder] Starting Game...");
                    LobbyManager.Instance.StartGame();
                    break;
                case ButtonActionType.CloseCurrentPage:
                    Debug.Log("[LobbyUIBinder] Closing current page...");
                    LobbyManager.Instance.CloseCurrentPage(this.gameObject);
                    break;
            }
        }

        // Helper to map GUI Rect (Top-Left origin) to RectTransform (Center/Bottom-Left origin depending on anchors)
        // Here we assume anchors are Top-Left (0,1) for simplicity matching Editor GUI
        private void ApplyRectToRectTransform(Rect rect, RectTransform rt)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(rect.x, -rect.y);
            rt.sizeDelta = new Vector2(rect.width, rect.height);
        }
    }
}
