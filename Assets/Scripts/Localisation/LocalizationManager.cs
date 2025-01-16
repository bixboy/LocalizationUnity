using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    public static event Action OnLanguageChanged;
    
    private Dictionary<string, Dictionary<string, string>> _translations = new Dictionary<string, Dictionary<string, string>>();
    private string _currentLanguage = "en";
    
    private void Awake()
    {
        Initialization();
        LoadTranslations();
    }

    private void Start()
    {
        LoadTranslations();
    }

    public void Initialization()
    {
        if (Instance == null)
        {
            Instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }
    
    public void SetLanguage(string language)
    {
        _currentLanguage = language;
        Debug.Log("Language set to: " + language);
        OnLanguageChanged?.Invoke();
    }
    
    public List<string> GetAllTranslationKeys()
    {
        List<string> keys = new List<string>(_translations.Keys);
        return keys;
    }

    public string GetTranslation(string key)
    {
        if (_translations.ContainsKey(key) && _translations[key].ContainsKey(_currentLanguage))
        {
            return _translations[key][_currentLanguage];
        }

        return $"Missing[{key}]";
    }

    public string GetCurrentLanguage()
    {
        return _currentLanguage;
    }
    
    public void LoadTranslations()
    {
        string filePath = "Assets/Resources/translations.json";

        if (System.IO.File.Exists(filePath))
        {
            string json = System.IO.File.ReadAllText(filePath);
            LocalizationData.TranslationList translationList = JsonUtility.FromJson<LocalizationData.TranslationList>(json);

            _translations.Clear();
            foreach (var translationData in translationList.translations)
            {
                Dictionary<string, string> translationDict = new Dictionary<string, string>();
                foreach (var langData in translationData.translations)
                {
                    translationDict[langData.language] = langData.translation;
                }
                _translations[translationData.key] = translationDict;
            }
        }
        else
        {
            Debug.LogWarning("No translation file found.");
        }
    }
}