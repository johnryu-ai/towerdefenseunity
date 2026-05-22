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

        // 업그레이드 UI 및 타워 관리용
        private Entities.TowerController selectedBuiltTower;
        private bool showUpgradePopup = false;
        private bool isTargetingMode = false;
        private System.Collections.Generic.Dictionary<Vector2Int, Entities.TowerController> builtTowers = new System.Collections.Generic.Dictionary<Vector2Int, Entities.TowerController>();

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
            if (showTowerPopup || (showUpgradePopup && !isTargetingMode))
            {
                float scaleX = 2340f / Screen.width;
                float scaleY = 1080f / Screen.height;
                Vector2 guiMousePos = new Vector2(mouseScreenPos.x * scaleX, (Screen.height - mouseScreenPos.y) * scaleY);

                Rect targetRect = new Rect(0, 0, 0, 0);
                if (showTowerPopup)
                {
                    targetRect = GameplayUISettings.Instance != null ? GameplayUISettings.Instance.leftPanelRect : new Rect(0, 135, 150, 945);
                }
                else if (showUpgradePopup)
                {
                    targetRect = GameplayUISettings.Instance != null ? GameplayUISettings.Instance.rightPanelRect : new Rect(2190, 135, 150, 945);
                }

                if (showTowerPopup && !targetRect.Contains(guiMousePos))
                {
                    showTowerPopup = false;
                }
                else if (showUpgradePopup && !targetRect.Contains(guiMousePos))
                {
                    showUpgradePopup = false;
                    if (selectedBuiltTower != null) selectedBuiltTower.Deselect();
                    selectedBuiltTower = null;
                }
                return;
            }

            Vector2 worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            // 타겟팅 모드 처리
            if (isTargetingMode && selectedBuiltTower != null)
            {
                Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
                if (hits.Length > 0)
                {
                    Entities.MonsterController closestMonster = null;
                    Entities.ObstacleController closestObstacle = null;
                    float minDistance = float.MaxValue;

                    foreach (var hit in hits)
                    {
                        float dist = Vector2.Distance(worldPos, hit.transform.position);
                        if (dist < minDistance)
                        {
                            var monster = hit.GetComponent<Entities.MonsterController>();
                            if (monster != null && selectedBuiltTower.IsValidTargetType(monster.GetFlyType()))
                            {
                                minDistance = dist;
                                closestMonster = monster;
                                closestObstacle = null;
                            }
                            else
                            {
                                var obstacle = hit.GetComponent<Entities.ObstacleController>();
                                if (obstacle != null)
                                {
                                    minDistance = dist;
                                    closestObstacle = obstacle;
                                    closestMonster = null;
                                }
                            }
                        }
                    }

                    if (closestMonster != null)
                    {
                        selectedBuiltTower.SetPriorityTarget(closestMonster);
                        isTargetingMode = false;
                        DeselectBuiltTower();
                        return;
                    }
                    else if (closestObstacle != null)
                    {
                        selectedBuiltTower.SetPriorityTarget(closestObstacle);
                        isTargetingMode = false;
                        DeselectBuiltTower();
                        return;
                    }
                }
                
                // 빈 바닥 클릭 시 타겟팅 모드 해제
                isTargetingMode = false;
                DeselectBuiltTower();
                return;
            }
            
            if (Map.MapController.Instance != null && GameManager.Instance.currentMapData != null)
            {
                int x = Mathf.RoundToInt((worldPos.x - Map.MapController.Instance.offsetX) / Map.MapController.TILE_SIZE);
                int y = Mathf.RoundToInt((worldPos.y - Map.MapController.Instance.offsetY) / Map.MapController.TILE_SIZE);
                
                if (x >= 0 && x < GameManager.Instance.currentMapData.gridWidth && 
                    y >= 0 && y < GameManager.Instance.currentMapData.gridHeight)
                {
                    TileType type = GameManager.Instance.currentMapData.GetTileAt(x, y);
                    if (type == TileType.Buildable)
                    {
                        // 건설 팝업 열기
                        showTowerPopup = true;
                        showUpgradePopup = false;
                        if (selectedBuiltTower != null) selectedBuiltTower.Deselect();
                        selectedBuiltTower = null;

                        popupPosition = mouseScreenPos;
                        clickedX = x;
                        clickedY = y;
                    }
                    else if (type == TileType.NonBuildable)
                    {
                        // 이미 지어진 타워가 있는지 확인
                        Vector2Int gridPos = new Vector2Int(x, y);
                        if (builtTowers.ContainsKey(gridPos))
                        {
                            SelectBuiltTower(builtTowers[gridPos]);
                        }
                    }
                }
            }
        }

        public void SelectBuiltTower(Entities.TowerController tower)
        {
            if (selectedBuiltTower != null && selectedBuiltTower != tower)
            {
                selectedBuiltTower.Deselect();
            }
            selectedBuiltTower = tower;
            selectedBuiltTower.Select(); // 타워 시각 효과 켜기
            showUpgradePopup = true;
            showTowerPopup = false; // 건설 팝업은 닫음
            
            // 타워 머리 위에 팝업 띄우기
            Vector3 screenPos = Camera.main.WorldToScreenPoint(tower.transform.position);
            popupPosition = new Vector2(screenPos.x, screenPos.y);
        }

        public void DeselectBuiltTower()
        {
            if (selectedBuiltTower != null)
            {
                selectedBuiltTower.Deselect();
            }
            showUpgradePopup = false;
            selectedBuiltTower = null;
            isTargetingMode = false; // 타겟팅 모드도 확실히 종료
        }

        private Rect popupRect;

        private void OnGUI()
        {
            Vector2 ratio = new Vector2(Screen.width / 2340f, Screen.height / 1080f);
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(ratio.x, ratio.y, 1f));

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 24, fontStyle = FontStyle.Bold };
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };

            if (showTowerPopup)
            {
                popupRect = GameplayUISettings.Instance != null ? GameplayUISettings.Instance.leftPanelRect : new Rect(0, 135, 150, 945); // Left Panel
                GUILayout.BeginArea(popupRect, "Build", GUI.skin.window);
                
                GUILayout.Space(10);

                var availableTowers = GameManager.Instance.currentMapData.config.availableTowers;
                int validTowers = 0;
                if (availableTowers != null)
                {
                    foreach (var tower in availableTowers)
                    {
                        if (tower != null && UserDataManager.Instance != null && UserDataManager.Instance.IsTowerUnlocked(tower.towerId)) validTowers++;
                    }
                }
                
                if (validTowers == 0)
                {
                    GUILayout.Label("No Unlocked Towers", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                }
                else
                {
                    foreach (var tower in availableTowers)
                    {
                        if (tower == null) continue;
                        if (UserDataManager.Instance != null && !UserDataManager.Instance.IsTowerUnlocked(tower.towerId)) continue;
                        
                        int cost = tower.upgradeTiers.Count > 0 ? tower.upgradeTiers[0].buildOrUpgradeCost : 0;
                        if (GUILayout.Button($"{tower.towerName}\n{cost}G", buttonStyle, GUILayout.Height(60)))
                        {
                            selectedTowerToBuild = tower;
                            TryBuildTower(clickedX, clickedY);
                            showTowerPopup = false;
                        }
                        GUILayout.Space(10);
                    }
                }
                
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close", buttonStyle, GUILayout.Height(40))) showTowerPopup = false;
                
                GUILayout.EndArea();
            }
            else if (showUpgradePopup && selectedBuiltTower != null)
            {
                popupRect = GameplayUISettings.Instance != null ? GameplayUISettings.Instance.rightPanelRect : new Rect(2190, 135, 150, 945); // Right Panel
                GUILayout.BeginArea(popupRect, "Manage", GUI.skin.window);
                
                TowerData data = selectedBuiltTower.GetData();
                int currentTier = selectedBuiltTower.GetCurrentTierIndex();
                bool isMaxLevel = currentTier >= data.upgradeTiers.Count - 1;

                GUILayout.Space(10);
                GUILayout.Label($"{data.towerName}\nLv {currentTier + 1}", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18 });
                GUILayout.Space(20);
                
                // Upgrade
                if (!isMaxLevel)
                {
                    int upgradeCost = data.upgradeTiers[currentTier + 1].buildOrUpgradeCost;
                    bool canAfford = GameManager.Instance.CurrentGold >= upgradeCost;
                    
                    GUI.enabled = canAfford;
                    if (GUILayout.Button($"Upgrade\n-{upgradeCost}G", buttonStyle, GUILayout.Height(60)))
                    {
                        selectedBuiltTower.UpgradeTower();
                    }
                    GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = false;
                    GUILayout.Button("Max Level", buttonStyle, GUILayout.Height(60));
                    GUI.enabled = true;
                }

                GUILayout.Space(10);

                // Sell
                int sellPrice = data.upgradeTiers[currentTier].sellPrice;
                GUI.color = Color.red;
                if (GUILayout.Button($"Sell\n+{sellPrice}G", buttonStyle, GUILayout.Height(60)))
                {
                    GameManager.Instance.AddGold(sellPrice);
                    
                    int gx = selectedBuiltTower.GridX;
                    int gy = selectedBuiltTower.GridY;
                    
                    GameManager.Instance.currentMapData.SetTileAt(gx, gy, TileType.Buildable);
                    Map.MapController.Instance.UpdateTileColor(gx, gy);
                    
                    builtTowers.Remove(new Vector2Int(gx, gy));
                    Destroy(selectedBuiltTower.gameObject);
                    DeselectBuiltTower();
                }
                GUI.color = Color.white;

                GUILayout.Space(10);

                // Target First
                GUI.color = isTargetingMode ? Color.yellow : Color.cyan;
                string targetBtnText = isTargetingMode ? "Select\nTarget" : "Target:\nFirst";
                if (GUILayout.Button(targetBtnText, buttonStyle, GUILayout.Height(60)))
                {
                    isTargetingMode = !isTargetingMode;
                    if (isTargetingMode) Debug.Log("우선 공격 대상을 선택하세요 (몬스터/장애물)");
                }
                GUI.color = Color.white;

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close", buttonStyle, GUILayout.Height(40))) 
                {
                    selectedBuiltTower.Deselect();
                    DeselectBuiltTower();
                }

                GUILayout.EndArea();
            }

            GUI.matrix = oldMatrix;
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
                        towerController.Initialize(selectedTowerToBuild, x, y);
                        builtTowers[new Vector2Int(x, y)] = towerController;
                    }

                    GameManager.Instance.currentMapData.SetTileAt(x, y, TileType.NonBuildable);
                    Map.MapController.Instance.UpdateTileColor(x, y);
                    
                    selectedTowerToBuild = null; 
                }
            }
        }
    }
}
