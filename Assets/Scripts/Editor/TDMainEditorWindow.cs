using UnityEditor;
using UnityEngine;

namespace TDF.Editor
{
    using Modules;

    public class TDMainEditorWindow : EditorWindow
    {
        private enum Tab { Map, Tower, Monster, Wave, Stage, Campaign, AssetMeta, PageUI, LobbyUI, Attribute }
        private Tab currentTab = Tab.Map;

        // 모듈 인스턴스
        private MapEditorModule mapEditorModule = new MapEditorModule();
        private TowerEditorModule towerEditorModule = new TowerEditorModule();
        private MonsterEditorModule monsterEditorModule = new MonsterEditorModule();
        private WaveEditorModule waveEditorModule = new WaveEditorModule();
        private StageEditorModule stageEditorModule = new StageEditorModule();
        private CampaignEditorModule campaignEditorModule = new CampaignEditorModule();
        private PageEditorModule pageEditorModule = new PageEditorModule();
        private LobbyUIEditorModule lobbyUIEditorModule = new LobbyUIEditorModule();
        private AttributeEditorModule attributeEditorModule = new AttributeEditorModule();

        [MenuItem("Tools/TDF/Tower Defense Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<TDMainEditorWindow>("TDF Editor");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();

            // 좌측 탭 메뉴 (사이드바)
            DrawSidebar();

            // 우측 메인 콘텐츠
            DrawMainContent();

            GUILayout.EndHorizontal();
        }

        private void DrawSidebar()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(150), GUILayout.ExpandHeight(true));

            GUILayout.Label("TDF Editor Menu", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Toggle(currentTab == Tab.Map, "Map Editor", "Button")) currentTab = Tab.Map;
            if (GUILayout.Toggle(currentTab == Tab.Tower, "Tower Editor", "Button")) currentTab = Tab.Tower;
            if (GUILayout.Toggle(currentTab == Tab.Monster, "Monster Editor", "Button")) currentTab = Tab.Monster;
            if (GUILayout.Toggle(currentTab == Tab.Wave, "Wave Editor", "Button")) currentTab = Tab.Wave;
            if (GUILayout.Toggle(currentTab == Tab.Stage, "Stage Editor", "Button")) currentTab = Tab.Stage;
            if (GUILayout.Toggle(currentTab == Tab.Campaign, "Campaign Editor", "Button")) currentTab = Tab.Campaign;
            if (GUILayout.Toggle(currentTab == Tab.PageUI, "Page & UI Editor", "Button")) currentTab = Tab.PageUI;
            if (GUILayout.Toggle(currentTab == Tab.LobbyUI, "Visual UI Editor", "Button")) currentTab = Tab.LobbyUI;
            if (GUILayout.Toggle(currentTab == Tab.Attribute, "Attribute Settings", "Button")) currentTab = Tab.Attribute;
            if (GUILayout.Toggle(currentTab == Tab.AssetMeta, "Asset & Meta", "Button")) currentTab = Tab.AssetMeta;

            GUILayout.EndVertical();
        }

        private void DrawMainContent()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            switch (currentTab)
            {
                case Tab.Map:
                    mapEditorModule.Draw();
                    break;
                case Tab.Tower:
                    towerEditorModule.Draw();
                    break;
                case Tab.Monster:
                    monsterEditorModule.Draw();
                    break;
                case Tab.Wave:
                    waveEditorModule.Draw();
                    break;
                case Tab.Stage:
                    stageEditorModule.Draw();
                    break;
                case Tab.Campaign:
                    campaignEditorModule.Draw();
                    break;
                case Tab.PageUI:
                    pageEditorModule.Draw();
                    break;
                case Tab.LobbyUI:
                    lobbyUIEditorModule.Draw();
                    break;
                case Tab.Attribute:
                    attributeEditorModule.Draw();
                    break;
                case Tab.AssetMeta:
                    GUILayout.Label("Asset & Meta Editor", EditorStyles.boldLabel);
                    break;
            }

            GUILayout.EndVertical();
        }
    }
}
