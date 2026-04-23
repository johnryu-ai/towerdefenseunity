using UnityEngine;
using TDF.Core.Data;

namespace TDF.Runtime.Managers
{
    public class BuildManager : MonoBehaviour
    {
        public static BuildManager Instance { get; private set; }

        private TowerData selectedTowerToBuild;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.Ready) return;

            // PC 환경 마우스 클릭 (모바일 터치로 확장 가능)
            if (Input.GetMouseButtonDown(0))
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
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                // 타일 클릭 처리
                if (hit.collider.CompareTag("BuildableTile"))
                {
                    TryBuildTower(hit.collider.transform);
                }
                // 기존 설치된 타워 클릭 처리 (업그레이드/판매 UI 띄우기)
                else if (hit.collider.CompareTag("Tower"))
                {
                    var tower = hit.collider.GetComponent<Entities.TowerController>();
                    if (tower != null)
                    {
                        // TODO: 타워 옵션 UI 팝업 띄우기 (업그레이드 또는 판매)
                        Debug.Log("타워 클릭됨. 업그레이드 UI를 띄워야 합니다.");
                    }
                }
            }
        }

        private void TryBuildTower(Transform tileTransform)
        {
            if (selectedTowerToBuild == null)
            {
                Debug.Log("선택된 타워가 없습니다.");
                return;
            }

            if (selectedTowerToBuild.upgradeTiers.Count == 0) return;

            int cost = selectedTowerToBuild.upgradeTiers[0].buildOrUpgradeCost;
            if (GameManager.Instance.UseGold(cost))
            {
                if (selectedTowerToBuild.assets != null && selectedTowerToBuild.assets.prefab != null)
                {
                    // 타일에 딱 맞게 중앙에 설치 (타일의 자식으로 넣거나 좌표 일치)
                    GameObject towerObj = Instantiate(selectedTowerToBuild.assets.prefab, tileTransform.position, Quaternion.identity);
                    var towerController = towerObj.GetComponent<Entities.TowerController>();
                    if (towerController != null)
                    {
                        towerController.Initialize(selectedTowerToBuild);
                    }

                    // 타일에 이미 타워가 있음을 표시 (다시 짓지 못하게)
                    tileTransform.tag = "Untagged"; 
                    
                    Debug.Log($"{selectedTowerToBuild.towerName} 건설 완료!");
                    
                    // 연속 건설 해제 (옵션)
                    selectedTowerToBuild = null; 
                }
            }
            else
            {
                Debug.Log("골드가 부족합니다.");
            }
        }
    }
}
