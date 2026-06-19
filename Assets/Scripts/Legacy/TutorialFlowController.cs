using System.Collections;
using System;
using UnityEngine;

public sealed class TutorialFlowController : MonoBehaviour
{
    private const string SpeakerChild = "child_owl";
    private const string SpeakerGrandfather = "old_owl";
    private const string SpeakerNarration = "system_narration";
    private const string DeliverLetterEventId = "tutorial.deliver_letter_to_grandfather";

    private enum TutorialStage
    {
        NotStarted,
        IntroMonologue,
        MailArrived,
        WaitingForEntrance,
        WaitingForLetterPickup,
        WaitingForGrandfather,
        LetterDelivered
    }

    [Header("Scene References")]
    [SerializeField] private InvestigationUI investigationUI;
    [SerializeField] private MapNavigationManager mapNavigationManager;
    [SerializeField] private MapArea letterArea;
    [SerializeField] private InventoryPickupObject letterPickup;

    [Header("Tutorial Settings")]
    [SerializeField] private float startDelaySeconds = 0.25f;
    [SerializeField] private bool activateLetterAfterReachingArea = true;

    private TutorialStage _stage = TutorialStage.NotStarted;
    private bool _letterPickedUp;
    private bool _letterDelivered;

    private void Awake()
    {
        ResolveReferences();

        if (letterPickup != null && activateLetterAfterReachingArea)
        {
            letterPickup.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        GameEventBus.Raised += HandleGameEvent;

        if (letterPickup != null)
        {
            letterPickup.PickedUp += HandleLetterPickedUp;
        }
    }

    private void OnDisable()
    {
        GameEventBus.Raised -= HandleGameEvent;

        if (letterPickup != null)
        {
            letterPickup.PickedUp -= HandleLetterPickedUp;
        }
    }

    private void Start()
    {
        StartCoroutine(RunTutorialFlow());
    }

    public void DeliverLetterToGrandfather()
    {
        if (_letterDelivered)
        {
            return;
        }

        _letterDelivered = true;
        _stage = TutorialStage.LetterDelivered;

        ShowLines(
            new DialogueLine(SpeakerChild, "\uD560\uC544\uBC84\uC9C0, \uBB38 \uC55E\uC5D0 \uC774\uB7F0 \uD3B8\uC9C0\uAC00 \uB5A8\uC5B4\uC838 \uC788\uC5C8\uC5B4\uC694. \uBC1C\uC2E0\uC778\uB3C4 \uC5C6\uACE0 \uBD09\uD22C\uC5D0 \uC774\uC0C1\uD55C \uBB38\uC591\uC774 \uC788\uC5B4\uC694.", "child_owl", "curious"),
            new DialogueLine(SpeakerGrandfather, "\uD760. \uAE00\uC528\uB294 \uC228\uAE30\uACE0 \uC2F6\uC5B4 \uD558\uACE0, \uBB38\uC591\uC740 \uBCF4\uC5EC\uC8FC\uACE0 \uC2F6\uC5B4 \uD558\uB294\uAD70.", "old_owl", "calm"),
            new DialogueLine(SpeakerChild, "\uADF8\uAC8C \uBB34\uC2A8 \uB73B\uC774\uC5D0\uC694?", "child_owl", "puzzled"),
            new DialogueLine(SpeakerGrandfather, "\uC218\uC218\uAED8\uB07C\uB77C\uB294 \uB73B\uC774\uC9C0. \uADF8\uB9AC\uACE0 \uB9C8\uCE68 \uB124\uAC00 \uD480\uC5B4\uBCFC \uB9CC\uD55C \uC815\uB3C4\uB2E4.", "old_owl", "teaching"),
            new DialogueLine(SpeakerGrandfather, "\uCD08\uB300\uC7A5\uC740 \uB124\uAC8C \uB9E1\uAE30\uB9C8. \uC548\uC5D0 \uC801\uD78C \uB2E8\uC11C\uC640 \uB9C8\uBC95\uC9C4\uC744 \uCC28\uADFC\uCC28\uADFC \uC0B4\uD3B4\uBCF4\uAC70\uB77C.", "old_owl", "teaching"));
    }

    private IEnumerator RunTutorialFlow()
    {
        ResolveReferences();

        if (startDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(startDelaySeconds);
        }

        _stage = TutorialStage.IntroMonologue;
        ShowLines(
            new DialogueLine(SpeakerChild, "\uC6B0\uB9AC \uD560\uC544\uBC84\uC9C0\uB294 \uC774\uB984\uB09C \uD0D0\uC815\uC774\uB2E4. \uC624\uB798\uB41C \uC0AC\uAC74\uB3C4, \uC544\uBB34\uB3C4 \uBCF4\uC9C0 \uBABB\uD55C \uAC70\uC9D3\uB9D0\uB3C4, \uD560\uC544\uBC84\uC9C0 \uB208\uC55E\uC5D0\uC11C\uB294 \uC624\uB798 \uC228\uC9C0 \uBABB\uD588\uB2E4.", "child_owl", "admire", showPortraits: true, portraitLayout: "single"),
            new DialogueLine(SpeakerChild, "\uB098\uB3C4 \uC5B8\uC820\uAC00\uB294 \uADF8\uB7F0 \uD0D0\uC815\uC774 \uB418\uACE0 \uC2F6\uB2E4. \uB2E8\uC11C\uB97C \uBCF4\uACE0, \uB9D0 \uC0AC\uC774\uC758 \uBE48\uD2C8\uC744 \uB4E3\uACE0, \uB9C8\uC9C0\uB9C9\uC5D0\uB294 \uC9C4\uC2E4\uC744 \uAEBC\uB0B4\uB294 \uD0D0\uC815.", "child_owl", "hopeful", showPortraits: true, portraitLayout: "single"),
            new DialogueLine(SpeakerChild, "\uBB3C\uB860 \uC9C0\uAE08\uC758 \uB098\uB294 \uC544\uC9C1 \uCD08\uC9DC\uB2E4. \uC0AC\uAC74\uBCF4\uB2E4 \uBA3C\uC800 \uCC3B\uC794\uC744 \uC5CE\uACE0, \uCD94\uB9AC\uBCF4\uB2E4 \uBA3C\uC800 \uBC30\uAC00 \uACE0\uD508 \uCABD\uC5D0 \uAC00\uAE5D\uC9C0\uB9CC.", "child_owl", "awkward", showPortraits: true, portraitLayout: "single"));
        yield return WaitUntilDialogueClosed();

        _stage = TutorialStage.MailArrived;
        Debug.Log("[Tutorial] Mail carrier sound: a bell rings, and something falls near the door.");
        ShowLines(
            new DialogueLine(SpeakerNarration, "\uADF8\uB54C, \uBB38 \uCABD\uC5D0\uC11C \uC791\uC740 \uC885\uC18C\uB9AC\uC640 \uD568\uAED8 \uC885\uC774\uAC00 \uBC14\uB2E5\uC5D0 \uB2FF\uB294 \uC18C\uB9AC\uAC00 \uB0AC\uB2E4.", "narration", showPortraits: false),
            new DialogueLine(SpeakerChild, "\uC6B0\uD3B8\uC778\uAC00? \uBB38\uC774 \uC788\uB294 \uCABD\uC73C\uB85C \uAC00\uC11C \uD655\uC778\uD574\uBCF4\uC790.", "child_owl", "curious", showPortraits: true, portraitLayout: "single"),
            new DialogueLine(SpeakerNarration, "\uC624\uB978\uCABD \uC774\uB3D9 \uD654\uC0B4\uD45C\uB97C \uB20C\uB7EC \uC785\uAD6C\uAC00 \uC788\uB294 \uACF5\uAC04\uC73C\uB85C \uC774\uB3D9\uD560 \uC218 \uC788\uB2E4.", "system", showPortraits: false));
        yield return WaitUntilDialogueClosed();

        _stage = TutorialStage.WaitingForEntrance;
        yield return new WaitUntil(() => mapNavigationManager != null && letterArea != null && mapNavigationManager.CurrentArea == letterArea);

        if (letterPickup != null)
        {
            letterPickup.gameObject.SetActive(true);
        }

        ShowLines(
            new DialogueLine(SpeakerChild, "\uBB38 \uC55E\uC5D0 \uD3B8\uC9C0\uAC00 \uB5A8\uC5B4\uC838 \uC788\uB2E4. \uD074\uB9AD\uD574\uC11C \uC8FC\uC6CC\uBCF4\uC790.", "child_owl", "focused", showPortraits: true, portraitLayout: "single"));
        yield return WaitUntilDialogueClosed();

        _stage = TutorialStage.WaitingForLetterPickup;
        yield return new WaitUntil(() => _letterPickedUp);

        ShowLines(
            new DialogueLine(SpeakerNarration, "\uC758\uBB38\uC758 \uCD08\uB300\uC7A5\uC744 \uC778\uBCA4\uD1A0\uB9AC\uC5D0 \uB123\uC5C8\uB2E4.", "system", showPortraits: false),
            new DialogueLine(SpeakerChild, "\uC774\uAC74 \uD560\uC544\uBC84\uC9C0\uAED8 \uBA3C\uC800 \uBCF4\uC5EC\uB4DC\uB9AC\uB294 \uAC8C \uC88B\uACA0\uB2E4.", "child_owl", "focused", showPortraits: true, portraitLayout: "single"));
        yield return WaitUntilDialogueClosed();

        _stage = TutorialStage.WaitingForGrandfather;
        yield return new WaitUntil(() => _letterDelivered);
    }

    private void HandleLetterPickedUp(InventoryPickupObject pickupObject, InventoryItem item)
    {
        _letterPickedUp = true;
    }

    private void HandleGameEvent(string eventId)
    {
        if (string.Equals(eventId, DeliverLetterEventId, StringComparison.OrdinalIgnoreCase))
        {
            DeliverLetterToGrandfather();
        }
    }

    private IEnumerator WaitUntilDialogueClosed()
    {
        yield return null;
        while (investigationUI != null && investigationUI.IsVisible)
        {
            yield return null;
        }
    }

    private void ShowLines(params DialogueLine[] lines)
    {
        if (investigationUI == null)
        {
            ResolveReferences();
        }

        investigationUI?.ShowSequence(lines);
    }

    private void ResolveReferences()
    {
        if (investigationUI == null)
        {
            investigationUI = FindFirstObjectByType<InvestigationUI>();
        }

        if (mapNavigationManager == null)
        {
            mapNavigationManager = FindFirstObjectByType<MapNavigationManager>();
        }

        if (letterPickup == null)
        {
            letterPickup = FindFirstObjectByType<InventoryPickupObject>(FindObjectsInactive.Include);
        }

    }
}
