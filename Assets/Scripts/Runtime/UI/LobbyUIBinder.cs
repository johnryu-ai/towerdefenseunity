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
                Debug.LogError("[LobbyUIBinder] uiData가 비어있습니다! 인스펙터에서 LobbyMenuUIData 에셋을 할당해주세요.");
                return;
            }
        }

        private void OnEnable()
        {
            if (uiRoot != null && uiData != null)
            {
                // UI가 다시 활성화될 때마다 최신 런타임 데이터 반영
                ApplyUI();
            }
        }

        public void ApplyUI()
        {
            // Clear existing generated children
            foreach (Transform child in uiRoot) {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            // 1. Create Background
            if (uiData.backgroundImage != null)
            {
                backgroundObj = new GameObject("Background_Generated", typeof(RectTransform), typeof(Image));
                backgroundObj.transform.SetParent(uiRoot, false);
                backgroundObj.transform.SetAsFirstSibling(); // Put it behind everything
                
                RectTransform rt = backgroundObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                
                Image img = backgroundObj.GetComponent<Image>();
                img.sprite = uiData.backgroundImage;
                img.type = Image.Type.Simple;
                img.preserveAspect = false; // Usually stretch for background
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
                foreach (var b in uiData.buttons)
                {
                    if (b.buttonName.StartsWith("Achieve_")) isAchievementPage = true;
                }

                if (isAchievementPage && UserDataManager.Instance != null)
                {
                    HashSet<string> achIds = new HashSet<string>();
                    List<float> yPositions = new List<float>();

                    foreach (var b in uiData.buttons)
                    {
                        if (b.buttonName.StartsWith("Achieve_"))
                        {
                            string id = b.buttonName.Split('_')[1]; // Achieve_ID_...
                            achIds.Add(id);
                        }
                    }

                    // Extract unique Y positions used by these elements
                    foreach (var b in uiData.buttons)
                    {
                        if (b.buttonName.EndsWith("_Title"))
                        {
                            if (!yPositions.Contains(b.buttonRect.y)) yPositions.Add(b.buttonRect.y);
                        }
                    }
                    yPositions.Sort(); // Ascending (Top to Bottom)

                    // Sort IDs: Unclaimed first, Claimed last
                    List<string> sortedIds = new List<string>(achIds);
                    sortedIds.Sort((a, b) => {
                        var progA = UserDataManager.Instance.GetAchievementProgress(a);
                        var progB = UserDataManager.Instance.GetAchievementProgress(b);
                        bool claimedA = progA != null && progA.completed && progA.rewardClaimed;
                        bool claimedB = progB != null && progB.completed && progB.rewardClaimed;
                        
                        if (claimedA == claimedB) return a.CompareTo(b);
                        return claimedA ? 1 : -1; // claimed goes to bottom
                    });

                    // Clone and reassign Y positions
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
                    buttonsToProcess.AddRange(uiData.buttons);
                }

                foreach (var btnData in buttonsToProcess)
                {
                    GameObject btnObj = new GameObject($"Btn_{btnData.buttonName}", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnObj.transform.SetParent(uiRoot, false);
                    
                    RectTransform rt = btnObj.GetComponent<RectTransform>();
                    ApplyRectToRectTransform(btnData.buttonRect, rt);
                    
                    Image img = btnObj.GetComponent<Image>();
                    if (btnData.buttonImage != null)
                    {
                        img.sprite = btnData.buttonImage;
                    }
                    else
                    {
                        // Default transparent or solid color if no image
                        img.color = new Color(1, 1, 1, 0.5f);
                    }
                    
                    Button btn = btnObj.GetComponent<Button>();
                    
                    // Setup Text
                    if (!string.IsNullOrEmpty(btnData.buttonText))
                    {
                        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                        textObj.transform.SetParent(btnObj.transform, false);
                        
                        RectTransform txtRt = textObj.GetComponent<RectTransform>();
                        txtRt.anchorMin = Vector2.zero;
                        txtRt.anchorMax = Vector2.one;
                        txtRt.offsetMin = Vector2.zero;
                        txtRt.offsetMax = Vector2.zero;
                        
                        Text txt = textObj.GetComponent<Text>();
                        txt.text = btnData.buttonText;
                        txt.color = btnData.textColor;
                        txt.fontSize = btnData.fontSize;
                        txt.alignment = TextAnchor.MiddleCenter;
                        txt.fontStyle = btnData.fontStyle;
                        
                        if (btnData.fontAsset != null)
                        {
                            txt.font = btnData.fontAsset;
                        }
                        else
                        {
                            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                            txt.font = defaultFont;
                        }
                    }
                    
                    // Bind Click Event
                    btn.onClick.AddListener(() => OnButtonClicked(btnData));

                    // Dynamic Data Binding (Shop, Achievement, Stage, etc.)
                    Text btnTxt = btnObj.GetComponentInChildren<Text>();
                    ProcessDynamicData(btnData, btn, btnTxt, img);
                }
            }
        }

        private void ProcessDynamicData(LobbyButtonData data, Button btn, Text txt, Image img)
        {
            if (UserDataManager.Instance == null) return;

            // 1. 상점 아이템
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
                        // TODO: 실제 구매 로직 (LobbyUIManager 참조)
                        Debug.Log($"[Shop] Buy item {itemId}");
                    });
                }
            }
            // 젬 표시 라벨
            else if (data.buttonName == "GemCountLabel" && txt != null)
            {
                txt.text = $"💎 GEMS : {UserDataManager.Instance.PlayerGems}";
            }

            // 2. 업적 보상 개별 버튼
            else if (data.buttonName.StartsWith("Achieve_") && data.buttonName.EndsWith("_Claim"))
            {
                // Name format: Achieve_{ID}_{Gems}_Claim
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
                    // 아직 진행 기록이 없을 때 (In Progress 처리)
                    if (txt != null) txt.text = $"💎 {gems}";
                    btn.interactable = false;
                    img.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }
            // 2-1. 전체 받기 버튼
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

            // 3. 리더보드
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

            // 4. 스테이지
            else if (data.buttonName.StartsWith("World_"))
            {
                btn.onClick.AddListener(() => {
                    // 배경 이미지 교체
                    if (backgroundObj != null)
                    {
                        Image bgImg = backgroundObj.GetComponent<Image>();
                        bgImg.color = new Color(Random.Range(0.2f,0.5f), Random.Range(0.2f,0.5f), Random.Range(0.2f,0.5f));
                    }
                });
            }
            else if (data.buttonName.StartsWith("Stage_"))
            {
                int stageIdx = int.Parse(data.buttonName.Replace("Stage_", ""));
                int world = (stageIdx - 1) / 10 + 1;
                int stage = (stageIdx - 1) % 10 + 1;
                
                if (txt != null) txt.text = $"Stage {world}-{stage}";

                var records = UserDataManager.Instance.Data.mapClearRecords;
                int clearedCount = records != null ? records.Count : 0;
                
                // 스테이지가 이미 클리어된 스테이지(stageIdx <= clearedCount) 이거나,
                // 현재 공략 중인 스테이지(stageIdx == clearedCount + 1) 일 때만 활성화.
                bool isUnlocked = (stageIdx <= clearedCount + 1);

                if (!isUnlocked)
                {
                    btn.interactable = false;
                    img.color = Color.gray;
                }
                else
                {
                    // 클리어된 스테이지면 다른 색상 (초록빛)
                    if (stageIdx <= clearedCount)
                        img.color = new Color(0.6f, 1f, 0.6f);
                        
                    btn.onClick.AddListener(() => {
                        LobbyManager.Instance.StartGameAt(stageIdx - 1);
                    });
                }
            }
        }

        private void OnButtonClicked(LobbyButtonData data)
        {
            if (LobbyManager.Instance == null)
            {
                Debug.LogWarning("LobbyManager.Instance is null. Cannot perform action.");
                return;
            }

            switch (data.actionType)
            {
                case ButtonActionType.OpenPage:
                    if (data.targetPageAsset != null)
                        LobbyManager.Instance.OpenPage(data.targetPageAsset.pageType);
                    else
                        Debug.LogWarning($"[LobbyUIBinder] Target Page Asset is not assigned for button: {data.buttonName}");
                    break;
                case ButtonActionType.StartGame:
                    LobbyManager.Instance.StartGame();
                    break;
                case ButtonActionType.CloseCurrentPage:
                    // If we are on a generated page, we'd close it.
                    // Assuming uiRoot is the page itself:
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
