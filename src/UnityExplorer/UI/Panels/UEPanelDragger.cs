using UnityEngine;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Panels;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels
{
    public class UEPanelDragger : PanelDragger
    {
        public UEPanelDragger(PanelBase uiPanel) : base(uiPanel) { }

        protected override bool MouseInResizeArea(Vector2 mousePos)
        {
            return !UIManager.NavBarRect.rect.Contains(UIManager.NavBarRect.InverseTransformPoint(mousePos))
                && base.MouseInResizeArea(mousePos);
        }
    }
}
