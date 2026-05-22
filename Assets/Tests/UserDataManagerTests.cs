using NUnit.Framework;
using UnityEngine;
using TDF.Runtime.Managers;

public class UserDataManagerTests
{
    private GameObject _managerGo;
    private UserDataManager _manager;

    [SetUp]
    public void SetUp()
    {
        // UserDataManager를 씬에 배치
        _managerGo = new GameObject("UserDataManagerTest");
        _manager = _managerGo.AddComponent<UserDataManager>();
    }

    [TearDown]
    public void TearDown()
    {
        // 테스트가 끝나면 싱글톤 인스턴스 파괴 및 오브젝트 삭제
        if (_managerGo != null)
        {
            Object.DestroyImmediate(_managerGo);
        }
        
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.ResetAllData();
        }
    }

    [Test]
    public void Test_TowerUnlockAndCheck()
    {
        string testTowerId = "test_fire_tower";
        
        // 초기 상태에는 언락되지 않은 상태
        Assert.IsFalse(_manager.IsTowerUnlocked(testTowerId));

        // 타워 언락 처리
        _manager.UnlockTower(testTowerId);

        // 정상적으로 언락되었는지 확인
        Assert.IsTrue(_manager.IsTowerUnlocked(testTowerId));
    }
    
    [Test]
    public void Test_SaveDataReset()
    {
        string testTowerId = "test_ice_tower";
        _manager.UnlockTower(testTowerId);
        
        Assert.IsTrue(_manager.IsTowerUnlocked(testTowerId));
        
        // 모든 유저 데이터 리셋
        _manager.ResetAllData();
        
        // 리셋 후 잠금 상태로 돌아왔는지 확인
        Assert.IsFalse(_manager.IsTowerUnlocked(testTowerId));
    }
}
