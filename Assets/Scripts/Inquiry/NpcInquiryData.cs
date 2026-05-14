using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NpcInquiryData", menuName = "Game/Dialogue/NPC Inquiry Data")]
public sealed class NpcInquiryData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string npcId;
    [SerializeField] private string displayName;

    [Header("Fallback Dialogue")]
    [SerializeField] private string[] noKeywordDialogueIds;
    [SerializeField] private string[] unknownKeywordDialogueIds;

    [TextArea(2, 5)]
    [SerializeField] private string noKeywordFallbackText = "아직 물어볼 만한 단서가 없다.";

    [TextArea(2, 5)]
    [SerializeField] private string unknownKeywordFallbackText = "그 이야기에 대해서는 아는 것이 없다.";

    [Header("Keyword Responses")]
    [SerializeField] private NpcInquiryTopic[] topics;

    public string NpcId => npcId;
    public string DisplayName => displayName;
    public string NoKeywordFallbackText => noKeywordFallbackText;
    public string UnknownKeywordFallbackText => unknownKeywordFallbackText;

    public IEnumerable<NpcInquiryTopic> Topics
    {
        get
        {
            if (topics == null)
            {
                yield break;
            }

            foreach (NpcInquiryTopic topic in topics)
            {
                if (topic != null && topic.Keyword != null)
                {
                    yield return topic;
                }
            }
        }
    }

    public IEnumerable<string> EnumerateNoKeywordDialogueIds()
    {
        return EnumerateDialogueIds(noKeywordDialogueIds);
    }

    public IEnumerable<string> EnumerateUnknownKeywordDialogueIds()
    {
        return EnumerateDialogueIds(unknownKeywordDialogueIds);
    }

    public bool TryGetTopic(KeywordData keyword, out NpcInquiryTopic topic)
    {
        topic = null;
        if (keyword == null || topics == null)
        {
            return false;
        }

        foreach (NpcInquiryTopic candidate in topics)
        {
            if (candidate?.Keyword == keyword || candidate?.Keyword?.KeywordId == keyword.KeywordId)
            {
                topic = candidate;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateDialogueIds(string[] ids)
    {
        if (ids == null)
        {
            yield break;
        }

        foreach (string id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                yield return id;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            npcId = name;
        }
    }
#endif
}
