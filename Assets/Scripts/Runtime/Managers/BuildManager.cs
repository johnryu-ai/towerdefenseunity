using UnityEngine;
using UnityEngine.InputSystem;
using TDF.Core.Data;

namespace TDF.Runtime.Managers
{
    public class BuildManager : MonoBehaviour
    {
        public static BuildManager Instance { get; private set; }

        private TowerData selectedTowerToBuild;

        // UI 팝업용 변수
        private bool showTowerPopup = false;
        private Vector2 popupPosition;
        private int clickedX, clickedY;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Ready) return;

            // 타워 선택 단축키 (키보드 1, 2, 3...)
            if (Keyboard.current != null && GameManager.Instance.currentMapData != null)
            {
                var mapTowers = GameManager.Instance.currentMapData.config.availableTowers;
                if (mapTowers != null)
                {
                    if (Keyboard.current.digit1Key.wasPressedThisFrame && mapTowers.Count > 0) selectedTowerToBuild = mapTowers[0];
                    if (Keyboard.current.digit2Key.wasPressedThisFrame && mapTowers.Count > 1) selectedTowerToBuild = mapTowers[1];
                    if (Keyboard.current.digit3Key.wasPressedThisFrame && mapTowers.Count > 2) selectedTowerToBuild = mapTowers[2];
                }
            }

            // New Input System 마우스 클릭 처리
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleClick();
            }
        }

        public void SelectTowerToBuild(TowerData towerData)
        {
            selectedTowerToBuild = towerData;
            Debug.Log($"건설 준비: {towerData.towerName}");
        }

        private void HandleClick()
        {
            if (Mouse.current == null) return;
            
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            
            // 팝업이 띄워져 있을 때 클릭 처리
            if (showTowerPopup)
            {
                Vector2 guiMousePos = new Vector2(mouseScreenPos.x, Screen.height - mouseScreenPos.y);
                if (!popupRect.Contains(guiMousePos))
                {
                    showTowerPopup = false; // 팝업 바깥을 누르면 닫힘
                }
                return; // 팝업이 열려있으면 맵 클릭 무시
            }

            Vector2 worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            
            if (Map.MapController.Instance != null && GameManager.Instance.currentMapData != null)
            {
                int x = Mathf.RoundToInt((worldPos.x - Map.MapController.Instance.offsetX) / Map.MapController.TILE_SIZE);
                int y = Mathf.RoundToInt(-(worldPos.y - Map.MapController.Instance.offsetY) / Map.MapController.TILE_SIZE);
                
                if (x >= 0 && x < GameManager.Instance.currentMapData.gridWidth && 
                    y >= 0 && y < GameManager.Instance.currentMapData.gridHeight)
                {
                    TileType type = GameManager.Instance.currentMapData.GetTileAt(x, y);
                    if (type == TileType.Buildable)
                    {
                        // 팝업 열기
                        showTowerPopup = true;
                        popupPosition = mouseScreenPos;
                        clickedX = x;
                        clickedY = y;
                    }
                }
            }
        }

        private Rect popupRect;

        private void OnGUI()
        {
            if (showTowerPopup)
            {
                var availableTowers = GameManager.Instance.currentMapData.config.availableTowers;
                int validTowers = 0;
                if (availableTowers != null)
                {
                    foreach (var tower in availableTowers)
                    {
                        if (tower != null) validTowers++;
                    }
                }
                
                float popupHeight = 40f + (validTowers * 30f) + 40f; 

                float px = Mathf.Clamp(popupPosition.x, 0, Screen.width - 160f);
                float py = Mathf.Clamp(Screen.height - popupPosition.y, 0, Screen.height - popupHeight);

                popupRect = new Rect(px, py, 160f, popupHeight);
                GUILayout.BeginArea(popupRect, "Select Tower", GUI.skin.window);
                
                if (validTowers == 0)
                {
                    GUILayout.Label("사용 가능한 타워가\n없습니다.\n(MapData 확인 필요)");
                }
                else
                {
                    foreach (var tower in availableTowers)
                    {
                        if (tower == null) continue;
                        if (GUILayout.Button(tower.towerName, GUILayout.Height(25f)))
                        {
                            selectedTowerToBuild = tower;
                            TryBuildTower(clickedX, clickedY);
                            showTowerPopup = false;
                        }
                    }
                }
                
                GUILayout.Space(5);
                if (GUILayout.Button("Cancel", GUILayout.Height(25f))) showTowerPopup = false;
                
                GUILayout.EndArea();
            }
        }

        private void TryBuildTower(int x, int y)
        {
            if (selectedTowerToBuild == null) return;
            if (selectedTowerToBuild.upgradeTiers.Count == 0) return;

            int cost = selectedTowerToBuild.upgradeTiers[0].buildOrUpgradeCost;
            if (GameManager.Instance.UseGold(cost))
            {
                if (selectedTowerToBuild.assets != null && selectedTowerToBuild.assets.prefab != null)
                {
                    Vector3 buildPos = Map.MapController.Instance.GetWorldPosition(x, y);
                    GameObject towerObj = Instantiate(selectedTowerToBuild.assets.prefab, buildPos, Quaternion.identity);
                    var towerController = towerObj.GetComponent<Entities.TowerController>();
                    if (towerController != null)
                    {
                        towerController.Initialize(selectedTowerToBuild);
                    }

                    GameManager.Instance.currentMapData.SetTileAt(x, y, TileType.NonBuildable);
                    Map.MapController.Instance.UpdateTileColor(x, y);
                    
                    selectedTowerToBuild = null; 
                }
            }
        }
    }
}
