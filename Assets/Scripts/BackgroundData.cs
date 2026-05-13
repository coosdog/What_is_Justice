using UnityEngine;

[CreateAssetMenu(fileName = "BackgroundData", menuName = "Game/Scene/Background Data")]
public sealed class BackgroundData : ScriptableObject
{
    [SerializeField] private string backgroundId;
    [SerializeField] private Sprite sprite;

    public string BackgroundId => backgroundId;
    public Sprite Sprite => sprite;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(backgroundId))
        {
            backgroundId = name;
        }
    }
#endif
}
