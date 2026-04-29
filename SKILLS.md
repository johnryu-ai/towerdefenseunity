# TDF Project - AI Skills & Workflow Guide

이 파일은 안티그래비티(AI)가 이 타워 디펜스 프로젝트에서 코드를 작성하고 수정할 때 반드시 준수해야 하는 규칙(Rule)과 가이드라인을 정의한 스킬(Skills) 파일입니다.

## 1. 데이터 저장 및 관리 규칙 (UserDataManager)
* **금지 사항**: 플레이어의 진행 데이터나 재화 등을 저장할 때 `PlayerPrefs`를 절대 사용하지 마세요.
* **권장 방식**: 항상 싱글톤인 `UserDataManager.Instance`를 통해 데이터를 읽고 씁니다.
  * *예시 (골드 추가)*: `UserDataManager.Instance.AddCurrency(goldAmount, 0);`
  * *예시 (타워 해금 확인)*: `UserDataManager.Instance.IsTowerUnlocked(towerId);`
  * 상태 변경 후 즉시 디스크 저장이 필요할 때는 `UserDataManager.Instance.Save()`를 호출하세요.

## 2. 로비 및 UI 개발 규칙
* **씬 분리 원칙**: 로비의 각 탭(Stage, Shop, Event 등)은 현재 독립된 씬(`Lobby_Stage`, `Lobby_Shop` 등)으로 나뉘어 있습니다.
* **네비게이션**: 메뉴 간 이동 로직을 짤 때는 내부 상태 변경이 아니라 `SceneManager.LoadScene("씬이름")`을 사용해야 합니다.
* **UI 방식**: 로비는 기본적으로 `LobbyUIManager.cs` 내의 IMGUI(OnGUI) 방식으로 구현되어 있습니다. 기존 스타일을 유지하되, 불가피한 앵커링이나 팝업은 IMGUI의 `GUI.Window` 또는 `GUILayout` 구조를 준수하세요.

## 3. 인게임 맵 및 카메라 제어 규칙
* **절대 좌표 하드코딩 금지**: 타일이나 타워의 좌표를 픽셀이나 특정 수치로 하드코딩하지 마세요.
* **동적 맵 처리**: 맵 크기와 위치는 `MapController.cs`가 `MapData.gridWidth` 및 `gridHeight`에 따라 동적으로 오프셋(`offsetX`, `offsetY`)을 계산합니다. 월드 좌표가 필요하다면 반드시 `MapController.Instance.GetWorldPosition(x, y)`를 사용하세요.
* **해상도 강제**: 게임은 항상 `1920x1080` FHD 해상도를 기준으로 작동하며, 카메라는 `MapController.cs`에 의해 맵 세로 크기가 화면에 꽉 차도록 `orthographicSize`가 자동 계산됩니다. 카메라 조작 코드를 추가하지 마세요.

## 4. 인게임 테스트 환경 규칙
* 개발 중 테스트를 위해 `SampleScene`을 다룰 때, 별도의 매니저 프리팹을 씬에 배치하도록 지시하지 마세요.
* `AutoTestBootstrapper.cs`가 `[RuntimeInitializeOnLoadMethod]`를 통해 런타임에 게임 매니저, UI 캔버스, 이벤트 시스템, 맵 컨트롤러를 자동으로 생성합니다.
* 데이터가 없다면 에디터의 `AssetDatabase`를 이용해 Campaign 및 Map 데이터를 자동으로 주입하므로, 테스트 코드 작성 시 이 구조에 의존하세요.

## 5. 타워 건설 및 해금 연동
* 인게임에서 타워를 지을 때 사용하는 `BuildManager.cs`는 화면에 지을 수 있는 타워 목록을 뿌려줍니다.
* 이때 맵 데이터(`availableTowers`)에 있는 타워라 하더라도, **반드시 `UserDataManager.Instance.IsTowerUnlocked()`를 통과한 타워만 UI에 노출되도록** 필터링 로직을 유지해야 합니다.
