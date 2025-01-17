using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GUILayout;

public class Localization : EditorWindow
{
    private static Dictionary<string, Dictionary<string, string>> _translations = new Dictionary<string, Dictionary<string, string>>();
    private List<string> _languages = new List<string> { "en", "fr", "ja" };
    private string[] _languageLabels = new string[] { "English (en)", "French (fr)", "Japanese (ja)" };
    private Vector2 _scrollPosition;
    
    private int _selectedTab = 0;
    private Vector2 _textMeshProScrollPosition;
    private List<TMP_Text> _translateObjects;
    public List<TMP_Text> GetTexts() => _translateObjects;

    [MenuItem("Tools/Localization Editor")]
    public static void ShowWindow()
    {
        GetWindow<Localization>("Localization Editor");
    }
    
    private void OnEnable()
    {
        LoadTranslations();
        FindAllTranslateObjects();
    }
    
    private void OnGUI()
    {
        _selectedTab = GUILayout.Toolbar(_selectedTab, new string[] { "Localization Table", "Text Objects" });

        switch (_selectedTab)
        {
            case 0:
                DrawLocalizationTable();
                break;
            case 1:
                DrawTextMeshProObjects();
                break;
        }
        
    }
    
    private void DrawLocalizationTable()
    {
        Label("Localization Table", EditorStyles.boldLabel);
        _scrollPosition = BeginScrollView(_scrollPosition, false, true);
    
        BeginHorizontal();
        Label("Key", Width(200));
        for (int i = 0; i < _languages.Count; i++)
        {
            _languages[i] = EditorGUILayout.TextField(_languages[i], Width(76.8f));
    
            if (GUILayout.Button("X", Width(20)))
            {
                RemoveLanguage(_languages[i]);
                break;
            }
        }
        EndHorizontal();
    
        List<string> keysToRemove = new List<string>();
        List<string> keys = new List<string>(_translations.Keys);
        foreach (var t in keys)
        {
            string key = t;
            BeginHorizontal();
    
            // Update and Remove Keys
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                keysToRemove.Add(key); 
            }
    
            // Update and Create Keys
            string newKey = EditorGUILayout.TextField(key, Width(179));
            if (newKey != key)
            {
                if (!_translations.ContainsKey(newKey))
                {
                    _translations[newKey] = new Dictionary<string, string>(_translations[key]);
                    _translations.Remove(key);
                    key = newKey;
                }
            }
    
            // Update and Create Translations
            for (int j = 0; j < _languages.Count; j++)
            {
                string lang = _languages[j];
                if (_translations[key].ContainsKey(lang))
                {
                    _translations[key][lang] = EditorGUILayout.TextField(_translations[key][lang], Width(100));
                }
                else
                {
                    _translations[key][lang] = EditorGUILayout.TextField("", Width(100));
                }
            }
            EndHorizontal();
        }
        foreach (var keyToRemove in keysToRemove)
        {
            RemoveKey(keyToRemove);
        }
        
        EndScrollView();
        
        // NEW KEY
        if (Button("Add New Key"))
        {
            AddNewKey();
        }
    
        // NEW LANGUAGE
        if (Button("Add New Language"))
        {
            AddNewLanguage();
        }
    
        // IMPORT CSV
        Space(10);
        if (Button("Import CSV"))
        {
            ImportCsv();
        }
    
        // SAVE
        if (Button("Save All"))
        {
            SaveTranslations();
        }
    }

    // Text Component
    #region Text Component

        /*- Draw Window -*/
        private void DrawTextMeshProObjects()
        {
            Label("Text Objects with Translate Component", EditorStyles.boldLabel);
            _textMeshProScrollPosition = BeginScrollView(_textMeshProScrollPosition, false, true);
        
            if (_translateObjects != null)
            {
                foreach (var translateObject in _translateObjects)
                {
                    if (translateObject == null)
                    {
                        Debug.LogWarning("A TextMeshProUGUI object in the list is null or has been destroyed.");
                        continue; // Passe à l'objet suivant
                    }
        
                    BeginHorizontal();
        
                    var translateComponent = translateObject.GetComponent<TranslationComponent>();
                    var textMeshProComponent = translateObject.GetComponent<TextMeshProUGUI>();
        
                    if (translateComponent != null && textMeshProComponent != null)
                    {
                        List<string> keys = new List<string>(_translations.Keys);
        
                        int currentIndex = keys.IndexOf(translateComponent._localizationKey);
                        if (currentIndex < 0) currentIndex = 0;
        
                        int selectedIndex = EditorGUILayout.Popup(
                            textMeshProComponent.name,
                            currentIndex,
                            keys.ToArray(),
                            GUILayout.Width(400)
                        );
        
                        if (selectedIndex != currentIndex)
                        {
                            string selectedKey = keys[selectedIndex];
        
                            translateComponent._localizationKey = selectedKey;
                            textMeshProComponent.text = GetTranslation(translateComponent._localizationKey);
                            EditorUtility.SetDirty(translateComponent);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Missing TranslationComponent or TextMeshProUGUI on the object.");
                    }
        
                    EndHorizontal();
                }
            }
            else
            {
                Debug.LogWarning("_translateObjects list is null.");
            }
        
            EndScrollView();
        
            if (GUILayout.Button("Refresh List"))
            {
                FindAllTranslateObjects();
            }
        }
        
        /*- Find all text -*/
        private void FindAllTranslateObjects()
        {
            _translateObjects = new List<TMP_Text>();
            var allObjects = Resources.FindObjectsOfTypeAll<TMP_Text>();

            foreach (var obj in allObjects)
            {
                if (obj == null || obj.gameObject == null)
                {
                    Debug.LogWarning("Found a null or destroyed object while searching for TMP_Text.");
                    continue;
                }

                if (obj.GetComponent<TranslationComponent>() != null)
                {
                    _translateObjects.Add(obj);
                }
            }

            Debug.Log($"Found {_translateObjects.Count} TextMeshPro objects with TranslationComponent.");
        }

    #endregion

    // Languages
    #region Languages

        /*- New Language -*/
        private void AddNewLanguage()
        {
            string newLanguageKey = "New Language";
            if (!_languages.Contains(newLanguageKey))
            {
                _languages.Add(newLanguageKey);
                Debug.Log($"Added new language: {newLanguageKey}");
            }
            else
            {
                Debug.LogWarning($"The language '{newLanguageKey}' already exists in the dictionary.");
            }
        }
        
        /*- Remove Language -*/
        private void RemoveLanguage(string langToRemove)
        {
            if (_languages.Contains(langToRemove))
            {
                _languages.Remove(langToRemove);
    
                foreach (var key in _translations.Keys)
                {
                    _translations[key].Remove(langToRemove);
                }
    
                Debug.Log($"{langToRemove} removed from translations.");
            }
            else
            {
                Debug.LogWarning("Language to remove does not exist.");
            }
        }

    #endregion

    // Keys
    #region Keys

        /*- New Key -*/
        private void AddNewKey()
        {
            string newKey = "New Key";
            if (!_translations.ContainsKey(newKey))
            {
                _translations.Add(newKey, new Dictionary<string, string>());
                foreach (var lang in _languages)
                {
                    _translations[newKey].Add(lang, "");
                }
            }
        }
        
        /*- Remove Key -*/
        private void RemoveKey(string key)
        {
            if (_translations.ContainsKey(key))
            {
                _translations.Remove(key);
                Debug.Log($"Key '{key}' removed successfully.");
            }
            else
            {
                Debug.LogWarning($"Key '{key}' does not exist.");
            }
        }

    #endregion

    // Save
    #region Save
    
    /*- Saving -*/
        private void SaveTranslations()
        {
            string filePath = "Assets/Resources/translations.json";
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory("Assets/Resources");
                File.Create(filePath).Close();
            }
    
            TranslationList translationList = new TranslationList();
            foreach (var keyValuePair in _translations)
            {
                translationList.translations.Add(new TranslationData(keyValuePair.Key, keyValuePair.Value));
            }
            
            string json = JsonUtility.ToJson(translationList, true);
            File.WriteAllText(filePath, json);
            
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
                {
                    AssetDatabase.Refresh();
                    UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                }
            };
        }
    
        /*- Load Translation -*/
        private void LoadTranslations()
        {
            string filePath = "Assets/Resources/translations.json";
    
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                TranslationList translationList = JsonUtility.FromJson<TranslationList>(json);
    
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
    
    #endregion
    
    /*- Import CSV -*/
    private void ImportCsv()
    {
        string path = EditorUtility.OpenFilePanel("Import Localization CSV", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string[] lines = File.ReadAllLines(path);
            if (lines.Length < 2)
            {
                Debug.LogError("Invalid CSV format. It must contain headers and at least one row.");
                return;
            }

            string[] headers = lines[0].Split(',');
            if (headers.Length < 2)
            {
                Debug.LogError("Invalid CSV headers. It must contain at least a Key column and one language.");
                return;
            }

            List<string> newLanguages = new List<string>();
            for (int i = 1; i < headers.Length; i++)
            {
                string lang = headers[i].Split('(')[0].Trim();
                if (!_languages.Contains(lang))
                {
                    _languages.Add(lang);
                }
                newLanguages.Add(lang);
            }

            // Parse rows
            for (int i = 1; i < lines.Length; i++)
            {
                string[] columns = lines[i].Split(',');
                if (columns.Length < 2) continue;

                string key = columns[0];
                if (!_translations.ContainsKey(key))
                {
                    _translations[key] = new Dictionary<string, string>();
                }

                for (int j = 1; j < columns.Length; j++)
                {
                    string lang = newLanguages[j - 1];
                    string translation = columns[j];
                    _translations[key][lang] = translation;
                }
            }

            Debug.Log("CSV imported successfully.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error importing CSV: {ex.Message}");
        }
    }
    
    public static List<string> GetAllTranslationKeys()
    {
        List<string> keys = new List<string>(_translations.Keys);
        return keys;
    }
    
    public static string GetTranslation(string key)
    {
        if (_translations.ContainsKey(key) && _translations[key].ContainsKey("en"))
        {
            return _translations[key]["en"];
        }

        return $"Missing[{key}]";
    }

    // Data
   #region Data
   
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
        
        [System.Serializable]
        public class TranslationList
        {
            public List<TranslationData> translations = new List<TranslationData>();
        }
   
   #endregion
}