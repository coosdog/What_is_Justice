using UnityEngine;

public sealed class NpcInquiryObject : ClickableInquiryObject
{
    [SerializeField] private NpcInquiryData inquiryData;
    [SerializeField] private string npcId;
    [SerializeField] private NpcInquiryManager inquiryManager;

    protected override void Awake()
    {
        base.Awake();
        if (inquiryManager == null)
        {
            inquiryManager = FindFirstObjectByType<NpcInquiryManager>();
        }
    }

    protected override void OnClicked()
    {
        if (inquiryManager == null)
        {
            return;
        }

        if (inquiryData != null)
        {
            inquiryManager.StartInquiry(inquiryData);
        }
        else
        {
            inquiryManager.StartInquiry(npcId);
        }
    }
}
