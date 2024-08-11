using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CSConsole.Lexers;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.AutoComplete;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.Runtime.Mono;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.CSConsole
{
    public class CSAutoCompleter : ISuggestionProvider
    {
        private const string OPEN_HIGHLIGHT = "<color=cyan>";

        private readonly HashSet<char> delimiters = new HashSet<char>
        {
            '{',
            '}',
            ',',
            ';',
            '<',
            '>',
            '(',
            ')',
            '[',
            ']',
            '=',
            '|',
            '&',
            '?',
        };

        private readonly StringBuilder highlightBuilder = new StringBuilder();

        private readonly Dictionary<string, string> keywordHighlights = new Dictionary<string, string>();


        private readonly Dictionary<string, string> namespaceHighlights = new Dictionary<string, string>();

        private readonly List<Suggestion> suggestions = new List<Suggestion>();

        public InputFieldRef InputField =>
            ConsoleController.Input;

        public bool AnchorToCaretPosition =>
            true;

        bool ISuggestionProvider.AllowNavigation =>
            true;

        public void OnSuggestionClicked(Suggestion suggestion)
        {
            ConsoleController.InsertSuggestionAtCaret(suggestion.UnderlyingValue);
            AutoCompleteModal.Instance.ReleaseOwnership(this);
        }

        public void CheckAutocompletes()
        {
            if (string.IsNullOrEmpty(InputField.Text))
            {
                AutoCompleteModal.Instance.ReleaseOwnership(this);

                return;
            }

            suggestions.Clear();

            var caret = Math.Max(0, Math.Min(InputField.Text.Length - 1, InputField.Component.caretPosition - 1));
            var startIdx = caret;

            // If the character at the caret index is whitespace or delimiter,
            // or if the next character (if it exists) is not whitespace,
            // then we don't want to provide suggestions.
            if (char.IsWhiteSpace(InputField.Text[caret]) || delimiters.Contains(InputField.Text[caret]) || (InputField.Text.Length > caret + 1 && !char.IsWhiteSpace(InputField.Text[caret + 1])))
            {
                AutoCompleteModal.Instance.ReleaseOwnership(this);

                return;
            }

            // get the current composition string (from caret back to last delimiter)
            while (startIdx > 0)
            {
                startIdx--;
                var c = InputField.Text[startIdx];
                if (delimiters.Contains(c) || char.IsWhiteSpace(c))
                {
                    startIdx++;

                    break;
                }
            }

            var input = InputField.Text.Substring(startIdx, caret - startIdx + 1);

            // Get MCS completions

            var evaluatorCompletions = ConsoleController.Evaluator.GetCompletions(input, out var prefix);

            if (evaluatorCompletions != null && evaluatorCompletions.Any())
            {
                suggestions.AddRange(from completion in evaluatorCompletions select new Suggestion(GetHighlightString(prefix, completion), completion));
            }

            // Get manual namespace completions

            foreach (var ns in ReflectionUtility.AllNamespaces)
            {
                if (ns.StartsWith(input))
                {
                    if (!namespaceHighlights.ContainsKey(ns))
                    {
                        namespaceHighlights.Add(ns, $"<color=#CCCCCC>{ns}</color>");
                    }

                    var completion = ns.Substring(input.Length, ns.Length - input.Length);
                    suggestions.Add(new Suggestion(namespaceHighlights[ns], completion));
                }
            }

            // Get manual keyword completions

            foreach (var kw in KeywordLexer.keywords)
            {
                if (kw.StartsWith(input)) // && kw.Length > input.Length)
                {
                    if (!keywordHighlights.ContainsKey(kw))
                    {
                        keywordHighlights.Add(kw, $"<color=#{SignatureHighlighter.keywordBlueHex}>{kw}</color>");
                    }

                    var completion = kw.Substring(input.Length, kw.Length - input.Length);
                    suggestions.Add(new Suggestion(keywordHighlights[kw], completion));
                }
            }

            if (suggestions.Any())
            {
                AutoCompleteModal.TakeOwnership(this);
                AutoCompleteModal.Instance.SetSuggestions(suggestions);
            }
            else
            {
                AutoCompleteModal.Instance.ReleaseOwnership(this);
            }
        }

        private string GetHighlightString(string prefix,
            string completion)
        {
            highlightBuilder.Clear();
            highlightBuilder.Append(OPEN_HIGHLIGHT);
            highlightBuilder.Append(prefix);
            highlightBuilder.Append(SignatureHighlighter.CLOSE_COLOR);
            highlightBuilder.Append(completion);

            return highlightBuilder.ToString();
        }
    }
}
