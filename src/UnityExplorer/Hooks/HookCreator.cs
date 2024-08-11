using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CSConsole;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Extensions;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Runtime;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.AutoComplete;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.Hooks
{
    public class HookCreator : ICellPoolDataSource<AddHookCell>
    {
        private static readonly List<MethodInfo> currentAddEligibleMethods = new List<MethodInfo>();
        private static readonly List<MethodInfo> filteredEligibleMethods = new List<MethodInfo>();
        private static readonly List<string> currentEligibleNamesForFiltering = new List<string>();

        // hook editor
        private static readonly LexerBuilder Lexer = new LexerBuilder();
        internal static HookInstance CurrentEditedHook;

        // Add Hooks UI
        internal static GameObject AddHooksRoot;
        internal static ScrollPool<AddHookCell> AddHooksScrollPool;
        internal static Text AddHooksLabel;
        internal static InputFieldRef AddHooksMethodFilterInput;
        internal static InputFieldRef ClassSelectorInputField;
        internal static Type pendingGenericDefinition;
        internal static MethodInfo pendingGenericMethod;

        public static bool PendingGeneric =>
            pendingGenericDefinition != null || pendingGenericMethod != null;

        // Hook Source Editor UI
        public static GameObject EditorRoot { get; private set; }
        public static Text EditingHookLabel { get; private set; }
        public static InputFieldScroller EditorInputScroller { get; private set; }

        public static InputFieldRef EditorInput =>
            EditorInputScroller.InputField;

        public static Text EditorInputText { get; private set; }
        public static Text EditorHighlightText { get; private set; }

        public int ItemCount =>
            filteredEligibleMethods.Count;

        // Set eligible method cell

        public void OnCellBorrowed(AddHookCell cell)
        {
        }

        public void SetCell(AddHookCell cell,
            int index)
        {
            if (index >= filteredEligibleMethods.Count)
            {
                cell.Disable();

                return;
            }

            cell.CurrentDisplayedIndex = index;
            var method = filteredEligibleMethods[index];

            cell.MethodNameLabel.text = SignatureHighlighter.ParseMethod(method);
        }

        // ~~~~~~ New hook method selector ~~~~~~~

        public void OnClassSelectedForHooks(string typeFullName)
        {
            var type = ReflectionUtility.GetTypeByName(typeFullName);
            if (type == null)
            {
                ExplorerCore.LogWarning($"Could not find any type by name {typeFullName}!");

                return;
            }

            if (type.IsGenericType)
            {
                pendingGenericDefinition = type;
                HookManagerPanel.genericArgsHandler.Show(OnGenericClassChosen, OnGenericClassCancel, type);
                HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.GenericArgsSelector);

                return;
            }

            ShowMethodsForType(type);
        }

        private void ShowMethodsForType(Type type)
        {
            SetAddHooksLabelType(SignatureHighlighter.Parse(type, true));

            AddHooksMethodFilterInput.Text = string.Empty;

            filteredEligibleMethods.Clear();
            currentAddEligibleMethods.Clear();
            currentEligibleNamesForFiltering.Clear();
            foreach (var method in type.GetMethods(ReflectionUtility.FLAGS))
            {
                if (UERuntimeHelper.IsBlacklisted(method))
                {
                    continue;
                }

                currentAddEligibleMethods.Add(method);
                currentEligibleNamesForFiltering.Add(SignatureHighlighter.RemoveHighlighting(SignatureHighlighter.ParseMethod(method)));
                filteredEligibleMethods.Add(method);
            }

            AddHooksScrollPool.Refresh(true, true);
        }

        private void OnGenericClassChosen(Type[] genericArgs)
        {
            var generic = pendingGenericDefinition.MakeGenericType(genericArgs);
            ShowMethodsForType(generic);
            HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.ClassMethodSelector);
        }

        private void OnGenericClassCancel()
        {
            pendingGenericDefinition = null;
            HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.ClassMethodSelector);
        }

        public void SetAddHooksLabelType(string typeText)
        {
            AddHooksLabel.text = $"Adding hooks to: {typeText}";

            AddHooksMethodFilterInput.GameObject.SetActive(true);
            AddHooksScrollPool.UIRoot.SetActive(true);
        }

        public static void AddHookClicked(int index)
        {
            if (index >= filteredEligibleMethods.Count)
            {
                return;
            }

            var method = filteredEligibleMethods[index];
            if (!method.IsGenericMethod && HookList.hookedSignatures.Contains(method.FullDescription()))
            {
                ExplorerCore.Log("Non-generic methods can only be hooked once.");

                return;
            }

            if (method.IsGenericMethod)
            {
                pendingGenericMethod = method;
                HookManagerPanel.genericArgsHandler.Show(OnGenericMethodChosen, OnGenericMethodCancel, method);
                HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.GenericArgsSelector);

                return;
            }

            AddHook(filteredEligibleMethods[index]);
        }

        private static void OnGenericMethodChosen(Type[] arguments)
        {
            var generic = pendingGenericMethod.MakeGenericMethod(arguments);
            AddHook(generic);
        }

        private static void OnGenericMethodCancel()
        {
            pendingGenericMethod = null;
            HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.ClassMethodSelector);
        }

        public static void AddHook(MethodInfo method)
        {
            HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.ClassMethodSelector);

            var sig = method.FullDescription();
            if (HookList.hookedSignatures.Contains(sig))
            {
                ExplorerCore.LogWarning("Method is already hooked!");

                return;
            }

            var hook = new HookInstance(method);
            HookList.hookedSignatures.Add(sig);
            HookList.currentHooks.Add(sig, hook);

            AddHooksScrollPool.Refresh(true);
            HookList.HooksScrollPool.Refresh(true);
        }

        public void OnAddHookFilterInputChanged(string input)
        {
            filteredEligibleMethods.Clear();

            if (string.IsNullOrEmpty(input))
            {
                filteredEligibleMethods.AddRange(currentAddEligibleMethods);
            }
            else
            {
                for (var i = 0; i < currentAddEligibleMethods.Count; i++)
                {
                    var eligible = currentAddEligibleMethods[i];
                    var sig = currentEligibleNamesForFiltering[i];
                    if (sig.ContainsIgnoreCase(input))
                    {
                        filteredEligibleMethods.Add(eligible);
                    }
                }
            }

            AddHooksScrollPool.Refresh(true, true);
        }

        // ~~~~~~~~ Hook source editor ~~~~~~~~

        internal static void SetEditedHook(HookInstance hook)
        {
            CurrentEditedHook = hook;
            EditingHookLabel.text = $"Editing: {SignatureHighlighter.Parse(hook.TargetMethod.DeclaringType, false, hook.TargetMethod)}";
            EditorInput.Text = hook.PatchSourceCode;
        }

        internal static void OnEditorInputChanged(string value)
        {
            EditorHighlightText.text = Lexer.BuildHighlightedString(value, 0, value.Length - 1, 0, EditorInput.Component.caretPosition, out _);
        }

        internal static void EditorInputCancel()
        {
            CurrentEditedHook = null;
            HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.ClassMethodSelector);
        }

        internal static void EditorInputSave()
        {
            var input = EditorInput.Text;
            var wasEnabled = CurrentEditedHook.Enabled;
            if (CurrentEditedHook.CompileAndGenerateProcessor(input))
            {
                if (wasEnabled)
                {
                    CurrentEditedHook.Patch();
                }

                CurrentEditedHook.PatchSourceCode = input;
                CurrentEditedHook = null;
                HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.ClassMethodSelector);
            }

            HookList.HooksScrollPool.Refresh(true);
        }

        // UI Construction

        internal void ConstructAddHooksView(GameObject rightGroup)
        {
            AddHooksRoot = UIFactory.CreateUIObject("AddHooksPanel", rightGroup);
            UIFactory.SetLayoutElement(AddHooksRoot, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(AddHooksRoot, false, false, true, true);

            var addRow = UIFactory.CreateHorizontalGroup(AddHooksRoot, "AddRow", false, true, true, true, 4, new Vector4(2, 2, 2, 2), new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(addRow, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            ClassSelectorInputField = UIFactory.CreateInputField(addRow, "ClassInput", "Enter a class to add hooks to...");
            UIFactory.SetLayoutElement(ClassSelectorInputField.Component.gameObject, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);
            var completer = new TypeCompleter(typeof(object), ClassSelectorInputField, true, false, true);
            //completer.AllTypes = true;

            var addButton = UIFactory.CreateButton(addRow, "AddButton", "View Methods");
            UIFactory.SetLayoutElement(addButton.Component.gameObject, 110, 25);
            addButton.OnClick += () =>
            {
                OnClassSelectedForHooks(ClassSelectorInputField.Text);
            };

            AddHooksLabel = UIFactory.CreateLabel(AddHooksRoot, "AddLabel", "Choose a class to begin...", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(AddHooksLabel.gameObject, minHeight: 30, minWidth: 100, flexibleWidth: 9999);

            AddHooksMethodFilterInput = UIFactory.CreateInputField(AddHooksRoot, "FilterInputField", "Filter method names...");
            UIFactory.SetLayoutElement(AddHooksMethodFilterInput.Component.gameObject, minHeight: 30, flexibleWidth: 9999);
            AddHooksMethodFilterInput.OnValueChanged += OnAddHookFilterInputChanged;

            AddHooksScrollPool = UIFactory.CreateScrollPool<AddHookCell>(AddHooksRoot, "MethodAddScrollPool", out var addScrollRoot, out var addContent);
            UIFactory.SetLayoutElement(addScrollRoot, flexibleHeight: 9999);
            AddHooksScrollPool.Initialize(this);

            AddHooksMethodFilterInput.GameObject.SetActive(false);
            AddHooksScrollPool.UIRoot.SetActive(false);
        }

        public void ConstructEditor(GameObject parent)
        {
            EditorRoot = UIFactory.CreateUIObject("HookSourceEditor", parent);
            UIFactory.SetLayoutElement(EditorRoot, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(EditorRoot, true, true, true, true, 2, 3, 3, 3, 3);

            EditingHookLabel = UIFactory.CreateLabel(EditorRoot, "EditingHookLabel", "NOT SET", TextAnchor.MiddleCenter);
            EditingHookLabel.fontStyle = FontStyle.Bold;
            UIFactory.SetLayoutElement(EditingHookLabel.gameObject, flexibleWidth: 9999, minHeight: 25);

            var editorLabel = UIFactory.CreateLabel(EditorRoot, "EditorLabel",
                "* Accepted method names are <b>Prefix</b>, <b>Postfix</b>, <b>Finalizer</b> and <b>Transpiler</b> (can define multiple).\n" + "* Your patch methods must be static.\n" +
                "* Hooks are temporary! Copy the source into your IDE to avoid losing work if you wish to keep it!");

            UIFactory.SetLayoutElement(editorLabel.gameObject, minHeight: 25, flexibleWidth: 9999);

            var editorButtonRow = UIFactory.CreateHorizontalGroup(EditorRoot, "ButtonRow", false, false, true, true, 5);
            UIFactory.SetLayoutElement(editorButtonRow, minHeight: 25, flexibleWidth: 9999);

            var editorSaveButton = UIFactory.CreateButton(editorButtonRow, "DoneButton", "Save and Return", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(editorSaveButton.Component.gameObject, minHeight: 25, flexibleWidth: 9999);
            editorSaveButton.OnClick += EditorInputSave;

            var editorDoneButton = UIFactory.CreateButton(editorButtonRow, "DoneButton", "Cancel and Return", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(editorDoneButton.Component.gameObject, minHeight: 25, flexibleWidth: 9999);
            editorDoneButton.OnClick += EditorInputCancel;

            var fontSize = 16;
            var inputObj = UIFactory.CreateScrollInputField(EditorRoot, "EditorInput", "", out var inputScroller, fontSize);
            EditorInputScroller = inputScroller;
            EditorInput.OnValueChanged += OnEditorInputChanged;

            EditorInputText = EditorInput.Component.textComponent;
            EditorInputText.supportRichText = false;
            EditorInputText.color = Color.clear;
            EditorInput.Component.customCaretColor = true;
            EditorInput.Component.caretColor = Color.white;
            EditorInput.PlaceholderText.fontSize = fontSize;

            // Lexer highlight text overlay
            var highlightTextObj = UIFactory.CreateUIObject("HighlightText", EditorInputText.gameObject);
            var highlightTextRect = highlightTextObj.GetComponent<RectTransform>();
            highlightTextRect.pivot = new Vector2(0, 1);
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = Vector2.zero;
            highlightTextRect.offsetMax = Vector2.zero;

            EditorHighlightText = highlightTextObj.AddComponent<Text>();
            EditorHighlightText.color = Color.white;
            EditorHighlightText.supportRichText = true;
            EditorHighlightText.fontSize = fontSize;

            // Set fonts
            EditorInputText.font = UniversalUI.ConsoleFont;
            EditorInput.PlaceholderText.font = UniversalUI.ConsoleFont;
            EditorHighlightText.font = UniversalUI.ConsoleFont;
        }
    }
}
