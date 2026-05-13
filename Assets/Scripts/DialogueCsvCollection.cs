using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueCsvCollection", menuName = "Game/Dialogue/CSV Collection")]
public sealed class DialogueCsvCollection : ScriptableObject
{
    [SerializeField] private TextAsset[] csvFiles;

    public IEnumerable<TextAsset> EnumerateCsvFiles()
    {
        if (csvFiles == null)
        {
            yield break;
        }

        foreach (TextAsset csvFile in csvFiles)
        {
            if (csvFile != null)
            {
                yield return csvFile;
            }
        }
    }
}
