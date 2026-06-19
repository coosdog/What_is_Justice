using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InventoryManager : MonoBehaviour
{
    [SerializeField] private InventoryItem[] startingItems = Array.Empty<InventoryItem>();

    private readonly Dictionary<string, InventoryItem> _items = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _itemOrder = new();

    public event Action<InventoryItem> ItemAdded;
    public event Action<InventoryItem> ItemInspected;
    public event Action<InventoryItem> ItemRemoved;

    public IReadOnlyCollection<InventoryItem> Items
    {
        get
        {
            List<InventoryItem> orderedItems = new(_itemOrder.Count);
            foreach (string itemId in _itemOrder)
            {
                if (_items.TryGetValue(itemId, out InventoryItem item))
                {
                    orderedItems.Add(item);
                }
            }

            return orderedItems;
        }
    }

    private void Awake()
    {
        if (startingItems == null)
        {
            return;
        }

        foreach (InventoryItem item in startingItems)
        {
            AddItem(item, false);
        }
    }

    public bool HasItem(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) && _items.ContainsKey(itemId.Trim());
    }

    public bool TryGetItem(string itemId, out InventoryItem item)
    {
        item = null;
        return !string.IsNullOrWhiteSpace(itemId) && _items.TryGetValue(itemId.Trim(), out item);
    }

    public bool AddItem(string itemId, string displayName, string description)
    {
        return AddItem(new InventoryItem(itemId, displayName, description), true);
    }

    public bool AddItem(
        string itemId,
        string displayName,
        string description,
        string inspectResultText,
        string rewardEvidenceId,
        string rewardKeywordId)
    {
        return AddItem(new InventoryItem(
            itemId,
            displayName,
            description,
            inspectResultText,
            rewardEvidenceId,
            rewardKeywordId), true);
    }

    public bool AddItem(InventoryItem item)
    {
        return AddItem(item, true);
    }

    private bool AddItem(InventoryItem item, bool notify)
    {
        if (item == null || !item.IsValid || HasItem(item.ItemId))
        {
            return false;
        }

        _items.Add(item.ItemId, item);
        _itemOrder.Add(item.ItemId);

        if (notify)
        {
            ItemAdded?.Invoke(item);
            Debug.Log($"Inventory item acquired: {item.DisplayName} ({item.ItemId})");
        }

        return true;
    }

    public bool RemoveItem(string itemId)
    {
        if (!TryGetItem(itemId, out InventoryItem item))
        {
            return false;
        }

        string normalizedId = item.ItemId;
        _items.Remove(normalizedId);
        _itemOrder.RemoveAll(id => string.Equals(id, normalizedId, StringComparison.OrdinalIgnoreCase));
        ItemRemoved?.Invoke(item);
        Debug.Log($"Inventory item removed: {item.DisplayName} ({item.ItemId})");
        return true;
    }

    public bool MarkInspected(string itemId)
    {
        if (!TryGetItem(itemId, out InventoryItem item) || item.Inspected)
        {
            return false;
        }

        item.MarkInspected();
        ItemInspected?.Invoke(item);
        return true;
    }
}
