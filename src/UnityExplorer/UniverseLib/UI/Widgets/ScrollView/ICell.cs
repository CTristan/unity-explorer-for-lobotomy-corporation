using UnityEngine;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.ObjectPool;

namespace UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView
{
    public interface ICell : IPooledObject
    {
        bool Enabled { get; }

        RectTransform Rect { get; set; }

        void Enable();
        void Disable();
    }
}
