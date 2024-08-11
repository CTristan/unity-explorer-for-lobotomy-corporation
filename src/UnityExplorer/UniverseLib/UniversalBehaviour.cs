using UnityEngine;

namespace UnityExplorerForLobotomyCorporation.UniverseLib
{
    /// <summary>Used for receiving Update events and starting Coroutines.</summary>
    internal class UniversalBehaviour : MonoBehaviour
    {
        internal static UniversalBehaviour Instance { get; private set; }

        internal void Update()
        {
            Universe.Update();
        }

        internal static void Setup()
        {
            var obj = new GameObject("UniverseLibBehaviour");
            DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<UniversalBehaviour>();
        }
    }
}
