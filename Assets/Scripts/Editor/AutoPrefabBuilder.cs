using UnityEngine;
using UnityEditor;
using TDF.Runtime.Entities;
using TDF.Core.Data;
using System.IO;

namespace TDF.Editor
{
    [InitializeOnLoad]
    public class AutoPrefabBuilder
    {
        static AutoPrefabBuilder()
        {
            EditorApplication.delayCall += CheckAndGeneratePrefabs;
        }

        static void CheckAndGeneratePrefabs()
        {
            bool createdAny = false;

            if (!AssetDatabase.IsValidFolder("Assets/TD/monster"))
            {
                AssetDatabase.CreateFolder("Assets/TD", "monster");
            }

            string monsterPrefabPath = "Assets/TD/monster/monster1.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(monsterPrefabPath) == null)
            {
                // 빈 게임 오브젝트 생성
                GameObject obj = new GameObject("monster1_prefab");
                
                // 스프라이트 렌더러 부착 및 시각적 설정
                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"); // 기본 사각형
                sr.color = Color.red; // 눈에 띄게 빨간색으로
                sr.sortingOrder = 10;
                
                // ★ 가장 중요한 핵심 스크립트 부착 ★
                obj.AddComponent<MonsterController>();
                
                // 프리팹으로 저장
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, monsterPrefabPath);
                GameObject.DestroyImmediate(obj);

                // 생성한 프리팹을 고객님의 monster1.asset 데이터에 자동으로 연결
                MonsterData data = AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/TD/monster/monster1.asset");
                if (data != null)
                {
                    if (data.assets == null) data.assets = new MonsterAssets();
                    data.assets.prefab = prefab;
                    EditorUtility.SetDirty(data);
                }
                
                createdAny = true;
                Debug.Log("✅ [성공] monster1.prefab을 자동 생성하고 MonsterController 스크립트를 부착했습니다!");
            }

            // 2. 타워 프리팹 확인 및 생성
            if (!AssetDatabase.IsValidFolder("Assets/TD/tower"))
            {
                AssetDatabase.CreateFolder("Assets/TD", "tower");
            }

            string towerPrefabPath = "Assets/TD/tower/tower1.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(towerPrefabPath) == null)
            {
                GameObject obj = new GameObject("tower1_prefab");
                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"); 
                sr.color = Color.cyan; // 타워는 청록색
                sr.sortingOrder = 5;
                
                obj.AddComponent<TowerController>();
                
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, towerPrefabPath);
                GameObject.DestroyImmediate(obj);

                TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/TD/tower/tower1.asset");
                if (data != null)
                {
                    if (data.assets == null) data.assets = new TowerAssets();
                    data.assets.prefab = prefab;
                    EditorUtility.SetDirty(data);
                }
                
                createdAny = true;
                Debug.Log("✅ [성공] tower1.prefab을 자동 생성하고 TowerController 스크립트를 부착했습니다!");
            }

            // 3. 발사체 프리팹 확인 및 생성
            string projPrefabPath = "Assets/TD/tower/projectile1.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(projPrefabPath) == null)
            {
                GameObject obj = new GameObject("projectile1_prefab");
                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"); // 둥근 모양
                sr.color = Color.yellow; // 노란색 총알
                sr.sortingOrder = 15;
                
                obj.AddComponent<ProjectileController>();
                
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, projPrefabPath);
                GameObject.DestroyImmediate(obj);

                TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/TD/tower/tower1.asset");
                if (data != null)
                {
                    if (data.upgradeTiers == null) data.upgradeTiers = new System.Collections.Generic.List<TowerUpgradeTier>();
                    if (data.upgradeTiers.Count == 0)
                    {
                        data.upgradeTiers.Add(new TowerUpgradeTier { range = 3f, attackSpeed = 1f, damage = 10f, buildOrUpgradeCost = 50 });
                    }
                    if (data.assets == null) data.assets = new TowerAssets();
                    data.assets.projectilePrefab = prefab;
                    EditorUtility.SetDirty(data);
                }
                
                createdAny = true;
                Debug.Log("✅ [성공] projectile1.prefab을 자동 생성하고 ProjectileController 스크립트를 부착했습니다!");
            }

            if (createdAny)
            {
                AssetDatabase.SaveAssets();
            }
        }
    }
}
