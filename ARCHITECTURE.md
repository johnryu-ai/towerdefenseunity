# Tower Defense Factory (TDF) - Architecture Documentation

## 1. 개요 (Overview)
본 프로젝트는 **데이터 주도(Data-Driven) 방식의 타워 디펜스 게임 및 에디터 툴셋**입니다.
하드코딩된 변수들을 최소화하고, 모든 기획 데이터(타워, 맵, 몬스터, 웨이브)를 `ScriptableObject`로 분리하여 에디터 및 기획자의 작업 효율을 극대화하는 것을 목표로 합니다.

## 2. 코어 시스템 아키텍처

### 2.1 데이터 관리 시스템
* **`ScriptableObject` 기반 메타 데이터**: `TowerData`, `MapData`, `WaveData`, `StageData`, `CampaignData` 등 게임의 모든 스펙은 ScriptableObject 에셋으로 존재합니다.
* **`UserDataManager` (Persistent Singleton)**: 
  * 플레이어의 로컬 진행도(골드, 젬, 해금된 타워, 클리어한 스테이지, 업적 등)를 `userdata.json` 파일로 디스크에 저장하고 관리합니다.
  * 게임을 시작할 때 최초 1회 로드되며 씬이 넘어가도(`DontDestroyOnLoad`) 유지됩니다.

### 2.2 씬(Scene) 매니지먼트
* **로비 씬 분리 구조**: 기존의 단일 로비 씬 구조에서 벗어나, 하위 메뉴들을 개별 씬(`Lobby_Main`, `Lobby_Stage`, `Lobby_Shop`, `Lobby_Achievement`, `Lobby_Leaderboard`, `Lobby_Event`)으로 분리하여 관리성을 높였습니다.
* **`LobbyUIManager`**: 로비의 전반적인 IMGUI 기반 UI를 담당하며, `Start()`에서 현재 씬의 이름(`SceneManager.GetActiveScene().name`)을 파악하여 알맞은 화면을 자동으로 렌더링합니다.

### 2.3 인게임 로직 및 부트스트래퍼
* **`GameManager`**: 스테이지 데이터(`StageData`)를 기반으로 초기 체력, 자금을 설정하고, 승리/패배 상태를 판별합니다. 승리 시 `UserDataManager`와 연동하여 메타 자원(PlayerGold, Gems)을 보상으로 지급합니다.
* **`AutoTestBootstrapper` (에디터 전용 편의 기능)**: 
  * `SampleScene`을 빈 상태로 켜놓고 바로 Play를 눌러도, 런타임에 자동으로 Camera, Managers, Canvas 등을 생성해 줍니다.
  * `CampaignData`나 `MapData`를 자동 색인하여 임시로 주입하므로, 씬 세팅 없이도 즉각적인 테스트가 가능합니다.

### 2.4 맵 및 렌더링 파이프라인
* **`MapController`**: `MapData`의 그리드 정보를 읽어 실제 타일 오브젝트를 동적으로 생성(`GenerateMap`)합니다.
* **해상도 및 카메라 자동 스케일링**:
  * 전체 화면을 FHD(1920x1080)로 강제 고정합니다.
  * 타일 개수(`gridHeight`)에 맞춰 카메라의 `orthographicSize`를 자동으로 계산하여, 맵이 화면의 정중앙에 위아래로 빈틈없이 꽉 차도록 수학적으로 렌더링합니다.

## 3. 향후 개발 확장 포인트
* **오브젝트 풀링(Object Pooling)**: 몬스터와 투사체는 생성/파괴가 빈번하므로 메모리 누수를 막기 위해 필수적으로 풀링 매니저를 통해 관리해야 합니다.
* **UGUI로의 전환 (선택 사항)**: 현재 로비 UI는 IMGUI로 작성되어 있습니다. 추후 모바일 대응이나 터치 최적화, 애니메이션 적용을 위해서는 Unity UGUI 체계로 점진적 마이그레이션을 고려할 수 있습니다.
