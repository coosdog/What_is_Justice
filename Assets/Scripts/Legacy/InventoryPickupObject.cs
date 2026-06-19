using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PointerClick2D))]
public sealed class InventoryPickupObject : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private InvestigationUI investigationUI;
    [SerializeField] private string itemId = "tutorial.letter";
    [SerializeField] private string displayName = "\uC758\uBB38\uC758 \uCD08\uB300\uC7A5";
    [TextArea(2, 5)]
    [SerializeField] private string description = "\uBC1C\uC2E0\uC778\uC774 \uC801\uD600 \uC788\uC9C0 \uC54A\uC740 \uCD08\uB300\uC7A5. \uC548\uCABD\uC5D0\uB294 \uC218\uC218\uAED8\uB07C\uCC98\uB7FC \uBCF4\uC774\uB294 \uBB38\uC7A5\uACFC \uB9C8\uBC95\uC9C4 \uBB38\uC591\uC774 \uB0A8\uC544 \uC788\uB2E4.";
    [TextArea(2, 5)]
    [SerializeField] private string inspectResultText = "\uD45C\uBA74\uC5D0 \uB0A8\uC740 \uBB38\uC591\uACFC \uC7A5\uC2DD\uC744 \uC880 \uB354 \uC790\uC138\uD788 \uC0B4\uD3B4\uBCFC \uD544\uC694\uAC00 \uC788\uB2E4.";
    [SerializeField] private Sprite icon;
    [SerializeField] private string rewardEvidenceId;
    [SerializeField] private string rewardKeywordId;
    [SerializeField] private bool hideAfterPickup = true;

    private PointerClick2D _clickable;
    private bool _pickedUp;

    public event Action<InventoryPickupObject, InventoryItem> PickedUp;

    public string ItemId => itemId;
    public bool HasPickedUp => _pickedUp;

    private void Awake()
    {
        _clickable = GetComponent<PointerClick2D>();

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (investigationUI == null)
        {
            investigationUI = FindFirstObjectByType<InvestigationUI>();
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
        if (_pickedUp || investigationUI != null && investigationUI.IsVisible)
        {
            return;
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        InventoryItem item = new(
            itemId,
            displayName,
            description,
            inspectResultText,
            ResolveIcon(),
            rewardEvidenceId,
            rewardKeywordId);
        inventoryManager?.AddItem(item);
        _pickedUp = true;
        PickedUp?.Invoke(this, item);

        if (hideAfterPickup)
        {
            gameObject.SetActive(false);
        }
    }

    private Sprite ResolveIcon()
    {
        if (icon != null)
        {
            return icon;
        }

        if (TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            return spriteRenderer.sprite;
        }

        return null;
    }
}
