using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ChapterOpeningDialoguePlayer : MonoBehaviour
{
    [SerializeField] private InvestigationUI investigationUI;
    [SerializeField] private CsvDialogueDatabase dialogueDatabase;
    [SerializeField] private string openingDialoguePrefix;
    [SerializeField] private int maxOpeningDialogueCount = 30;
    [SerializeField] private float startDelaySeconds = 0.25f;
    [SerializeField] private bool playOnSceneStart = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForLoadedScene()
    {
        if (FindFirstObjectByType<ChapterOpeningDialoguePlayer>() != null)
        {
            return;
        }

        GameObject playerObject = new("ChapterOpeningDialoguePlayer");
        playerObject.AddComponent<ChapterOpeningDialoguePlayer>();
    }

    private IEnumerator Start()
    {
        if (!playOnSceneStart)
        {
            yield break;
        }

        ResolveReferences();

        if (startDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(startDelaySeconds);
        }
        else
        {
            yield return null;
        }

        ResolveReferences();
        if (investigationUI == null || investigationUI.IsVisible)
        {
            yield break;
        }

        List<DialogueLine> lines = BuildOpeningLines();
        if (lines.Count > 0)
        {
            investigationUI.ShowSequence(lines);
        }
    }

    private List<DialogueLine> BuildOpeningLines()
    {
        List<DialogueLine> lines = new();
        string prefix = ResolveOpeningDialoguePrefix();
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return lines;
        }

        int maxCount = Mathf.Max(1, maxOpeningDialogueCount);
        for (int i = 1; i <= maxCount; i++)
        {
            string dialogueId = $"{prefix}.{i:000}";
            if (dialogueDatabase != null && dialogueDatabase.TryGetEntry(dialogueId, out DialogueEntry entry))
            {
                lines.Add(new DialogueLine(
                    entry.Speaker,
                    entry.Text,
                    entry.PortraitKey,
                    entry.Emotion,
                    entry.BoardNodeId,
                    entry.BoardDisplayName,
                    entry.BoardDescription,
                    entry.ShowPortraits,
                    entry.PortraitLayout));
            }
        }

        return lines;
    }

    private string ResolveOpeningDialoguePrefix()
    {
        if (!string.IsNullOrWhiteSpace(openingDialoguePrefix))
        {
            return openingDialoguePrefix.Trim().TrimEnd('.');
        }

        return InferOpeningDialoguePrefix(SceneManager.GetActiveScene().name);
    }

    private static string InferOpeningDialoguePrefix(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return string.Empty;
        }

        if (sceneName.IndexOf("Tutorial", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "tutorial.opening";
        }

        if (sceneName.IndexOf("Chapter1", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "chapter1.opening";
        }

        if (sceneName.IndexOf("Chapter2", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "chapter2.opening";
        }

        if (sceneName.IndexOf("Chapter3", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "chapter3.opening";
        }

        return string.Empty;
    }

    private void ResolveReferences()
    {
        if (investigationUI == null)
        {
            investigationUI = FindFirstObjectByType<InvestigationUI>();
        }

        if (dialogueDatabase == null)
        {
            dialogueDatabase = FindFirstObjectByType<CsvDialogueDatabase>();
        }
    }
}
