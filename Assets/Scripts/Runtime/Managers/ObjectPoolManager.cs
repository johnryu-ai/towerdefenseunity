using System.Collections.Generic;
using UnityEngine;

namespace TDF.Runtime.Managers
{
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        // 프리팹 인스턴스 ID를 키로 사용하는 풀 딕셔너리
        private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();
        private Transform poolContainer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                poolContainer = new GameObject("ObjectPoolContainer").transform;
                poolContainer.SetParent(this.transform);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            int prefabId = prefab.GetInstanceID();

            if (!poolDictionary.ContainsKey(prefabId))
            {
                poolDictionary[prefabId] = new Queue<GameObject>();
            }

            GameObject objectToSpawn = null;

            // 풀에 사용 가능한 오브젝트가 있는지 확인
            while (poolDictionary[prefabId].Count > 0)
            {
                GameObject obj = poolDictionary[prefabId].Dequeue();
                if (obj != null) // 혹시 파괴된 오브젝트가 들어있을 경우 대비
                {
                    objectToSpawn = obj;
                    break;
                }
            }

            // 풀에 오브젝트가 없으면 새로 생성
            if (objectToSpawn == null)
            {
                objectToSpawn = Instantiate(prefab);
                // 풀링 관리를 위해 생성된 오브젝트에 식별자 부착 로직이 필요하다면 여기에 추가
            }

            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.SetActive(true);

            return objectToSpawn;
        }

        public void ReturnToPool(GameObject obj, GameObject prefabOrigin)
        {
            if (obj == null || prefabOrigin == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(poolContainer);

            int prefabId = prefabOrigin.GetInstanceID();

            if (!poolDictionary.ContainsKey(prefabId))
            {
                poolDictionary[prefabId] = new Queue<GameObject>();
            }

            poolDictionary[prefabId].Enqueue(obj);
        }
    }
}
