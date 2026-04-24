using UnityEngine;

namespace TDF.Core.Data
{
    [CreateAssetMenu(fileName = "NewAchievement", menuName = "TDF/Data/AchievementData")]
    public class AchievementData : ScriptableObject
    {
        public string achievementId;
        public string achievementName;
        [TextArea] public string description;

        [Header("Condition")]
        public int targetValue; // 예: 적 처치 수 100마리 -> 100

        [Header("Rewards")]
        public int rewardGold;
        public int rewardGems;

        [Header("Visuals")]
        public Sprite achievementIcon;
    }
}
