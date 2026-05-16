using System.Collections.Generic;
using UnityEngine;

public sealed class NpcProfileRegistry : MonoBehaviour
{
    [SerializeField] private NpcProfile[] profiles;

    public IEnumerable<NpcProfile> Profiles
    {
        get
        {
            if (profiles == null)
            {
                yield break;
            }

            foreach (NpcProfile profile in profiles)
            {
                if (profile != null && !string.IsNullOrWhiteSpace(profile.displayName))
                {
                    yield return profile;
                }
            }
        }
    }
}
