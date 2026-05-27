using System.Collections.Generic;
using UnityEngine;

public sealed class GameplayHudVisibilityController : MonoBehaviour
{
    [SerializeField] private GameObject[] managedHudObjects;
    [SerializeField] private bool autoFindDefaultHudObjects = true;

    private static GameplayHudVisibilityController _instance;

    private readonly HashSet<Object> _hideRequesters = new();
    private readonly Dictionary<GameObject, bool> _originalActiveStates = new();
    private readonly List<GameObject> _resolvedHudObjects = new();

    public static void RequestHide(Object requester)
    {
        if (requester == null)
        {
            return;
        }

        Instance.AddHideRequester(requester);
    }

    public static void ReleaseHide(Object requester)
    {
        if (requester == null || _instance == null)
        {
            return;
        }

        _instance.RemoveHideRequester(requester);
    }

    public void RegisterHudObject(GameObject hudObject)
    {
        if (hudObject == null || _resolvedHudObjects.Contains(hudObject))
        {
            return;
        }

        _resolvedHudObjects.Add(hudObject);
        ApplyVisibility();
    }

    public void UnregisterHudObject(GameObject hudObject)
    {
        if (hudObject == null)
        {
            return;
        }

        _resolvedHudObjects.Remove(hudObject);
        _originalActiveStates.Remove(hudObject);
    }

    private static GameplayHudVisibilityController Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = FindFirstObjectByType<GameplayHudVisibilityController>();
            if (_instance != null)
            {
                return _instance;
            }

            GameObject controllerObject = new("GameplayHudVisibilityController");
            _instance = controllerObject.AddComponent<GameplayHudVisibilityController>();
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        ResolveHudObjects();
        ApplyVisibility();
    }

    private void OnDisable()
    {
        RestoreOriginalStates();
    }

    private void AddHideRequester(Object requester)
    {
        ResolveHudObjects();
        if (_hideRequesters.Add(requester))
        {
            ApplyVisibility();
        }
    }

    private void RemoveHideRequester(Object requester)
    {
        if (_hideRequesters.Remove(requester))
        {
            ApplyVisibility();
        }
    }

    private void ApplyVisibility()
    {
        if (_hideRequesters.Count > 0)
        {
            HideHudObjects();
            return;
        }

        RestoreOriginalStates();
    }

    private void HideHudObjects()
    {
        foreach (GameObject hudObject in _resolvedHudObjects)
        {
            if (hudObject == null || ShouldSkipForActiveRequester(hudObject))
            {
                continue;
            }

            if (!_originalActiveStates.ContainsKey(hudObject))
            {
                _originalActiveStates.Add(hudObject, hudObject.activeSelf);
            }

            hudObject.SetActive(false);
        }
    }

    private void RestoreOriginalStates()
    {
        foreach (KeyValuePair<GameObject, bool> entry in _originalActiveStates)
        {
            if (entry.Key != null)
            {
                entry.Key.SetActive(entry.Value);
            }
        }

        _originalActiveStates.Clear();
    }

    private void ResolveHudObjects()
    {
        _resolvedHudObjects.Clear();

        if (managedHudObjects != null)
        {
            foreach (GameObject hudObject in managedHudObjects)
            {
                AddResolvedHudObject(hudObject);
            }
        }

        if (!autoFindDefaultHudObjects)
        {
            return;
        }

        foreach (PlayerDispositionHud hud in FindObjectsByType<PlayerDispositionHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            AddResolvedHudObject(hud.gameObject);
        }

        foreach (Transform target in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (target.name == "NotebookHint")
            {
                AddResolvedHudObject(target.gameObject);
            }
        }
    }

    private void AddResolvedHudObject(GameObject hudObject)
    {
        if (hudObject != null && !_resolvedHudObjects.Contains(hudObject))
        {
            _resolvedHudObjects.Add(hudObject);
        }
    }

    private bool ShouldSkipForActiveRequester(GameObject hudObject)
    {
        foreach (Object requester in _hideRequesters)
        {
            if (requester is not Component component)
            {
                continue;
            }

            Transform requesterTransform = component.transform;
            Transform hudTransform = hudObject.transform;
            if (hudTransform == requesterTransform ||
                hudTransform.IsChildOf(requesterTransform) ||
                requesterTransform.IsChildOf(hudTransform))
            {
                return true;
            }
        }

        return false;
    }
}
