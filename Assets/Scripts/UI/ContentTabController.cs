using System;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class ContentTabController : MonoBehaviour
{
    [Serializable]
    private sealed class ContentTab
    {
        public string name = string.Empty;
        public Button button = null;
        public GameObject view = null;
        public Graphic tabGraphic = null;
    }

    [SerializeField] private ContentTab[] tabs = Array.Empty<ContentTab>();
    [SerializeField] private int defaultTabIndex;

    [Header("Keyboard Shortcuts")]
    [SerializeField] private bool enableKeyboardShortcuts = true;
    [SerializeField] private KeyCode storyKey = KeyCode.S;
    [SerializeField] private KeyCode npcInfoKey = KeyCode.N;
    [SerializeField] private KeyCode evidenceCombineKey = KeyCode.None;
    [SerializeField] private KeyCode evidenceCheckKey = KeyCode.None;
    [SerializeField] private KeyCode inventoryKey = KeyCode.B;
    [SerializeField] private KeyCode mantraKey = KeyCode.M;

    [Header("Tab Colors")]
    [SerializeField] private bool updateTabColors = true;
    [SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color normalColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    public int CurrentTabIndex { get; private set; } = -1;

    private void Awake()
    {
        BindButtons();
        ShowTab(defaultTabIndex);
    }

    private void OnValidate()
    {
        if (defaultTabIndex < 0)
        {
            defaultTabIndex = 0;
        }
    }

    private void Update()
    {
        if (!enableKeyboardShortcuts)
        {
            return;
        }

        if (IsKeyPressed(storyKey))
        {
            ShowStory();
        }
        else if (IsKeyPressed(npcInfoKey))
        {
            ShowNpcInfo();
        }
        else if (IsKeyPressed(evidenceCombineKey))
        {
            ShowEvidenceCombine();
        }
        else if (IsKeyPressed(evidenceCheckKey))
        {
            ShowEvidenceCheck();
        }
        else if (IsKeyPressed(inventoryKey))
        {
            ShowInventory();
        }
        else if (IsKeyPressed(mantraKey))
        {
            ShowMantra();
        }
    }

    public void ShowStory()
    {
        ShowTab(0);
    }

    public void ShowNpcInfo()
    {
        ShowTab(1);
    }

    public void ShowEvidenceCombine()
    {
        ShowTab(2);
    }

    public void ShowEvidenceCheck()
    {
        ShowTab(3);
    }

    public void ShowInventory()
    {
        ShowTab(4);
    }

    public void ShowMantra()
    {
        ShowTab(5);
    }

    public void ShowTab(int tabIndex)
    {
        if (tabs == null || tabs.Length == 0)
        {
            return;
        }

        if (tabIndex < 0 || tabIndex >= tabs.Length)
        {
            tabIndex = 0;
        }

        CurrentTabIndex = tabIndex;

        for (int i = 0; i < tabs.Length; i++)
        {
            ContentTab tab = tabs[i];
            bool isSelected = i == tabIndex;

            if (tab.view != null)
            {
                tab.view.SetActive(isSelected);

                InventoryUI inventoryUI = isSelected ? tab.view.GetComponentInChildren<InventoryUI>(true) : null;
                if (inventoryUI != null)
                {
                    inventoryUI.RefreshView();
                }
            }

            if (updateTabColors && tab.tabGraphic != null)
            {
                tab.tabGraphic.color = isSelected ? selectedColor : normalColor;
            }
        }
    }

    private void BindButtons()
    {
        if (tabs == null)
        {
            return;
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            Button button = tabs[i].button;
            if (button == null)
            {
                continue;
            }

            int capturedIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowTab(capturedIndex));
        }
    }

    private static bool IsKeyPressed(KeyCode key)
    {
        if (key == KeyCode.None)
        {
            return false;
        }

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return key switch
        {
            KeyCode.B => keyboard.bKey.wasPressedThisFrame,
            KeyCode.C => keyboard.cKey.wasPressedThisFrame,
            KeyCode.E => keyboard.eKey.wasPressedThisFrame,
            KeyCode.M => keyboard.mKey.wasPressedThisFrame,
            KeyCode.N => keyboard.nKey.wasPressedThisFrame,
            KeyCode.S => keyboard.sKey.wasPressedThisFrame,
            _ => false
        };
#else
        return Input.GetKeyDown(key);
#endif
    }
}
