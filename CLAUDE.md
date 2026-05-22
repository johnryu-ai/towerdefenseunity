# Unity Tower Defense Editor - Development Guide

## Build & Test Commands
We run Unity in batchmode for automated builds and tests. Update the Unity editor path according to your machine.

### Run EditMode Tests
```bash
"C:\Program Files\Unity\Hub\Editor\6000.4.3f1\Editor\Unity.exe" -runTests -batchmode -projectPath . -testPlatform EditMode -testResults Logs/editmode-results.xml
```

### Run PlayMode Tests
```bash
"C:\Program Files\Unity\Hub\Editor\6000.4.3f1\Editor\Unity.exe" -runTests -batchmode -projectPath . -testPlatform PlayMode -testResults Logs/playmode-results.xml
```

### Generate C# Project Files (IntelliSense Sync)
```bash
"C:\Program Files\Unity\Hub\Editor\6000.4.3f1\Editor\Unity.exe" -batchmode -projectPath . -executeMethod UnityEditor.SyncVS.Sync -quit
```

---

## Coding Style & Architecture Guidelines

### Core Principles
1. **Unity 6 Async (`Awaitable`)**: Prefer using Unity's new `Awaitable` (e.g. `Awaitable.WaitForSecondsAsync`) instead of old `IEnumerator` or `.NET Task` structures for cleaner, non-allocating async control flow.
2. **Data-Driven Architecture**: Avoid hardcoding stats. Read values dynamically from `ScriptableObject` definitions (e.g. `TowerData`, `MapData`, `MonsterData`).
3. **No PlayerPrefs**: All persistent player states must be read/written via `UserDataManager.Instance` and saved locally inside `userdata.json`.
4. **Coordinate Math & Resolution**: Use `MapController.Instance.GetWorldPosition(x, y)` to obtain spatial vectors rather than hardcoding transform coordinates. The target screen resolution is forced at `2340x1080` (19.5:9 ratio).
5. **UI Structure**: The lobby relies on an IMGUI system (`LobbyUIManager`) segregated into independent scenes (`Lobby_Shop`, `Lobby_Stage`, etc.). Scene navigation is done via `SceneManager.LoadScene()`.

### Code Layout Requirements
* **Class Names**: PascalCase.
* **Fields & Variables**: Private/protected fields prefixed with `_` (e.g., `_playerGold`). Properties in PascalCase.
* **Namespaces**: Group system-wide behaviors within namespaces (e.g., `TDF.Core`, `TDF.Runtime`, `TDF.Editor`).
