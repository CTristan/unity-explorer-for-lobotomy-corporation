using System.Reflection;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer
{
    public class ExplorerBehaviour : MonoBehaviour
    {
        internal bool quitting;
        internal static ExplorerBehaviour Instance { get; private set; }

        internal void Update()
        {
            ExplorerCore.Update();
        }

        // For editor, to clean up objects

        internal void OnDestroy()
        {
            OnApplicationQuit();
        }

        internal void OnApplicationQuit()
        {
            if (quitting)
            {
                return;
            }

            quitting = true;

            TryDestroy(UIManager.UIRoot?.transform.root.gameObject);

            TryDestroy((typeof(Universe).Assembly.GetType("UniverseLib.UniversalBehaviour").GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null) as Component)
                .gameObject);

            TryDestroy(gameObject);
        }

        internal static void Setup()
        {
            var obj = new GameObject("ExplorerBehaviour");
            DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<ExplorerBehaviour>();
        }

        internal void TryDestroy(GameObject obj)
        {
            try
            {
                if (obj)
                {
                    Destroy(obj);
                }
            }
            catch
            {
            }
        }
    }
}
