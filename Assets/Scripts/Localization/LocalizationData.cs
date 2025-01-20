using System.Collections.Generic;
using UnityEngine;

public class LocalizationData
{
    [System.Serializable]
    public class TranslationList
    {
        public List<TranslationData> translations = new List<TranslationData>();
    }

    [System.Serializable]
    public class TranslationData
    {
        public string key;
        public List<LanguageData> translations;

        public TranslationData(string key, Dictionary<string, string> translationsDict)
        {
            this.key = key;
            translations = new List<LanguageData>();
            foreach (var lang in translationsDict)
            {
                translations.Add(new LanguageData(lang.Key, lang.Value));
            }
        }
    }

    [System.Serializable]
    public class LanguageData
    {
        public string language;
        public string translation;

        public LanguageData(string language, string translation)
        {
            this.language = language;
            this.translation = translation;
        }
    }
}
