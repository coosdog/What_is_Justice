using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class NpcInquiryTopic
{
    [SerializeField] private KeywordData keyword;
    [SerializeField] private string[] responseDialogueIds;

    [TextArea(2, 5)]
    [SerializeField] private string fallbackResponseText;

    public KeywordData Keyword => keyword;
    public string FallbackResponseText => fallbackResponseText;

    public IEnumerable<string> EnumerateResponseDialogueIds()
    {
        if (responseDialogueIds == null)
        {
            yield break;
        }

        foreach (string id in responseDialogueIds)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                yield return id;
            }
        }
    }
}
