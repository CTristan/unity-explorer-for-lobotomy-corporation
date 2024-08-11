using System;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Config;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.Loader
{
    public interface IExplorerLoader
    {
        string ExplorerFolderDestination { get; }
        string ExplorerFolderName { get; }
        string UnhollowedModulesFolder { get; }

        ConfigHandler ConfigHandler { get; }

        Action<object> OnLogMessage { get; }
        Action<object> OnLogWarning { get; }
        Action<object> OnLogError { get; }
    }
}
