using UnityEngine;
using UnityEngine.InputSystem;
using TDF.Core.Data;

namespace TDF.Runtime.Managers
{
    public class BuildManager : MonoBehaviour
    {
        public static BuildManager Instance { get; private set; }

        [Header("Test Towers (Press 1, 2, 3...)")]
        public System.Collections.Generic.List<TowerData> availableTowers;

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
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame && availableTowers.Count > 0) SelectTowerToBuild(availableTowers[0]);
                if (Keyboard.current.digit2Key.wasPressedThisFrame && availableTowers.Count > 1) SelectTowerToBuild(availableTowers[1]);
                if (Keyboard.current.digit3Key.wasPressedThisFrame && availableTowers.Count > 2) SelectTowerToBuild(availableTowers[2]);
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
            
            // UI를 누른 건지 판별하기 위해 (단순 테스트용이므로 임시로 넘김)
            
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
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

        private void OnGUI()
        {
            if (showTowerPopup)
            {
                int validTowers = 0;
                foreach (var tower in availableTowers)
                {
                    if (tower != null) validTowers++;
                }
                
                float popupHeight = 40f + (validTowers * 30f) + 40f; // 제목바 + 버튼들 + 취소버튼 + 여유공간

                // 화면 바깥으로 나가지 않도록 조정
                float px = Mathf.Clamp(popupPosition.x, 0, Screen.width - 160f);
                float py = Mathf.Clamp(Screen.height - popupPosition.y, 0, Screen.height - popupHeight);

                Rect rect = new Rect(px, py, 160f, popupHeight);
                GUILayout.BeginArea(rect, "Select Tower", GUI.skin.window);
                
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
