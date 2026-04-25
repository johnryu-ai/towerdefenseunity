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
                DontDestroyOnLoad(gameObject);
                Load();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // 파일 IO
        // ══════════════════════════════════════════════════════════════════

        /// <summary>현재 saveData를 JSON 파일로 즉시 저장한다.</summary>
        public void Save()
        {
            try
            {
                saveData.lastSavedAt = DateTime.Now.ToString("o");
                string json = JsonUtility.ToJson(saveData, prettyPrint: true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[UserDataManager] 저장 완료 → {SaveFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserDataManager] 저장 실패: {e.Message}");
            }
        }

        /// <summary>JSON 파일에서 saveData를 불러온다. 파일이 없으면 새 데이터를 생성한다.</summary>
        public void Load()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    saveData = JsonUtility.FromJson<UserSaveData>(json);
                    if (saveData == null) saveData = new UserSaveData();
                    Debug.Log($"[UserDataManager] 로드 완료 → {SaveFilePath}");
                }
                else
                {
                    saveData = new UserSaveData();
                    Debug.Log("[UserDataManager] 저장 파일 없음. 새 데이터로 시작합니다.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserDataManager] 로드 실패: {e.Message}");
                saveData = new UserSaveData();
            }
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
        // 재화 (골드 / 크리스탈)
        // ══════════════════════════════════════════════════════════════════

        public int PlayerGold => saveData.playerGold;
        public int PlayerGems => saveData.playerGems;

        /// <summary>골드·크리스탈을 지급한다 (음수 전달 금지).</summary>
        public void AddCurrency(int gold = 0, int gems = 0)
        {
            saveData.playerGold += gold;
            saveData.playerGems += gems;
            Save();
        }

        /// <returns>골드가 충분하면 차감 후 true, 부족하면 false</returns>
        public bool SpendGold(int amount)
        {
            if (saveData.playerGold < amount) return false;
            saveData.playerGold -= amount;
            Save();
            return true;
        }

        /// <returns>크리스탈이 충분하면 차감 후 true, 부족하면 false</returns>
        public bool SpendGems(int amount)
        {
            if (saveData.playerGems < amount) return false;
            saveData.playerGems -= amount;
            Save();
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
