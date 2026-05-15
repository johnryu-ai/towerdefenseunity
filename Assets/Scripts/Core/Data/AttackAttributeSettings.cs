using UnityEngine;

namespace TDF.Core.Data
{
    [CreateAssetMenu(fileName = "AttackAttributeSettings", menuName = "TDF/Data/AttackAttributeSettings")]
    public class AttackAttributeSettings : ScriptableObject
    {
        [Header("Fire (Burn)")]
        [Tooltip("Percentage of base damage dealt as burn per second (e.g., 0.2 = 20%)")]
        public float fireBurnDamageRatio = 0.2f;
        public float fireDuration = 3f;

        [Header("Ice (Cold)")]
        [Tooltip("Movement speed multiplier (e.g., 0.4 = 40% speed, which means 60% slow)")]
        public float iceSlowMultiplier = 0.4f;
        public float iceDuration = 2f;

        [Header("Lightning (Electric)")]
        public float lightningStunDuration = 0.5f;

        private static AttackAttributeSettings _instance;
        public static AttackAttributeSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<AttackAttributeSettings>("Settings/AttackAttributeSettings");
                    if (_instance == null)
                    {
                        // Fallback: create a temporary instance with default values if not found
                        _instance = CreateInstance<AttackAttributeSettings>();
                        Debug.LogWarning("AttackAttributeSettings asset not found in Resources/Settings/. Using default values.");
                    }
                }
                return _instance;
            }
        }
    }
}
