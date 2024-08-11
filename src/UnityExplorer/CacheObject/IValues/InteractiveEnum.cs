using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.AutoComplete;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.IValues
{
    public class InteractiveEnum : InteractiveValue
    {
        private readonly List<Text> flagTexts = new List<Text>();
        private readonly List<Toggle> flagToggles = new List<Toggle>();

        public OrderedDictionary CurrentValues;
        private EnumCompleter enumCompleter;
        private ButtonRef enumHelperButton;
        public Type EnumType;

        private InputFieldRef inputField;
        public bool IsFlags;

        private Type lastType;

        private GameObject toggleHolder;

        public CachedEnumValue ValueAtIndex(int idx)
        {
            return (CachedEnumValue)CurrentValues[idx];
        }

        public CachedEnumValue ValueAtKey(object key)
        {
            return (CachedEnumValue)CurrentValues[key];
        }

        // Setting value from owner
        public override void SetValue(object value)
        {
            EnumType = value.GetType();

            if (lastType != EnumType)
            {
                CurrentValues = GetEnumValues(EnumType);

                IsFlags = EnumType.GetCustomAttributes(typeof(FlagsAttribute), true) is object[] fa && fa.Any();
                if (IsFlags)
                {
                    SetupTogglesForEnumType();
                }
                else
                {
                    inputField.Component.gameObject.SetActive(true);
                    enumHelperButton.Component.gameObject.SetActive(true);
                    toggleHolder.SetActive(false);
                }

                enumCompleter.EnumType = EnumType;
                enumCompleter.CacheEnumValues();

                lastType = EnumType;
            }

            if (!IsFlags)
            {
                inputField.Text = value.ToString();
            }
            else
            {
                SetTogglesForValue(value);
            }

            enumCompleter.chosenSuggestion = value.ToString();
            AutoCompleteModal.Instance.ReleaseOwnership(enumCompleter);
        }

        private void SetTogglesForValue(object value)
        {
            try
            {
                for (var i = 0; i < CurrentValues.Count; i++) flagToggles[i].isOn = (value as Enum).HasFlag(ValueAtIndex(i).ActualValue as Enum);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception setting flag toggles: " + ex);
            }
        }

        // Setting value to owner

        private void OnApplyClicked()
        {
            try
            {
                if (!IsFlags)
                {
                    if (ParseUtility.TryParse(inputField.Text, EnumType, out var value, out var ex))
                    {
                        CurrentOwner.SetUserValue(value);
                    }
                    else
                    {
                        throw ex;
                    }
                }
                else
                {
                    SetValueFromFlags();
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception setting from dropdown: " + ex);
            }
        }

        private void SetValueFromFlags()
        {
            try
            {
                var values = new List<string>();
                for (var i = 0; i < CurrentValues.Count; i++)
                    if (flagToggles[i].isOn)
                    {
                        values.Add(ValueAtIndex(i).Name);
                    }

                CurrentOwner.SetUserValue(Enum.Parse(EnumType, string.Join(", ", values.ToArray())));
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception setting from flag toggles: " + ex);
            }
        }

        // UI Construction

        private void EnumHelper_OnClick()
        {
            enumCompleter.HelperButtonClicked();
        }

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "InteractiveEnum", false, false, true, true, 3, new Vector4(4, 4, 4, 4), new Color(0.06f, 0.06f, 0.06f));
            UIFactory.SetLayoutElement(UIRoot, minHeight: 25, flexibleHeight: 9999, flexibleWidth: 9999);

            var hori = UIFactory.CreateUIObject("Hori", UIRoot);
            UIFactory.SetLayoutElement(hori, minHeight: 25, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(hori, false, false, true, true, 2);

            var applyButton = UIFactory.CreateButton(hori, "ApplyButton", "Apply", new Color(0.2f, 0.27f, 0.2f));
            UIFactory.SetLayoutElement(applyButton.Component.gameObject, minHeight: 25, minWidth: 100);
            applyButton.OnClick += OnApplyClicked;

            inputField = UIFactory.CreateInputField(hori, "InputField", "Enter name or underlying value...");
            UIFactory.SetLayoutElement(inputField.UIRoot, minHeight: 25, flexibleHeight: 50, minWidth: 100, flexibleWidth: 1000);
            inputField.Component.lineType = InputField.LineType.MultiLineNewline;
            inputField.UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            enumHelperButton = UIFactory.CreateButton(hori, "EnumHelper", "▼");
            UIFactory.SetLayoutElement(enumHelperButton.Component.gameObject, 25, 25, 0, 0);
            enumHelperButton.OnClick += EnumHelper_OnClick;

            enumCompleter = new EnumCompleter(EnumType, inputField);

            toggleHolder = UIFactory.CreateUIObject("ToggleHolder", UIRoot);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(toggleHolder, false, false, true, true, 4);
            UIFactory.SetLayoutElement(toggleHolder, minHeight: 25, flexibleWidth: 9999, flexibleHeight: 9999);

            return UIRoot;
        }

        private void SetupTogglesForEnumType()
        {
            toggleHolder.SetActive(true);
            inputField.Component.gameObject.SetActive(false);
            enumHelperButton.Component.gameObject.SetActive(false);

            // create / set / hide toggles
            for (var i = 0; i < CurrentValues.Count || i < flagToggles.Count; i++)
            {
                if (i >= CurrentValues.Count)
                {
                    if (i >= flagToggles.Count)
                    {
                        break;
                    }

                    flagToggles[i].gameObject.SetActive(false);

                    continue;
                }

                if (i >= flagToggles.Count)
                {
                    AddToggleRow();
                }

                flagToggles[i].isOn = false;
                flagTexts[i].text = ValueAtIndex(i).Name;
            }
        }

        private void AddToggleRow()
        {
            var row = UIFactory.CreateUIObject("ToggleRow", toggleHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(row, false, false, true, true, 2);
            UIFactory.SetLayoutElement(row, minHeight: 25, flexibleWidth: 9999);

            var toggleObj = UIFactory.CreateToggle(row, "ToggleObj", out var toggle, out var toggleText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, flexibleWidth: 9999);

            flagToggles.Add(toggle);
            flagTexts.Add(toggleText);
        }

        #region Enum cache

        internal static readonly Dictionary<string, OrderedDictionary> enumCache = new Dictionary<string, OrderedDictionary>();

        internal static OrderedDictionary GetEnumValues(Type enumType)
        {
            //isFlags = enumType.GetCustomAttributes(typeof(FlagsAttribute), true) is object[] fa && fa.Any();

            if (!enumCache.ContainsKey(enumType.AssemblyQualifiedName))
            {
                var dict = new OrderedDictionary();
                var addedNames = new HashSet<string>();

                var i = 0;
                foreach (var value in Enum.GetValues(enumType))
                {
                    var name = value.ToString();
                    if (addedNames.Contains(name))
                    {
                        continue;
                    }

                    addedNames.Add(name);

                    dict.Add(value, new CachedEnumValue(value, i, name));
                    i++;
                }

                enumCache.Add(enumType.AssemblyQualifiedName, dict);
            }

            return enumCache[enumType.AssemblyQualifiedName];
        }

        #endregion
    }

    public struct CachedEnumValue
    {
        public CachedEnumValue(object value,
            int index,
            string name)
        {
            EnumIndex = index;
            Name = name;
            ActualValue = value;
        }

        public readonly object ActualValue;
        public int EnumIndex;
        public readonly string Name;
    }
}
