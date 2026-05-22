using UnityEngine;

namespace TDF.Core.Data
{
    [CreateAssetMenu(fileName = "ScoreCalculationSettings", menuName = "TDF/Score Calculation Settings")]
    public class ScoreCalculationSettings : ScriptableObject
    {
        private static ScoreCalculationSettings instance;
        public static ScoreCalculationSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ScoreCalculationSettings>("Settings/ScoreCalculationSettings");
                    if (instance == null)
                    {
                        // 런타임에 에셋이 없을 경우 크래시를 방지하기 위해 임시 인스턴스 생성
                        instance = CreateInstance<ScoreCalculationSettings>();
                    }
                }
                return instance;
            }
        }

        [Header("Score Formula Weights")]
        [Tooltip("남은 골드에 곱할 가중치 (기본 10)")]
        public int goldWeight = 10;

        [Tooltip("남은 HP에 곱할 가중치 (기본 50)")]
        public int hpWeight = 50;

        [Tooltip("시간 보너스의 기본값 (기본 10000)")]
        public int timeBonusBase = 10000;

        [Tooltip("시간당 차감할 보너스 점수 (기본 20)")]
        public int timeBonusDecay = 20;

        [Header("Reward Settings")]
        [Tooltip("점수를 골드로 변환할 비율 (기본 0.1)")]
        public float scoreToGoldRatio = 0.1f;

        [Tooltip("클리어 시 지급할 기본 젬 개수 (기본 5)")]
        public int rewardGems = 5;
        
        #if UNITY_EDITOR
        // 에디터에서 에셋이 다시 로드될 때 싱글톤 참조를 강제로 null화하여 최신 상태를 로드할 수 있게 함
        public static void ResetInstance()
        {
            instance = null;
        }
        #endif
    }
}
