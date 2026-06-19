using UnityEngine;

public sealed class StoryChoiceInventoryTester : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private string itemId = "test.letter";
    [SerializeField] private string displayName = "테스트 편지";
    [TextArea(2, 5)]
    [SerializeField] private string description = "선택지 버튼 테스트로 획득한 편지다.";
    [TextArea(2, 5)]
    [SerializeField] private string inspectResultText = "편지 표면을 살펴보니 희미한 문양이 남아 있다.";
    [SerializeField] private Sprite icon;
    [SerializeField] private string rewardEvidenceId;
    [SerializeField] private string rewardKeywordId;

    private void Awake()
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }
    }

    public void AddTestItem()
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (inventoryManager == null)
        {
            Debug.LogWarning($"{nameof(StoryChoiceInventoryTester)} could not find an InventoryManager.", this);
            return;
        }

        InventoryItem item = new(
            itemId,
            displayName,
            description,
            inspectResultText,
            icon,
            rewardEvidenceId,
            rewardKeywordId);

        if (!inventoryManager.AddItem(item))
        {
            Debug.Log($"Inventory item was not added. It may already exist: {itemId}", this);
        }
    }
}
