using UnityEngine;

namespace TDF.Core.Data
{
    [CreateAssetMenu(fileName = "FontSettings", menuName = "TDF/Font Settings")]
    public class FontSettings : ScriptableObject
    {
        private static FontSettings instance;
        public static FontSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<FontSettings>("Settings/FontSettings");
                    if (instance == null)
                    {
                        // 런타임에 에셋이 누락되었을 경우 크래시를 방지하기 위해 빈 인스턴스를 메모리에 생성
                        instance = CreateInstance<FontSettings>();
                    }
                }
                return instance;
            }
        }

        [Header("Global UI Font Settings")]
        [Tooltip("게임 내 기본 텍스트 및 레이블에 일괄 적용할 fallback 폰트입니다.")]
        public Font defaultFont;

        [Tooltip("대형 타이틀이나 헤더 텍스트에 적용할 폰트입니다. 미지정 시 defaultFont가 기본 적용됩니다.")]
        public Font titleFont;

        [Header("English Font Settings")]
        [Tooltip("영어 전용 UI를 위한 Inter 폰트입니다.")]
        public Font interFont;

        [Tooltip("영어 전용 UI를 위한 Outfit 폰트입니다.")]
        public Font outfitFont;

        [Tooltip("영어 전용 UI를 위한 Roboto 폰트입니다.")]
        public Font robotoFont;

        [Tooltip("영어 전용 UI를 위한 Cinzel 폰트입니다.")]
        public Font cinzelFont;

        [Tooltip("기타 추가로 등록하여 사용할 영어 폰트 목록입니다.")]
        public Font[] additionalEnglishFonts;

        #if UNITY_EDITOR
        // 에디터에서 에셋이 갱신된 후 강제로 null화하여 최신 상태를 로드할 수 있게 함
        public static void ResetInstance()
        {
            instance = null;
        }
        #endif
    }
}
