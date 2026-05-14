using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class TmpTextPrewarmUtility
{
    public static void Prewarm(TMP_Text text, string sampleText = null)
    {
        if (text == null)
        {
            return;
        }

        string originalText = text.text;
        string textToPrewarm = string.IsNullOrEmpty(sampleText) ? originalText : sampleText;

        if (text.font != null && !string.IsNullOrEmpty(textToPrewarm))
        {
            if (!text.font.TryAddCharacters(textToPrewarm, out string missingCharacters) &&
                !string.IsNullOrEmpty(missingCharacters))
            {
                Debug.LogWarning($"{text.name} could not add TMP characters: {missingCharacters}");
            }
        }

        text.text = textToPrewarm;
        text.ForceMeshUpdate(true, true);
        text.text = originalText;
        text.ForceMeshUpdate(true, true);
    }

    public static void Prewarm(Button button, string sampleText = null)
    {
        if (button == null)
        {
            return;
        }

        Prewarm(button.GetComponentInChildren<TMP_Text>(true), sampleText);
    }
}
