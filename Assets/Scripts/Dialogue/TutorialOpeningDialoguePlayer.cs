using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TutorialOpeningDialoguePlayer : MonoBehaviour
{
    private static readonly string[] OpeningDialogueIds =
    {
        "tutorial.opening.001",
        "tutorial.opening.002",
        "tutorial.opening.003",
        "tutorial.opening.004",
        "tutorial.opening.005",
        "tutorial.opening.006",
        "tutorial.opening.007",
    };

    private const string TutorialSceneName = "TutorialScene";

    [SerializeField] private InvestigationUI investigationUI;
    [SerializeField] private CsvDialogueDatabase dialogueDatabase;
    [SerializeField] private float startDelaySeconds = 0.25f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForTutorialScene()
    {
        if (SceneManager.GetActiveScene().name != TutorialSceneName)
        {
            return;
        }

        if (FindFirstObjectByType<TutorialOpeningDialoguePlayer>() != null)
        {
            return;
        }

        GameObject playerObject = new("TutorialOpeningDialoguePlayer");
        playerObject.AddComponent<TutorialOpeningDialoguePlayer>();
    }

    private IEnumerator Start()
    {
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
        foreach (string dialogueId in OpeningDialogueIds)
        {
            if (dialogueDatabase != null && dialogueDatabase.TryGetEntry(dialogueId, out DialogueEntry entry))
            {
                string speaker = string.IsNullOrWhiteSpace(entry.Speaker) ? "기록" : entry.Speaker;
                lines.Add(new DialogueLine(speaker, entry.Text, entry.PortraitKey, entry.Emotion));
            }
        }

        if (lines.Count > 0)
        {
            return lines;
        }

        lines.Add(new DialogueLine("쥐 조수", "다녀왔어요. 손은 비었지만 몸은 무사합니다.", "assistant", "awkward"));
        lines.Add(new DialogueLine("올빼미 탐정", "무사하다는 게 문제군. 이번에도 일거리를 못 가져왔다면, 오늘 저녁은 널 잡아먹을 수밖에 없어.", "player", "dry"));
        lines.Add(new DialogueLine("쥐 조수", "그런 농담 할 때마다 제 수명이 사흘씩 줄어드는 거 아세요?", "assistant", "protest"));
        lines.Add(new DialogueLine("쥐 조수", "그래도 일거리라면 있어요. 작지만 냄새가 수상한 의뢰요.", "assistant", "careful"));
        return lines;
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
