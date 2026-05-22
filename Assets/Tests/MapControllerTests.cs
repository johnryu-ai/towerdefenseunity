using NUnit.Framework;
using UnityEngine;
using TDF.Runtime.Map;
using TDF.Core.Data;

public class MapControllerTests
{
    private GameObject _mapControllerGo;
    private MapController _mapController;

    [SetUp]
    public void SetUp()
    {
        _mapControllerGo = new GameObject("MapControllerTest");
        _mapController = _mapControllerGo.AddComponent<MapController>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_mapControllerGo != null)
        {
            Object.DestroyImmediate(_mapControllerGo);
        }
    }

    [Test]
    public void Test_MapControllerInstanceSetup()
    {
        Assert.IsNotNull(MapController.Instance);
    }

    [Test]
    public void Test_GenerateMapAndCoordinateCalculation()
    {
        // 테스트용 MapData 동적 생성
        MapData testMapData = ScriptableObject.CreateInstance<MapData>();
        testMapData.ResizeGrid(5, 5); // 5x5 그리드로 설정
        
        // 맵 타일 생성
        _mapController.GenerateMap(testMapData);

        // MapController.TILE_SIZE가 1이므로 (2, 2) 타일의 월드 좌표는 (2f, 2f, 0f)가 되어야 함
        Vector3 worldPos = _mapController.GetWorldPosition(2, 2);
        
        Assert.AreEqual(2f, worldPos.x);
        Assert.AreEqual(2f, worldPos.y);
        Assert.AreEqual(0f, worldPos.z);
    }

    [Test]
    public void Test_GenerateMapAndCoordinateCalculation_14x8()
    {
        // 14x8 크기 맵 생성 테스트
        MapData testMapData = ScriptableObject.CreateInstance<MapData>();
        testMapData.ResizeGrid(14, 8);
        
        _mapController.GenerateMap(testMapData);

        // (0,0) 좌하단 타일 위치가 (0,0)인지 검증
        Vector3 bottomLeft = _mapController.GetWorldPosition(0, 0);
        Assert.AreEqual(0f, bottomLeft.x);
        Assert.AreEqual(0f, bottomLeft.y);

        // (13,7) 우상단 타일 위치가 (13,7)인지 검증
        Vector3 topRight = _mapController.GetWorldPosition(13, 7);
        Assert.AreEqual(13f, topRight.x);
        Assert.AreEqual(7f, topRight.y);

        // 카메라 종횡비 및 중심 고정 검증
        if (Camera.main != null)
        {
            // orthographicSize = 8 / 2 = 4f
            Assert.AreEqual(4f, Camera.main.orthographicSize);
            
            // 카메라 위치 X = 14/2 - 0.5 = 6.5f
            // 카메라 위치 Y = 8/2 - 0.5 = 3.5f
            Assert.AreEqual(6.5f, Camera.main.transform.position.x);
            Assert.AreEqual(3.5f, Camera.main.transform.position.y);
        }
    }
}
