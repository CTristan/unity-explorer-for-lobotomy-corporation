using System;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Panels;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI
{
    internal class ExplorerUIBase : UIBase
    {
        public ExplorerUIBase(string id, Action updateMethod) : base(id, updateMethod) { }

        protected override PanelManager CreatePanelManager()
        {
            return new UEPanelManager(this);
        }
    }
}
