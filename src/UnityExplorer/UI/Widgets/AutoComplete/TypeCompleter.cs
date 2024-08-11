using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels;
using UnityExplorerForLobotomyCorporation.UniverseLib;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.AutoComplete
{
    public class TypeCompleter : ISuggestionProvider
    {
        private static readonly Dictionary<string, Type> shorthandToType = new Dictionary<string, Type>
        {
            {
                "object", typeof(object)
            },
            {
                "string", typeof(string)
            },
            {
                "bool", typeof(bool)
            },
            {
                "byte", typeof(byte)
            },
            {
                "sbyte", typeof(sbyte)
            },
            {
                "char", typeof(char)
            },
            {
                "decimal", typeof(decimal)
            },
            {
                "double", typeof(double)
            },
            {
                "float", typeof(float)
            },
            {
                "int", typeof(int)
            },
            {
                "uint", typeof(uint)
            },
            {
                "long", typeof(long)
            },
            {
                "ulong", typeof(ulong)
            },
            {
                "short", typeof(short)
            },
            {
                "ushort", typeof(ushort)
            },
            {
                "void", typeof(void)
            },
        };

        internal static readonly Dictionary<string, string> sharedTypeToLabel = new Dictionary<string, string>();

        private readonly bool allowAbstract;
        private readonly bool allowEnum;
        private readonly bool allowGeneric;
        private readonly Stopwatch cacheTypesStopwatch = new Stopwatch();

        private readonly List<Suggestion> loadingSuggestions = new List<Suggestion>
        {
            new Suggestion("<color=grey>Loading...</color>", ""),
        };

        private readonly HashSet<string> suggestedTypes = new HashSet<string>();

        private readonly List<Suggestion> suggestions = new List<Suggestion>();
        private HashSet<Type> allowedTypes;
        private string chosenSuggestion;
        private bool enabled = true;
        private Coroutine getSuggestionsCoroutine;
        private string pendingInput;

        public TypeCompleter(Type baseType,
            InputFieldRef inputField) : this(baseType, inputField, true, true, true)
        {
        }

        public TypeCompleter(Type baseType,
            InputFieldRef inputField,
            bool allowAbstract,
            bool allowEnum,
            bool allowGeneric)
        {
            BaseType = baseType;
            InputField = inputField;

            this.allowAbstract = allowAbstract;
            this.allowEnum = allowEnum;
            this.allowGeneric = allowGeneric;

            inputField.OnValueChanged += OnInputFieldChanged;

            CacheTypes();
        }

        public bool Enabled
        {
            get =>
                enabled;
            set
            {
                enabled = value;
                if (!enabled)
                {
                    AutoCompleteModal.Instance.ReleaseOwnership(this);
                    if (getSuggestionsCoroutine != null)
                    {
                        RuntimeHelper.StopCoroutine(getSuggestionsCoroutine);
                    }
                }
            }
        }

        public Type BaseType { get; set; }

        public InputFieldRef InputField { get; }

        public bool AnchorToCaretPosition =>
            false;

        bool ISuggestionProvider.AllowNavigation =>
            false;

        public void OnSuggestionClicked(Suggestion suggestion)
        {
            chosenSuggestion = suggestion.UnderlyingValue;
            InputField.Text = suggestion.UnderlyingValue;
            SuggestionClicked?.Invoke(suggestion);

            suggestions.Clear();
            //AutoCompleteModal.Instance.SetSuggestions(suggestions, true);
            AutoCompleteModal.Instance.ReleaseOwnership(this);
        }

        public event Action<Suggestion> SuggestionClicked;

        public void CacheTypes()
        {
            allowedTypes = null;
            cacheTypesStopwatch.Reset();
            cacheTypesStopwatch.Start();
            ReflectionUtility.GetImplementationsOf(BaseType, OnTypesCached, allowAbstract, allowGeneric, allowEnum);
        }

        private void OnTypesCached(HashSet<Type> set)
        {
            allowedTypes = set;

            // ExplorerCore.Log($"Cached {allowedTypes.Count} TypeCompleter types in {cacheTypesStopwatch.ElapsedMilliseconds * 0.001f} seconds.");

            if (pendingInput != null)
            {
                GetSuggestions(pendingInput);
                pendingInput = null;
            }
        }

        private void OnInputFieldChanged(string input)
        {
            if (!Enabled)
            {
                return;
            }

            if (input != chosenSuggestion)
            {
                chosenSuggestion = null;
            }

            if (string.IsNullOrEmpty(input) || input == chosenSuggestion)
            {
                if (getSuggestionsCoroutine != null)
                {
                    RuntimeHelper.StopCoroutine(getSuggestionsCoroutine);
                }

                AutoCompleteModal.Instance.ReleaseOwnership(this);
            }
            else
            {
                GetSuggestions(input);
            }
        }

        private void GetSuggestions(string input)
        {
            if (allowedTypes == null)
            {
                if (pendingInput != null)
                {
                    AutoCompleteModal.TakeOwnership(this);
                    AutoCompleteModal.Instance.SetSuggestions(loadingSuggestions);
                }

                pendingInput = input;

                return;
            }

            if (getSuggestionsCoroutine != null)
            {
                RuntimeHelper.StopCoroutine(getSuggestionsCoroutine);
            }

            getSuggestionsCoroutine = RuntimeHelper.StartCoroutine(GetSuggestionsAsync(input));
        }

        private IEnumerator GetSuggestionsAsync(string input)
        {
            suggestions.Clear();
            suggestedTypes.Clear();

            AutoCompleteModal.TakeOwnership(this);
            AutoCompleteModal.Instance.SetSuggestions(suggestions);

            // shorthand types all inherit from System.Object
            if (shorthandToType.TryGetValue(input, out var shorthand) && allowedTypes.Contains(shorthand))
            {
                AddSuggestion(shorthand);
            }

            foreach (var entry in shorthandToType)
            {
                if (allowedTypes.Contains(entry.Value) && entry.Key.StartsWith(input, StringComparison.InvariantCultureIgnoreCase))
                {
                    AddSuggestion(entry.Value);
                }
            }

            // Check for exact match first
            if (ReflectionUtility.GetTypeByName(input) is Type t && allowedTypes.Contains(t))
            {
                AddSuggestion(t);
            }

            if (!suggestions.Any())
            {
                AutoCompleteModal.Instance.SetSuggestions(loadingSuggestions, false);
            }
            else
            {
                AutoCompleteModal.Instance.SetSuggestions(suggestions, false);
            }

            var sw = new Stopwatch();
            sw.Start();

            // ExplorerCore.Log($"Checking {allowedTypes.Count} types...");

            foreach (var entry in allowedTypes)
            {
                if (AutoCompleteModal.CurrentHandler == null)
                {
                    yield break;
                }

                if (sw.ElapsedMilliseconds > 10)
                {
                    yield return null;
                    if (suggestions.Any())
                    {
                        AutoCompleteModal.Instance.SetSuggestions(suggestions, false);
                    }

                    sw.Reset();
                    sw.Start();
                }

                if (entry.FullName.ContainsIgnoreCase(input))
                {
                    AddSuggestion(entry);
                }
            }

            AutoCompleteModal.Instance.SetSuggestions(suggestions, false);

            // ExplorerCore.Log($"Fetched {suggestions.Count} TypeCompleter suggestions in {sw.ElapsedMilliseconds * 0.001f} seconds.");
        }

        private void AddSuggestion(Type type)
        {
            if (suggestedTypes.Contains(type.FullName))
            {
                return;
            }

            suggestedTypes.Add(type.FullName);

            if (!sharedTypeToLabel.ContainsKey(type.FullName))
            {
                sharedTypeToLabel.Add(type.FullName, SignatureHighlighter.Parse(type, true));
            }

            suggestions.Add(new Suggestion(sharedTypeToLabel[type.FullName], type.FullName));
        }
    }
}
