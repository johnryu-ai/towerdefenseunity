using UnityEngine;

namespace TDF.Core.Data
{
    [CreateAssetMenu(fileName = "NewEventData", menuName = "TDF/Data/EventData")]
    public class EventData : ScriptableObject
    {
        public string eventId;
        public string eventName;
        
        [Header("Duration")]
        [Tooltip("YYYY-MM-DD HH:mm:ss format (Optional)")]
        public string startDate;
        public string endDate;

        [Header("Visuals")]
        public Sprite bannerImage;

        [Header("Rewards / Links")]
        [Tooltip("Clicking the banner will give these rewards")]
        public int rewardGold;
        public int rewardGems;
        
        [Tooltip("Or open this external link")]
        public string externalLink;
    }
}
