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

            if (createdAny)
            {
                AssetDatabase.SaveAssets();
            }
        }
    }
}
