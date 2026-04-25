using System;
using System.Collections.Generic;
using UnityEngine;

namespace TDF.Core.Data
{
    // ══════════════════════════════════════════════════════════════════════
    // 1. 맵 클리어 기록
    // ══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class StageClearResult
    {
        public int livesRemaining;      // 클리어 시 남은 생명력
        public int goldRemaining;       // 클리어 시 남은 골드
        public int clearTimeSeconds;    // 클리어에 걸린 시간 (초)
        public int score;               // gold*10 + lives*50 + 시간 보너스
        public string clearedAt;        // ISO 날짜 문자열
    }

    [Serializable]
    public class MapClearRecord
    {
        public string stageId;          // StageData의 asset 이름 (고유 식별자)
        public string stageName;        // 표시용 이름
        public bool cleared;
        public bool hasBestResult;      // JsonUtility null 대체 플래그
        public StageClearResult bestResult = new StageClearResult();
        public int totalAttempts;       // 총 시도 횟수
    }

    // ══════════════════════════════════════════════════════════════════════
    // 2. 현재 플레이 세션 스냅샷
    // ══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class CurrentPlaySession
    {
        public bool hasSession;         // 세션 존재 여부 (null 대체)
        public string campaignName;     // 현재 진행 중인 캠페인 이름
        public int stageIndex;          // 현재 스테이지 인덱스
        public int currentGold;         // 현재 골드
        public int currentHP;           // 현재 생명력
        public int currentWaveIndex;    // 현재 웨이브 인덱스
        public string savedAt;          // 저장 시각 (ISO)
    }

    // ══════════════════════════════════════════════════════════════════════
    // 3. 구매 내역
    // ══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class PurchaseRecord
    {
        public string purchaseId;       // GUID (고유 구매 ID)
        public string itemId;           // ShopItemData.itemId
        public string itemName;         // 표시용 이름
        public int costGold;            // 지불한 골드
        public int costGems;            // 지불한 젬
        public string purchasedAt;      // ISO 날짜 문자열
    }

    // ══════════════════════════════════════════════════════════════════════
    // 4. 언락된 타워 정보
    // ══════════════════════════════════════════════════════════════════════

    // 언락 경로 구분자 상수 (string enum 대신 사용)
    public static class TowerUnlockSource
    {
        public const string Default       = "default";        // 스테이지 기본 제공
        public const string StageReward   = "stage_reward";   // 스테이지 클리어 보상
        public const string Purchase      = "purchase";       // 상점 구매
        public const string Achievement   = "achievement";    // 업적 달성 보상
        public const string Event         = "event";          // 이벤트 보상
    }

    [Serializable]
    public class UnlockedTowerRecord
    {
        public string towerId;           // TowerData.towerId
        public string unlockedAt;        // ISO 날짜 문자열
        public string unlockedBy;        // TowerUnlockSource 상수
        public string sourceId;          // 스테이지명 / 업적ID / 구매ID 등
    }

    // ══════════════════════════════════════════════════════════════════════
    // 5. 이벤트 진행 상태
    // ══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class EventProgressRecord
    {
        public string eventId;           // EventData.eventId
        public string eventName;         // 표시용 이름
        public bool completed;           // 이벤트 완료 여부
        public bool rewardClaimed;       // 보상 수령 여부
        public string joinedAt;          // 참여 시각 (ISO)
        public string completedAt;       // 완료 시각 (ISO, 미완료 시 빈 문자열)
    }

    // ══════════════════════════════════════════════════════════════════════
    // 6. 업적 진행 상태
    // ══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class AchievementProgressRecord
    {
        public string achievementId;     // AchievementData.achievementId
        public string achievementName;   // 표시용 이름
        public int currentProgress;      // 현재 진행 수치
        public bool completed;           // 달성 여부
        public bool rewardClaimed;       // 보상 수령 여부
        public string completedAt;       // 달성 시각 (ISO, 미달성 시 빈 문자열)
    }

    // ══════════════════════════════════════════════════════════════════════
    // 루트 저장 객체
    // ══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class UserSaveData
    {
        public List<MapClearRecord>             mapClearRecords       = new List<MapClearRecord>();
        public CurrentPlaySession               currentPlaySession    = new CurrentPlaySession();
        public List<PurchaseRecord>             purchases             = new List<PurchaseRecord>();
        public List<UnlockedTowerRecord>        unlockedTowers        = new List<UnlockedTowerRecord>();
        public List<EventProgressRecord>        eventProgresses       = new List<EventProgressRecord>();
        public List<AchievementProgressRecord>  achievementProgresses = new List<AchievementProgressRecord>();

        [Header("Currency")]
        public int  playerGold = 0;   // 골드 (인게임 재화)
        public int  playerGems = 0;   // 크리스탈 (프리미엄 재화 / 업적 보상)

        public string lastSavedAt;
    }
}
