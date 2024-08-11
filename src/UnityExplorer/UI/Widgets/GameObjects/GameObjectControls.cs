using UnityEngine;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.GameObjects
{
    // The base wrapper to hold a reference to the parent Inspector and the GameObjectInfo and TransformControls widgets.

    public class GameObjectControls
    {
        public GameObjectControls(GameObjectInspector parent)
        {
            Parent = parent;

            GameObjectInfo = new GameObjectInfoPanel(this);
            TransformControl = new TransformControls(this);
        }

        public GameObjectInspector Parent { get; }

        public GameObject Target =>
            Parent.Target;

        public GameObjectInfoPanel GameObjectInfo { get; }

        public TransformControls TransformControl { get; }

        public void UpdateGameObjectInfo(bool firstUpdate,
            bool force)
        {
            GameObjectInfo.UpdateGameObjectInfo(firstUpdate, force);
        }

        public void UpdateVectorSlider()
        {
            TransformControl.UpdateVectorSlider();
        }
    }
}
