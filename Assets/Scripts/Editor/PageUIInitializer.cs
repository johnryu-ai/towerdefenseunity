using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TDF.Core.Data;
using System.IO;

namespace TDF.Editor
{
    public class PageUIInitializer : EditorWindow
    {
        [MenuItem("Tools/TDF/Generate Default Visual Layouts")]
        public static void Generate()
        {
            string layoutPath = "Assets/Data/Layouts";
            string dataPath = "Assets/Data/Pages";

            // 폴더 생성
            if (!Directory.Exists(Path.Combine(Application.dataPath, "Data/Layouts"))) 
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Layouts"));
            if (!Directory.Exists(Path.Combine(Application.dataPath, "Data/Pages"))) 
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Pages"));

            // 1. 모든 PageData 생성 (미리 존재해야 참조 가능)
            foreach (PageType type in System.Enum.GetValues(typeof(PageType)))
            {
                string dataFilePath = $"{dataPath}/{type}Data.asset";
                PageData data = AssetDatabase.LoadAssetAtPath<PageData>(dataFilePath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<PageData>();
                    AssetDatabase.CreateAsset(data, dataFilePath);
                }
                data.pageId = type.ToString().ToLower();
                data.pageType = type;
                data.isPopup = (type == PageType.GameOption || type == PageType.Event);
                EditorUtility.SetDirty(data);
            }

            // 2. 모든 Visual Layout Data 생성 및 참조 할당
            foreach (PageType type in System.Enum.GetValues(typeof(PageType)))
            {
                string name = $"{type}Layout";
                string fullLayoutPath = $"{layoutPath}/{name}.asset";

                LobbyMenuUIData layoutData = AssetDatabase.LoadAssetAtPath<LobbyMenuUIData>(fullLayoutPath);
                if (layoutData == null)
                {
                    layoutData = ScriptableObject.CreateInstance<LobbyMenuUIData>();
                    AssetDatabase.CreateAsset(layoutData, fullLayoutPath);
                }
                
                SetupBasicLayout(layoutData, type);
                EditorUtility.SetDirty(layoutData);

                // PageData에 방금 만든 레이아웃 연결
                string dataFilePath = $"{dataPath}/{type}Data.asset";
                PageData data = AssetDatabase.LoadAssetAtPath<PageData>(dataFilePath);
                if (data != null)
                {
                    data.visualLayout = layoutData;
                    EditorUtility.SetDirty(data);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TDF Visual Layout Data & PageData Generated Successfully!");
        }

        private static void SetupBasicLayout(LobbyMenuUIData data, PageType type)
        {
            data.buttons.Clear();
            data.topBannerRect = new Rect(460, 50, 1000, 150);

            // 공통: 뒤로 가기 / 닫기 버튼
            if (type != PageType.Main && type != PageType.Login)
            {
                LobbyButtonData closeBtn = new LobbyButtonData();
                closeBtn.buttonName = "CloseButton";
                closeBtn.buttonText = "X";
                closeBtn.buttonRect = new Rect(1700, 50, 100, 100);
                closeBtn.textColor = Color.white;
                closeBtn.fontSize = 50;
                closeBtn.fontStyle = FontStyle.Bold;
                closeBtn.actionType = ButtonActionType.CloseCurrentPage;
                data.buttons.Add(closeBtn);
            }

            switch (type)
            {
                case PageType.Main:
                    string[] btnNames = { "START", "STAGE", "SHOP", "ACHIEVEMENT", "LEADERBOARD", "EVENT", "OPTION" };
                    PageType[] targetPages = { PageType.Main, PageType.StageSelect, PageType.Shop, PageType.Achievement, PageType.Leaderboard, PageType.Event, PageType.GameOption };

                    float startY = 250f;
                    float spacing = 100f;
                    float btnWidth = 450f;
                    float btnHeight = 80f;
                    float centerX = (1920f - btnWidth) / 2f;

                    for (int i = 0; i < btnNames.Length; i++)
                    {
                        LobbyButtonData btn = new LobbyButtonData();
                        btn.buttonName = btnNames[i] + "Button";
                        btn.buttonText = btnNames[i];
                        btn.buttonRect = new Rect(centerX, startY + (i * spacing), btnWidth, btnHeight);
                        btn.textColor = Color.white;
                        btn.fontSize = 32;
                        btn.fontStyle = FontStyle.Bold;
                        
                        if (btnNames[i] == "START") btn.actionType = ButtonActionType.StartGame;
                        else
                        {
                            btn.actionType = ButtonActionType.OpenPage;
                            string pageDataPath = $"Assets/Data/Pages/{targetPages[i]}Data.asset";
                            PageData pd = AssetDatabase.LoadAssetAtPath<PageData>(pageDataPath);
                            if (pd != null) btn.targetPageAsset = pd;
                        }
                        data.buttons.Add(btn);
                    }
                    break;

                case PageType.StageSelect:
                    // 월드 탭 (1~10 고정 생성)
                    for (int w = 1; w <= 10; w++)
                    {
                        LobbyButtonData worldBtn = new LobbyButtonData();
                        worldBtn.buttonName = $"World_{w}";
                        worldBtn.buttonText = $"World {w}";
                        worldBtn.buttonRect = new Rect(100, 200 + (w - 1) * 70, 300, 60);
                        worldBtn.fontSize = 24;
                        worldBtn.actionType = ButtonActionType.CloseCurrentPage; // Dummy action for now
                        data.buttons.Add(worldBtn);
                    }

                    // 스테이지 그리드 (실제 등록된 StageData 기반)
                    string[] stageGuids = AssetDatabase.FindAssets("t:StageData");
                    int renderCount = 0;
                    foreach (string guid in stageGuids)
                    {
                        StageData sData = AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(guid));
                        if (sData == null) continue;

                        int sIdx = sData.stageIndex > 0 ? sData.stageIndex : (renderCount + 1);
                        int row = renderCount / 5;
                        int col = renderCount % 5;
                        
                        LobbyButtonData stageBtn = new LobbyButtonData();
                        stageBtn.buttonName = $"Stage_{sIdx}";
                        stageBtn.buttonText = $"Stage {sIdx}";
                        stageBtn.buttonRect = new Rect(500 + col * 250, 200 + row * 250, 200, 200);
                        stageBtn.fontSize = 28;
                        stageBtn.actionType = ButtonActionType.StartGame;
                        data.buttons.Add(stageBtn);
                        renderCount++;
                    }
                    if (renderCount == 0)
                    {
                        LobbyButtonData emptyBtn = new LobbyButtonData();
                        emptyBtn.buttonName = "EmptyLabel";
                        emptyBtn.buttonText = "No StageData Assets Found.";
                        emptyBtn.buttonRect = new Rect(600, 400, 800, 100);
                        emptyBtn.fontSize = 40;
                        data.buttons.Add(emptyBtn);
                    }
                    break;

                case PageType.Achievement:
                    // 전체 받기 버튼
                    LobbyButtonData claimAllBtn = new LobbyButtonData();
                    claimAllBtn.buttonName = "AchieveClaimAllButton";
                    claimAllBtn.buttonText = "Claim All";
                    claimAllBtn.buttonRect = new Rect(1350, 50, 200, 80);
                    claimAllBtn.fontSize = 28;
                    data.buttons.Add(claimAllBtn);

                    string[] achieveGuids = AssetDatabase.FindAssets("t:AchievementData");
                    int aCount = 0;
                    foreach (string guid in achieveGuids)
                    {
                        AchievementData achData = AssetDatabase.LoadAssetAtPath<AchievementData>(AssetDatabase.GUIDToAssetPath(guid));
                        if (achData == null) continue;

                        LobbyButtonData titleBtn = new LobbyButtonData();
                        titleBtn.buttonName = $"Achieve_{achData.achievementId}_Title";
                        titleBtn.buttonText = $"{achData.achievementName} : Target {achData.targetValue}";
                        titleBtn.buttonRect = new Rect(300, 200 + aCount * 150, 800, 100);
                        titleBtn.fontSize = 35;
                        data.buttons.Add(titleBtn);

                        LobbyButtonData claimBtn = new LobbyButtonData();
                        claimBtn.buttonName = $"Achieve_{achData.achievementId}_{achData.rewardGems}_Claim";
                        claimBtn.buttonText = $"💎 {achData.rewardGems}";
                        claimBtn.buttonRect = new Rect(1150, 200 + aCount * 150, 300, 100);
                        claimBtn.fontSize = 35;
                        data.buttons.Add(claimBtn);
                        aCount++;
                    }
                    if (aCount == 0)
                    {
                        LobbyButtonData emptyBtn = new LobbyButtonData();
                        emptyBtn.buttonName = "EmptyLabel";
                        emptyBtn.buttonText = "No AchievementData Assets Found.";
                        emptyBtn.buttonRect = new Rect(460, 400, 1000, 100);
                        emptyBtn.fontSize = 40;
                        data.buttons.Add(emptyBtn);
                    }
                    break;

                case PageType.Event:
                    string[] eventGuids = AssetDatabase.FindAssets("t:EventData");
                    int eCount = 0;
                    foreach (string guid in eventGuids)
                    {
                        EventData eData = AssetDatabase.LoadAssetAtPath<EventData>(AssetDatabase.GUIDToAssetPath(guid));
                        if (eData == null) continue;

                        LobbyButtonData eventMapBtn = new LobbyButtonData();
                        eventMapBtn.buttonName = $"Event_{eData.eventId}";
                        eventMapBtn.buttonText = $"{eData.eventName}\n(Click to Enter)";
                        eventMapBtn.buttonRect = new Rect(460, 250 + eCount * 250, 1000, 200);
                        eventMapBtn.fontSize = 40;
                        eventMapBtn.buttonImage = eData.bannerImage;
                        eventMapBtn.actionType = ButtonActionType.StartGame;
                        data.buttons.Add(eventMapBtn);
                        eCount++;
                    }
                    if (eCount == 0)
                    {
                        LobbyButtonData emptyBtn = new LobbyButtonData();
                        emptyBtn.buttonName = "EmptyLabel";
                        emptyBtn.buttonText = "No EventData Assets Found.";
                        emptyBtn.buttonRect = new Rect(460, 400, 1000, 100);
                        emptyBtn.fontSize = 40;
                        data.buttons.Add(emptyBtn);
                    }
                    break;

                case PageType.GameOption:
                    string[] options = { "BGM : ON", "SFX : ON", "PUSH ALARM : OFF" };
                    for (int i = 0; i < options.Length; i++)
                    {
                        LobbyButtonData optBtn = new LobbyButtonData();
                        optBtn.buttonName = $"Option_{i}";
                        optBtn.buttonText = options[i];
                        optBtn.buttonRect = new Rect(660, 300 + i * 150, 600, 100);
                        optBtn.fontSize = 35;
                        data.buttons.Add(optBtn);
                    }
                    break;

                case PageType.Shop:
                    LobbyButtonData gemLabel = new LobbyButtonData();
                    gemLabel.buttonName = "GemCountLabel";
                    gemLabel.buttonText = "💎 GEMS : 1,000";
                    gemLabel.buttonRect = new Rect(100, 50, 300, 100);
                    gemLabel.fontSize = 30;
                    data.buttons.Add(gemLabel);

                    string[] shopGuids = AssetDatabase.FindAssets("t:ShopItemData");
                    int sCount = 0;
                    foreach (string guid in shopGuids)
                    {
                        ShopItemData shopData = AssetDatabase.LoadAssetAtPath<ShopItemData>(AssetDatabase.GUIDToAssetPath(guid));
                        if (shopData == null) continue;

                        int row = sCount / 3;
                        int col = sCount % 3;
                        LobbyButtonData itemBtn = new LobbyButtonData();
                        itemBtn.buttonName = $"ShopItem_{shopData.itemId}";
                        itemBtn.buttonText = $"{shopData.itemName}\n\n({shopData.costGems} 💎 / {shopData.costGold} G)";
                        itemBtn.buttonRect = new Rect(300 + col * 450, 250 + row * 350, 350, 300);
                        itemBtn.fontSize = 28;
                        itemBtn.buttonImage = shopData.itemIcon;
                        data.buttons.Add(itemBtn);
                        sCount++;
                    }
                    if (sCount == 0)
                    {
                        LobbyButtonData emptyBtn = new LobbyButtonData();
                        emptyBtn.buttonName = "EmptyLabel";
                        emptyBtn.buttonText = "No ShopItemData Assets Found.";
                        emptyBtn.buttonRect = new Rect(460, 400, 1000, 100);
                        emptyBtn.fontSize = 40;
                        data.buttons.Add(emptyBtn);
                    }
                    break;

                case PageType.Leaderboard:
                    LobbyButtonData rankTitle = new LobbyButtonData();
                    rankTitle.buttonName = "RankTitle";
                    rankTitle.buttonText = "Top 10 Players";
                    rankTitle.buttonRect = new Rect(460, 200, 1000, 100);
                    rankTitle.fontSize = 40;
                    data.buttons.Add(rankTitle);
                    
                    for (int i = 1; i <= 5; i++)
                    {
                        LobbyButtonData playerBtn = new LobbyButtonData();
                        playerBtn.buttonName = $"Rank_{i}";
                        playerBtn.buttonText = $"{i}. Player{i * 123} - Score: {10000 - i * 500}";
                        playerBtn.buttonRect = new Rect(460, 200 + i * 120, 1000, 100);
                        playerBtn.fontSize = 35;
                        data.buttons.Add(playerBtn);
                    }
                    break;
            }
        }
    }
}
