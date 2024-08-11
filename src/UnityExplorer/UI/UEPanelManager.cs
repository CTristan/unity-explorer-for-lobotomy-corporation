using UnityEngine;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Panels;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI
{
    public class UEPanelManager : PanelManager
    {
        public UEPanelManager(UIBase owner) : base(owner)
        {
        }

        protected internal override Vector3 MousePosition =>
            DisplayManager.MousePosition;

        protected internal override Vector2 ScreenDimensions =>
            new Vector2(DisplayManager.Width, DisplayManager.Height);

        protected override bool MouseInTargetDisplay =>
            DisplayManager.MouseInTargetDisplay;

        internal void DoInvokeOnPanelsReordered()
        {
            InvokeOnPanelsReordered();
        }

        protected override void SortDraggerHeirarchy()
        {
            base.SortDraggerHeirarchy();

            // move AutoCompleter to first update
            if (!UIManager.Initializing && AutoCompleteModal.Instance != null)
            {
                draggerInstances.Remove(AutoCompleteModal.Instance.Dragger);
                draggerInstances.Insert(0, AutoCompleteModal.Instance.Dragger);
            }
        }
    }
}
