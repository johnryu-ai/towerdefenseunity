using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

namespace TDF.Core.Data
{
    [System.Serializable]
    public class StoryMoment
    {
        [TextArea(3, 5)]
        public string storyText;
        public Sprite backgroundImage;
        public VideoClip videoClip;
    }

    [CreateAssetMenu(fileName = "NewStageData", menuName = "TDF/Data/StageData")]
    public class StageData : ScriptableObject
    {
        [Header("Sequence Info")]
        public int stageIndex;
        public MapData linkedMapData;

        [Header("Narrative")]
        public List<StoryMoment> entryStory = new List<StoryMoment>();
        public List<StoryMoment> clearStory = new List<StoryMoment>();

        [Header("Waves")]
        public WaveData waveData;
    }
}
