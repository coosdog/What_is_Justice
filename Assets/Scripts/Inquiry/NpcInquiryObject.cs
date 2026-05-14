using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PointerClick2D))]
public sealed class NpcInquiryObject : MonoBehaviour
{
    [SerializeField] private NpcInquiryData inquiryData;
    [SerializeField] private string npcId;
    [SerializeField] private NpcInquiryManager inquiryManager;

    private PointerClick2D _clickable;

    private void Awake()
    {
        _clickable = GetComponent<PointerClick2D>();
        if (inquiryManager == null)
        {
            inquiryManager = FindFirstObjectByType<NpcInquiryManager>();
        }
    }

    private void OnEnable()
    {
        if (_clickable != null)
        {
            _clickable.Clicked += HandleClicked;
        }
    }

    private void OnDisable()
    {
        if (_clickable != null)
        {
            _clickable.Clicked -= HandleClicked;
        }
    }

    private void HandleClicked()
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
