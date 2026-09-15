using CookedOut.Domain;
using UnityEngine;

namespace CookedOut.Presentation
{
    public sealed class PlayerPrefsSaveStore : ISaveStore
    {
        private const string Prefix = "CookedOut.";

        public void Save(string slotId, string serializedState)
        {
            PlayerPrefs.SetString(Prefix + slotId, serializedState);
            PlayerPrefs.Save();
        }

        public bool TryLoad(string slotId, out string serializedState)
        {
            var key = Prefix + slotId;
            if (!PlayerPrefs.HasKey(key))
            {
                serializedState = null;
                return false;
            }

            serializedState = PlayerPrefs.GetString(key);
            return true;
        }

        public static string KeyFor(string slotId)
        {
            return Prefix + slotId;
        }
    }
}
