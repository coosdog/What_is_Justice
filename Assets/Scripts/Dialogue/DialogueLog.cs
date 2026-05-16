using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DialogueLog : MonoBehaviour
{
    [SerializeField] private int maxEntries = 80;

    private readonly List<DialogueLine> _entries = new();

    public event Action Changed;
    public IReadOnlyList<DialogueLine> Entries => _entries;

    public void Add(DialogueLine line)
    {
        if (string.IsNullOrWhiteSpace(line.Text))
        {
            return;
        }

        _entries.Add(line);
        while (_entries.Count > maxEntries)
        {
            _entries.RemoveAt(0);
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        _entries.Clear();
        Changed?.Invoke();
    }
}
