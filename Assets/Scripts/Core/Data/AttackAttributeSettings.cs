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
        public AnimationClip fireEffectAnimation;
        public Vector3 fireEffectOffset = Vector3.zero;
        public Vector3 fireEffectScale = Vector3.one;

        [Header("Ice (Cold)")]
        [Tooltip("Movement speed multiplier (e.g., 0.4 = 40% speed, which means 60% slow)")]
        public float iceSlowMultiplier = 0.4f;
        public float iceDuration = 2f;
        public AnimationClip iceEffectAnimation;
        public Vector3 iceEffectOffset = Vector3.zero;
        public Vector3 iceEffectScale = Vector3.one;

        [Header("Lightning (Electric)")]
        public float lightningStunDuration = 0.5f;

        [Header("Tower Multi Attack Settings")]
        public float multiAttackRange = 0.7f;

        [HideInInspector] public Sprite[] fireSprites;
        [HideInInspector] public float fireDurationCalculated = 0f;

        [HideInInspector] public Sprite[] iceSprites;
        [HideInInspector] public float iceDurationCalculated = 0f;

#if UNITY_EDITOR
        public void ExtractSprites()
        {
            fireSprites = ExtractSpritesFromClip(fireEffectAnimation);
            fireDurationCalculated = fireEffectAnimation != null ? fireEffectAnimation.length : 0f;

            iceSprites = ExtractSpritesFromClip(iceEffectAnimation);
            iceDurationCalculated = iceEffectAnimation != null ? iceEffectAnimation.length : 0f;
        }

        private Sprite[] ExtractSpritesFromClip(AnimationClip clip)
        {
            if (clip == null) return null;
            var bindings = UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var binding in bindings)
            {
                if (binding.propertyName == "m_Sprite")
                {
                    var keyframes = UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    if (keyframes != null && keyframes.Length > 0)
                    {
                        Sprite[] sprites = new Sprite[keyframes.Length];
                        for (int i = 0; i < keyframes.Length; i++)
                        {
                            sprites[i] = keyframes[i].value as Sprite;
                        }
                        return sprites;
                    }
                }
            }
            return null;
        }

        private void OnValidate()
        {
            ExtractSprites();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

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
