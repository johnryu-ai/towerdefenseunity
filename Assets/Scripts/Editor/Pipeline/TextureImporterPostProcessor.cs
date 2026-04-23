using UnityEditor;
using UnityEngine;

namespace TDF.Editor.Pipeline
{
    /// <summary>
    /// 외부 이미지를 유니티로 임포트할 때 타워 디펜스(2D) 환경에 맞게 자동으로 설정을 변경해주는 스크립트입니다.
    /// </summary>
    public class TextureImporterPostProcessor : AssetPostprocessor
    {
        // 텍스처 임포트 직전에 자동으로 호출되는 콜백 함수
        void OnPreprocessTexture()
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;

            // 프로젝트 내 특정 폴더(예: Assets/Art 또는 Assets/Sprites)에 들어오는 이미지만 자동 처리하고 싶다면 아래 주석을 해제하고 경로를 지정하세요.
            // if (!assetPath.Contains("Assets/Art/")) return;

            // 이미 임포트된 텍스처를 수정할 때는 무시 (최초 임포트 시에만 적용)
            // 이를 막기 위해 임포터가 기본값인지 확인하는 추가 조건문이 있을 수 있지만, 
            // 여기서는 단순성을 위해 모든 텍스처 임포트 시 2D 스프라이트 포맷으로 덮어씁니다.

            // 1. 텍스처 타입을 Sprite(2D and UI)로 변경
            if (textureImporter.textureType != TextureImporterType.Sprite)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
            }

            // 2. 도트/픽셀 아트를 선명하게 보여주기 위해 필터 모드를 Point (no filter)로 설정
            textureImporter.filterMode = FilterMode.Point;

            // 3. 스프라이트 압축으로 인한 화질 저하 및 픽셀 깨짐 방지를 위해 압축 안 함으로 설정
            TextureImporterPlatformSettings defaultSettings = textureImporter.GetDefaultPlatformTextureSettings();
            defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.SetPlatformTextureSettings(defaultSettings);

            // 4. 알파(투명도) 채널 처리 설정
            textureImporter.alphaIsTransparency = true;
            
            // 5. 밉맵(Mipmap) 생성 비활성화 (2D 픽셀아트 게임에서는 보통 불필요)
            textureImporter.mipmapEnabled = false;
        }
    }
}
