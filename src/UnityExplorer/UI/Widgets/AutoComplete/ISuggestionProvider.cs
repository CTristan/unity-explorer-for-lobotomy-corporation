using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.AutoComplete
{
    public interface ISuggestionProvider
    {
        InputFieldRef InputField { get; }
        bool AnchorToCaretPosition { get; }

        bool AllowNavigation { get; }

        void OnSuggestionClicked(Suggestion suggestion);
    }
}
