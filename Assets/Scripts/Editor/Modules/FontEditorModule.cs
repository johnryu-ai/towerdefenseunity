using UnityEditor;
using UnityEngine;
using TDF.Core.Data;
using System.IO;

namespace TDF.Editor.Modules
{
    public class FontEditorModule
    {
        private FontSettings settings;
        private const string SETTINGS_PATH = "Assets/Resources/Settings/FontSettings.asset";
        
        // Preview state
        private string previewText = "타워 디펜스 에디터 폰트 테스트! Hello World! 1234567890";
        private int previewSize = 32;

        public void Draw()
        {
            if (settings == null)
            {
                LoadSettings();
            }

            if (settings == null)
            {
                EditorGUILayout.HelpBox("Font Settings asset not found. Click the button below to create it.", MessageType.Warning);
                if (GUILayout.Button("Create Font Settings Asset", GUILayout.Height(30)))
                {
                    CreateSettings();
                }
                return;
            }

            SerializedObject serializedSettings = new SerializedObject(settings);
            serializedSettings.Update();

            EditorGUILayout.LabelField("Global Font Settings Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Font Assignment
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Global UI Font Mapping", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            SerializedProperty defaultFontProp = serializedSettings.FindProperty("defaultFont");
            SerializedProperty titleFontProp = serializedSettings.FindProperty("titleFont");

            EditorGUILayout.PropertyField(defaultFontProp, new GUIContent("Default Body Font"));
            EditorGUILayout.HelpBox("Used as the fallback font for buttons, labels, and standard text in both lobby and in-game UI.", MessageType.None);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(titleFontProp, new GUIContent("Title Header Font"));
            EditorGUILayout.HelpBox("Used for large headers or titles (optional). If unassigned, Default Body Font is used.", MessageType.None);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // English Specific Font Assignment
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("English Specific Fonts", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            SerializedProperty interFontProp = serializedSettings.FindProperty("interFont");
            SerializedProperty outfitFontProp = serializedSettings.FindProperty("outfitFont");
            SerializedProperty robotoFontProp = serializedSettings.FindProperty("robotoFont");
            SerializedProperty cinzelFontProp = serializedSettings.FindProperty("cinzelFont");
            SerializedProperty additionalEnglishFontsProp = serializedSettings.FindProperty("additionalEnglishFonts");

            EditorGUILayout.PropertyField(interFontProp, new GUIContent("Inter Font (Sleek UI)"));
            EditorGUILayout.PropertyField(outfitFontProp, new GUIContent("Outfit Font (Modern UI)"));
            EditorGUILayout.PropertyField(robotoFontProp, new GUIContent("Roboto Font (Clean UI)"));
            EditorGUILayout.PropertyField(cinzelFontProp, new GUIContent("Cinzel Font (Fantasy/Classic Title)"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(additionalEnglishFontsProp, new GUIContent("Additional English Fonts"), true);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Preview Panel
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Dynamic Font Style Preview", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            previewText = EditorGUILayout.TextField("Test Sentence", previewText);
            previewSize = EditorGUILayout.IntSlider("Preview Size", previewSize, 10, 80);
            EditorGUILayout.Space(10);

            // Draw text samples
            DrawFontPreview("Default Font", settings.defaultFont);
            DrawFontPreview("Title Font", settings.titleFont != null ? settings.titleFont : settings.defaultFont);
            DrawFontPreview("Inter Font", settings.interFont);
            DrawFontPreview("Outfit Font", settings.outfitFont);
            DrawFontPreview("Roboto Font", settings.robotoFont);
            DrawFontPreview("Cinzel Font", settings.cinzelFont);

            if (settings.additionalEnglishFonts != null)
            {
                for (int i = 0; i < settings.additionalEnglishFonts.Length; i++)
                {
                    if (settings.additionalEnglishFonts[i] != null)
                    {
                        DrawFontPreview($"Additional Font [{i}]: {settings.additionalEnglishFonts[i].name}", settings.additionalEnglishFonts[i]);
                    }
                }
            }

            EditorGUILayout.EndVertical();

            if (serializedSettings.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Font Settings", GUILayout.Height(25)))
            {
                LoadSettings();
            }
            if (GUILayout.Button("Auto-Register English Fonts", GUILayout.Height(25)))
            {
                AutoRegisterEnglishFonts();
            }
            if (GUILayout.Button("Fix Fonts (Force Dynamic)", GUILayout.Height(25)))
            {
                ForceAllFontsDynamic(true);
            }
            if (GUILayout.Button("Find Fonts Folder", GUILayout.Height(25)))
            {
                Object fontsFolder = AssetDatabase.LoadAssetAtPath<Object>("Assets/Fonts");
                if (fontsFolder != null)
                {
                    Selection.activeObject = fontsFolder;
                    EditorGUIUtility.PingObject(fontsFolder);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Assets/Fonts folder not found. Make sure fonts are downloaded.", "OK");
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawFontPreview(string label, Font font)
        {
            if (font != null)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.font = font;
                if (font.dynamic)
                {
                    style.fontSize = previewSize;
                }
                style.wordWrap = true;
                
                EditorGUILayout.LabelField($"{label} Preview:", EditorStyles.miniBoldLabel);
                try
                {
                    EditorGUILayout.LabelField(previewText, style);
                }
                catch (System.Exception ex)
                {
                    EditorGUILayout.HelpBox($"Failed to render preview for {label}. The font face may not be loaded properly yet. Try clicking 'Fix Fonts (Force Dynamic)' to reimport. (Error: {ex.Message})", MessageType.Warning);
                }
                EditorGUILayout.Space(5);
            }
            else
            {
                EditorGUILayout.HelpBox($"{label} is not assigned.", MessageType.Info);
            }
        }

        private void AutoRegisterEnglishFonts()
        {
            if (settings == null) return;

            ForceAllFontsDynamic(false);

            Undo.RecordObject(settings, "Auto-Register English Fonts");

            settings.interFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Inter-Regular.ttf");
            settings.outfitFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Outfit-Regular.ttf");
            settings.robotoFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Roboto-Regular.ttf");
            settings.cinzelFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Cinzel-Regular.ttf");

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", "English fonts automatically mapped from Assets/Fonts!", "OK");
        }

        private void ForceAllFontsDynamic(bool showDialog = true)
        {
            string fontsFolderPath = "Assets/Fonts";
            if (!Directory.Exists(fontsFolderPath))
            {
                if (showDialog) EditorUtility.DisplayDialog("Error", "Assets/Fonts folder not found.", "OK");
                return;
            }

            string[] files = Directory.GetFiles(fontsFolderPath, "*.*", SearchOption.AllDirectories);
            int count = 0;
            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".ttf" || ext == ".otf")
                {
                    TrueTypeFontImporter importer = AssetImporter.GetAtPath(file) as TrueTypeFontImporter;
                    if (importer != null && (importer.fontTextureCase != FontTextureCase.Dynamic || !importer.includeFontData))
                    {
                        importer.fontTextureCase = FontTextureCase.Dynamic;
                        importer.includeFontData = true;
                        importer.SaveAndReimport();
                        AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceUpdate); // 강제 임포트/캐시파괴 유도
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                AssetDatabase.Refresh();
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Success", $"Successfully verified and updated {count} fonts to Dynamic mode!", "OK");
            }
        }

        private void LoadSettings()
        {
            settings = AssetDatabase.LoadAssetAtPath<FontSettings>(SETTINGS_PATH);
            FontSettings.ResetInstance();
        }

        private void CreateSettings()
        {
            string dir = Path.GetDirectoryName(SETTINGS_PATH);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            ForceAllFontsDynamic(false);

            settings = ScriptableObject.CreateInstance<FontSettings>();
            
            // Auto-detect newly downloaded fonts as defaults if possible
            Font defaultDungGeunMo = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/neodgm.ttf");
            if (defaultDungGeunMo != null)
            {
                settings.defaultFont = defaultDungGeunMo;
            }

            // Auto-detect English fonts
            settings.interFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Inter-Regular.ttf");
            settings.outfitFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Outfit-Regular.ttf");
            settings.robotoFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Roboto-Regular.ttf");
            settings.cinzelFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Cinzel-Regular.ttf");

            AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            FontSettings.ResetInstance();
        }
    }
}
