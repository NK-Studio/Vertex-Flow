using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VertexFlow.Core;

namespace VertexFlow.Localization
{
    public static class VertexPainterLocalization
    {
        private static readonly Dictionary<string, Dictionary<Language, string>> _localizedTexts = new();

        public static Language CurrentLanguage
        {
            get => (Language)EditorPrefs.GetInt("VertexPainter_Language", (int)Language.Korean);
            set => EditorPrefs.SetInt("VertexPainter_Language", (int)value);
        }

        public static void LoadCSV()
        {
            _localizedTexts.Clear();

            string path = AssetDatabase.GUIDToAssetPath("9e4fa6b08527744189bade4515b526cf");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[VertexPainter] Localization file not found at {path}");
                path = "Assets/Plugins/VertexFlow/Scripts/Editor/Asset/Localization/VertexPainterLocalization.csv";
            }

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[VertexPainter] Localization file not found at {path}");
                return;
            }

            string[] lines = File.ReadAllLines(path);

            if (lines.Length <= 1) return;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] columns = line.Split(',');
                if (columns.Length >= 4)
                {
                    string key = columns[0];
                    var langDict = new Dictionary<Language, string>();
                    langDict[Language.English] = columns[1].Replace("\\n", "\n");
                    langDict[Language.Korean] = columns[2].Replace("\\n", "\n");
                    langDict[Language.Japanese] = columns[3].Replace("\\n", "\n");
                    _localizedTexts[key] = langDict;
                }
            }
        }

        private static string GetText(string key, Language lang)
        {
            if (_localizedTexts.Count == 0) LoadCSV();
            if (_localizedTexts.ContainsKey(key) && _localizedTexts[key].ContainsKey(lang))
            {
                return _localizedTexts[key][lang];
            }

            return key;
        }

        public static string GetText(string key)
        {
            return GetText(key, CurrentLanguage);
        }
    }
}