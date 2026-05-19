using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TDF.Core.Data;

namespace TDF.Runtime.Managers
{
    /// <summary>
    /// 유저 로컬 데이터를 관리하는 싱글톤 매니저.
    /// Application.persistentDataPath/userdata.json 에 JSON으로 저장한다.
    /// DontDestroyOnLoad 적용 → 씬 전환 후에도 유지된다.
    /// </summary>
    public class UserDataManager : MonoBehaviour
    {
        public static UserDataManager Instance { get; private set; }

        private const string SAVE_FILE_NAME = "userdata.json";
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        private UserSaveData saveData = new UserSaveData();

        /// <summary>현재 저장된 전체 유저 데이터에 대한 읽기 전용 참조.</summary>
        public UserSaveData Data => saveData;

        // ── 생명주기 ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                
                // BackendManager의 로그인 성공 이벤트를 기다렸다가 클라우드에서 데이터를 불러옵니다.
                // (로컬 파일 로딩은 이전 유저 데이터 충돌을 막기 위해 제외합니다.)
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (BackendManager.Instance != null)
            {
                BackendManager.Instance.OnSignInSuccess += LoadFromCloudEventWrapper;
                BackendManager.Instance.OnSignOut += ResetAllData;
                
                // 만약 이미 로그인되어 있다면 즉시 로드
                if (BackendManager.Instance.IsSignedIn)
                {
                    LoadFromCloudEventWrapper();
                }
            }
        }

        private void OnDestroy()
        {
            if (BackendManager.Instance != null)
            {
                BackendManager.Instance.OnSignInSuccess -= LoadFromCloudEventWrapper;
                BackendManager.Instance.OnSignOut -= ResetAllData;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // 파일 IO
        // ══════════════════════════════════════════════════════════════════

        /// <summary>현재 saveData를 클라우드에 저장한다 (로컬 백업 병행).</summary>
        public async void Save()
        {
            try
            {
                saveData.lastSavedAt = DateTime.Now.ToString("o");
                string json = JsonUtility.ToJson(saveData, prettyPrint: true);
                File.WriteAllText(SaveFilePath, json);

                // 클라우드 세이브 연동
                if (BackendManager.Instance != null && BackendManager.Instance.IsSignedIn)
                {
                    var data = new Dictionary<string, object> { { "UserSaveData", json } };
                    await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.SaveAsync(data);
                    Debug.Log("[UserDataManager] 클라우드 저장 완료");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserDataManager] 저장 실패: {e.Message}");
            }
        }

        private bool isLoadingFromCloud = false;

        /// <summary>서버에서 데이터를 불러온다. 신규 유저의 경우 초기값으로 세팅된다.</summary>
        public async Awaitable LoadFromCloudAsync()
        {
            if (isLoadingFromCloud)
            {
                // Awaitable은 여러 곳에서 동시에 await 할 수 없는 구조적 한계가 있습니다.
                // 따라서 이미 로딩 중이라면 끝날 때까지 프레임 단위로 대기합니다.
                while (isLoadingFromCloud)
                {
                    await Awaitable.NextFrameAsync();
                }
                return;
            }

            isLoadingFromCloud = true;
            try
            {
                await InternalLoadFromCloudAsync();
            }
            finally
            {
                isLoadingFromCloud = false;
            }
        }

        private async Awaitable InternalLoadFromCloudAsync()
        {
            if (BackendManager.Instance == null || !BackendManager.Instance.IsSignedIn)
            {
                Debug.LogWarning("[UserDataManager] 로그인되어 있지 않아 데이터를 불러올 수 없습니다.");
                if (saveData == null) saveData = new UserSaveData();
                return;
            }

            // 마이그레이션 중이라면 데이터베이스 덮어쓰기/읽기 충돌 방지를 위해 끝날 때까지 대기합니다.
            while (BackendManager.Instance.IsMigrating)
            {
                await Awaitable.WaitForSecondsAsync(0.1f);
            }

            // [중요] 유니티 클라우드(UGS) 권한 전파 지연 이슈 해결
            // 데이터 로드 전 반드시 1.5초 정도를 대기하여 서버 동기화를 보장합니다.
            await Awaitable.WaitForSecondsAsync(1.5f);

            Debug.Log("[UserDataManager] 서버에서 진행 데이터를 불러옵니다...");
            var keys = new HashSet<string> { "UserSaveData" };
            
            int maxRetries = 5; // 재시도 횟수 증가
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var loadedData = await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                    if (loadedData.TryGetValue("UserSaveData", out var cloudJson))
                    {
                        saveData = JsonUtility.FromJson<UserSaveData>(cloudJson.Value.GetAsString());
                        Debug.Log("[UserDataManager] 기존 유저 데이터 로드 완료.");
                    }
                    else
                    {
                        saveData = new UserSaveData();
                        Debug.Log("[UserDataManager] 신규 유저입니다. 데이터를 초기화합니다.");
                        Save(); 
                    }
                    return; // 성공 시 함수 종료
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[UserDataManager] 클라우드 로드 재시도 중 ({i+1}/{maxRetries}). 오류: {e.Message}");
                    await Awaitable.WaitForSecondsAsync(1.5f); // 대기 시간 증가
                }
            }

            // 모든 재시도 실패 시 빈 데이터로 초기화
            Debug.LogError("[UserDataManager] 클라우드 데이터를 불러오는데 최종 실패했습니다.");
            if (saveData == null) saveData = new UserSaveData();
        }
        
        private async void LoadFromCloudEventWrapper()
        {
            await LoadFromCloudAsync();
        }

        // ══════════════════════════════════════════════════════════════════
        // 1. 맵 클리어 기록
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 스테이지 클리어 결과를 기록한다.
        /// 이전 기록보다 점수가 높을 때만 bestResult가 갱신된다.
        /// </summary>
        public void RecordStageClear(string stageId, string stageName,
            int livesRemaining, int goldRemaining, int clearTimeSeconds)
        {
            int score = (goldRemaining * 10)
                      + (livesRemaining * 50)
                      + Mathf.Max(0, 10000 - clearTimeSeconds * 20);

            MapClearRecord record = saveData.mapClearRecords.Find(r => r.stageId == stageId);
            if (record == null)
            {
                record = new MapClearRecord { stageId = stageId, stageName = stageName };
                saveData.mapClearRecords.Add(record);
            }

            record.cleared = true;
            record.totalAttempts++;

            // 더 높은 점수일 때만 갱신 (리더보드 최고 결과)
            if (!record.hasBestResult || score > record.bestResult.score)
            {
                record.hasBestResult = true;
                record.bestResult.livesRemaining  = livesRemaining;
                record.bestResult.goldRemaining   = goldRemaining;
                record.bestResult.clearTimeSeconds = clearTimeSeconds;
                record.bestResult.score           = score;
                record.bestResult.clearedAt       = DateTime.Now.ToString("o");
            }

            Save();
        }

        /// <param name="stageId">StageData의 asset 이름</param>
        /// <returns>기록이 없으면 null 반환</returns>
        public MapClearRecord GetMapRecord(string stageId)
            => saveData.mapClearRecords.Find(r => r.stageId == stageId);

        // ══════════════════════════════════════════════════════════════════
        // 2. 현재 플레이 세션
        // ══════════════════════════════════════════════════════════════════

        /// <summary>현재 인게임 상태를 스냅샷으로 저장한다 (중간 저장 용도).</summary>
        public void SavePlaySession(string campaignName, int stageIndex,
            int gold, int hp, int waveIndex)
        {
            var s = saveData.currentPlaySession;
            s.hasSession      = true;
            s.campaignName    = campaignName;
            s.stageIndex      = stageIndex;
            s.currentGold     = gold;
            s.currentHP       = hp;
            s.currentWaveIndex = waveIndex;
            s.savedAt         = DateTime.Now.ToString("o");
            Save();
        }

        /// <summary>클리어 또는 게임오버 후 세션 정보를 삭제한다.</summary>
        public void ClearPlaySession()
        {
            saveData.currentPlaySession.hasSession = false;
            Save();
        }

        /// <returns>저장된 세션이 없으면 null 반환</returns>
        public CurrentPlaySession GetPlaySession()
            => saveData.currentPlaySession.hasSession ? saveData.currentPlaySession : null;

        // ══════════════════════════════════════════════════════════════════
        // 3. 구매 내역
        // ══════════════════════════════════════════════════════════════════

        /// <summary>새 구매 내역을 추가한다.</summary>
        public void AddPurchase(string itemId, string itemName, int costGold = 0, int costGems = 0)
        {
            saveData.purchases.Add(new PurchaseRecord
            {
                purchaseId  = Guid.NewGuid().ToString(),
                itemId      = itemId,
                itemName    = itemName,
                costGold    = costGold,
                costGems    = costGems,
                purchasedAt = DateTime.Now.ToString("o")
            });
            Save();
        }

        /// <returns>해당 itemId를 구매한 적이 있으면 true</returns>
        public bool HasPurchased(string itemId)
            => saveData.purchases.Exists(p => p.itemId == itemId);

        // ══════════════════════════════════════════════════════════════════
        // 4. 언락된 타워 정보
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 타워를 언락 목록에 추가한다. 이미 존재하면 무시한다.
        /// unlockedBy 에는 TowerUnlockSource 상수를 사용할 것.
        /// </summary>
        public void UnlockTower(string towerId,
            string unlockedBy = TowerUnlockSource.Default,
            string sourceId   = "")
        {
            if (saveData.unlockedTowers.Exists(t => t.towerId == towerId)) return;

            saveData.unlockedTowers.Add(new UnlockedTowerRecord
            {
                towerId    = towerId,
                unlockedAt = DateTime.Now.ToString("o"),
                unlockedBy = unlockedBy,
                sourceId   = sourceId
            });
            Save();
        }

        /// <returns>해당 타워가 언락되어 있으면 true</returns>
        public bool IsTowerUnlocked(string towerId)
            => saveData.unlockedTowers.Exists(t => t.towerId == towerId);

        /// <returns>언락된 모든 타워 ID 목록</returns>
        public List<string> GetUnlockedTowerIds()
        {
            var ids = new List<string>();
            foreach (var t in saveData.unlockedTowers) ids.Add(t.towerId);
            return ids;
        }

        // ══════════════════════════════════════════════════════════════════
        // 5. 이벤트 진행 상태
        // ══════════════════════════════════════════════════════════════════

        /// <summary>이벤트에 참여(등록)한다. 이미 참여했으면 무시한다.</summary>
        public void JoinEvent(string eventId, string eventName)
        {
            if (saveData.eventProgresses.Exists(e => e.eventId == eventId)) return;

            saveData.eventProgresses.Add(new EventProgressRecord
            {
                eventId       = eventId,
                eventName     = eventName,
                completed     = false,
                rewardClaimed = false,
                joinedAt      = DateTime.Now.ToString("o"),
                completedAt   = string.Empty
            });
            Save();
        }

        /// <summary>이벤트를 완료 처리한다.</summary>
        public void CompleteEvent(string eventId)
        {
            var ev = saveData.eventProgresses.Find(e => e.eventId == eventId);
            if (ev == null || ev.completed) return;
            ev.completed    = true;
            ev.completedAt  = DateTime.Now.ToString("o");
            Save();
        }

        /// <summary>이벤트 보상을 수령 처리한다.</summary>
        public void ClaimEventReward(string eventId)
        {
            var ev = saveData.eventProgresses.Find(e => e.eventId == eventId);
            if (ev == null || !ev.completed || ev.rewardClaimed) return;
            ev.rewardClaimed = true;
            Save();
        }

        /// <returns>기록이 없으면 null 반환</returns>
        public EventProgressRecord GetEventProgress(string eventId)
            => saveData.eventProgresses.Find(e => e.eventId == eventId);

        // ══════════════════════════════════════════════════════════════════
        // 6. 업적 진행 상태
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 업적을 등록한다. 이미 존재하면 무시한다.
        /// 보통 게임 시작 시 AchievementData 에셋을 순회하며 일괄 호출한다.
        /// </summary>
        public void RegisterAchievement(string achievementId, string achievementName)
        {
            if (saveData.achievementProgresses.Exists(a => a.achievementId == achievementId)) return;

            saveData.achievementProgresses.Add(new AchievementProgressRecord
            {
                achievementId   = achievementId,
                achievementName = achievementName,
                currentProgress = 0,
                completed       = false,
                rewardClaimed   = false,
                completedAt     = string.Empty
            });
            // 일괄 등록 후 Save()는 호출부에서 직접 호출할 것
        }

        /// <summary>
        /// 업적의 진행도를 절대값으로 업데이트한다.
        /// targetValue 이상이면 자동으로 completed 처리한다.
        /// </summary>
        public void UpdateAchievementProgress(string achievementId, int progress, int targetValue)
        {
            var ach = saveData.achievementProgresses.Find(a => a.achievementId == achievementId);
            if (ach == null || ach.completed) return;

            ach.currentProgress = Mathf.Min(progress, targetValue);
            if (ach.currentProgress >= targetValue)
            {
                ach.completed   = true;
                ach.completedAt = DateTime.Now.ToString("o");
            }
            Save();
        }

        /// <summary>업적 진행도를 delta만큼 증가시킨다 (증분 업데이트).</summary>
        public void IncrementAchievement(string achievementId, int targetValue, int delta = 1)
        {
            var ach = saveData.achievementProgresses.Find(a => a.achievementId == achievementId);
            if (ach == null || ach.completed) return;
            UpdateAchievementProgress(achievementId, ach.currentProgress + delta, targetValue);
        }

        /// <summary>업적 보상을 수령 처리한다.</summary>
        public void ClaimAchievementReward(string achievementId)
        {
            var ach = saveData.achievementProgresses.Find(a => a.achievementId == achievementId);
            if (ach == null || !ach.completed || ach.rewardClaimed) return;
            ach.rewardClaimed = true;
            Save();
        }

        /// <returns>기록이 없으면 null 반환</returns>
        public AchievementProgressRecord GetAchievementProgress(string achievementId)
            => saveData.achievementProgresses.Find(a => a.achievementId == achievementId);

        /// <returns>등록된 전체 업적 목록</returns>
        public List<AchievementProgressRecord> GetAllAchievements()
            => saveData.achievementProgresses;

        /// <returns>완료됐지만 아직 보상을 받지 않은 업적 목록</returns>
        public List<AchievementProgressRecord> GetClaimableRewards()
            => saveData.achievementProgresses.FindAll(a => a.completed && !a.rewardClaimed);

        // ══════════════════════════════════════════════════════════════════
        // 재화 (크리스탈) - EconomyManager 연동 (골드는 인게임 전용으로 제외됨)
        // ══════════════════════════════════════════════════════════════════

        public int PlayerGems => EconomyManager.Instance != null ? (int)EconomyManager.Instance.CurrentGems : saveData.playerGems;

        /// <summary>크리스탈을 지급한다 (서버 연동). 골드 파라미터는 하위 호환성을 위해 유지되나 무시됨.</summary>
        public async void AddCurrency(int gold = 0, int gems = 0)
        {
            if (EconomyManager.Instance != null)
            {
                if (gems > 0) await EconomyManager.Instance.AddCurrencyAsync(EconomyManager.CURRENCY_GEMS_ID, gems);
            }
            else
            {
                saveData.playerGems += gems;
                Save();
            }
        }

        /// <returns>이제 로비에서 골드를 소모하지 않으므로 무조건 false를 반환합니다.</returns>
        public bool SpendGold(int amount)
        {
            return false;
        }

        /// <returns>크리스탈이 충분하면 차감 후 true, 부족하면 false.</returns>
        public bool SpendGems(int amount)
        {
            if (PlayerGems < amount) return false;

            if (EconomyManager.Instance != null)
            {
                _ = EconomyManager.Instance.SpendCurrencyAsync(EconomyManager.CURRENCY_GEMS_ID, amount);
            }
            else
            {
                saveData.playerGems -= amount;
                Save();
            }
            return true;
        }

        // ══════════════════════════════════════════════════════════════════
        // 유틸리티
        // ══════════════════════════════════════════════════════════════════


        /// <summary>모든 유저 데이터를 초기화하고 저장 파일을 삭제한다. (복구 불가)</summary>
        public void ResetAllData()
        {
            saveData = new UserSaveData();
            if (File.Exists(SaveFilePath)) File.Delete(SaveFilePath);
            Debug.LogWarning("[UserDataManager] ⚠️ 모든 유저 데이터가 초기화되었습니다.");
        }

        /// <summary>저장 파일의 절대 경로를 반환한다 (디버깅용).</summary>
        public string GetSaveFilePath() => SaveFilePath;
    }
}
