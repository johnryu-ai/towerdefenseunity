using UnityEngine;

namespace TDF.Core.Data
{
    public enum AchievementConditionType 
    { 
        TotalStagesCleared, 
        TotalGoldEarned, 
        TotalMonstersKilled, 
        SpecificStageClear 
    }

    [CreateAssetMenu(fileName = "NewAchievement", menuName = "TDF/Data/AchievementData")]
    public class AchievementData : ScriptableObject
    {
        public string achievementId;
        public string achievementName;
        [TextArea] public string description;

        [Header("Condition")]
        public AchievementConditionType conditionType = AchievementConditionType.TotalStagesCleared;
        public int targetValue; // 예: 적 처치 수 100마리 -> 100
        [Tooltip("SpecificStageClear 조건일 때 확인할 스테이지 이름 (예: Stage1-1)")]
        public string targetStringData;

        [Header("Rewards")]
        public int rewardGold;
        public int rewardGems;

        [Header("Visuals")]
        public Sprite achievementIcon;
    }
}
