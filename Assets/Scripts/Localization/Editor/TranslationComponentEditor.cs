#if UNITY_EDITOR
using TMPro;
using UnityEditor;

[CustomEditor(typeof(TranslationComponent))]
public class TranslationComponentEditor : Editor
{
    private TranslationComponent _translationComponent;
    private TMP_Text _textComponent;

    private void OnEnable()
    {
        _translationComponent = (TranslationComponent)target;
        _textComponent = _translationComponent.GetComponent<TMP_Text>();
        if (_translationComponent._localizationKey != null)
        {
            _textComponent.SetText(Localization.GetTranslation(_translationComponent._localizationKey));
        }
    }

    public override void OnInspectorGUI()
    {
        if (_translationComponent.IsTranslatable)
        {
            var keys = Localization.GetAllTranslationKeys();
            EditorGUILayout.LabelField("Select a Translation Key", EditorStyles.boldLabel);

            int currentIndex = keys.IndexOf(_translationComponent._localizationKey);
            if (currentIndex < 0) currentIndex = 0;

            int selectedIndex = EditorGUILayout.Popup("Translation Key", currentIndex, keys.ToArray());

            if (selectedIndex != currentIndex)
            {
                string selectedKey = keys[selectedIndex];
                if (_translationComponent && _textComponent)
                {
                    ChangeKey(selectedKey);
                    EditorUtility.SetDirty(target);
                }
            }
        }
        base.OnInspectorGUI();
    }

    public void ChangeKey(string newKey)
    {
        _translationComponent._localizationKey = newKey;
        _textComponent.SetText(Localization.GetTranslation(newKey));
    }
    
    private void OnDestroy()
    {
        var localizationEditor = FindFirstObjectByType<Localization>();
        if (localizationEditor != null && localizationEditor.GetTexts() != null)
        {
            localizationEditor.GetTexts().Remove(_textComponent);
        }
    }
}
#endif