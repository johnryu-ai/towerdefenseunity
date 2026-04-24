using UnityEngine;
using System.Collections.Generic;

namespace TDF.Core.Data
{
    [CreateAssetMenu(fileName = "NewCampaignData", menuName = "TDF/Data/CampaignData")]
    public class CampaignData : ScriptableObject
    {
        [Header("Campaign Info")]
        public string campaignName = "New Campaign";
        
        [Header("Stage Sequence")]
        public List<StageData> stages = new List<StageData>();
    }
}
