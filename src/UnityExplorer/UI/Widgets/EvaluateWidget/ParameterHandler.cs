using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Extensions;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.AutoComplete;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.EvaluateWidget
{
    public class ParameterHandler : BaseArgumentHandler
    {
        private Text basicLabel;
        private GameObject basicLabelHolder;
        private object basicValue;

        internal EnumCompleter enumCompleter;
        private ButtonRef enumHelperButton;
        private ParameterInfo paramInfo;
        private Type paramType;
        private ButtonRef pasteButton;

        private bool usingBasicLabel;

        public void OnBorrowed(ParameterInfo paramInfo)
    {
        this.paramInfo = paramInfo;

        paramType = paramInfo.ParameterType;
        if (paramType.IsByRef)
        {
            paramType = paramType.GetElementType();
        }

        argNameLabel.text = $"{SignatureHighlighter.Parse(paramType, false)} <color={SignatureHighlighter.LOCAL_ARG}>{paramInfo.Name}</color>";

        if (ParseUtility.CanParse(paramType) || typeof(Type).IsAssignableFrom(paramType))
        {
            usingBasicLabel = false;

            inputField.Component.gameObject.SetActive(true);
            basicLabelHolder.SetActive(false);
            typeCompleter.Enabled = typeof(Type).IsAssignableFrom(paramType);
            enumCompleter.Enabled = paramType.IsEnum;
            enumHelperButton.Component.gameObject.SetActive(paramType.IsEnum);

            if (!typeCompleter.Enabled)
            {
                if (paramType == typeof(string))
                {
                    inputField.PlaceholderText.text = "...";
                }
                else
                {
                    inputField.PlaceholderText.text = $"eg. {ParseUtility.GetExampleInput(paramType)}";
                }
            }
            else
            {
                inputField.PlaceholderText.text = "Enter a Type name...";
                typeCompleter.BaseType = typeof(object);
                typeCompleter.CacheTypes();
            }

            if (enumCompleter.Enabled)
            {
                enumCompleter.EnumType = paramType;
                enumCompleter.CacheEnumValues();
            }
        }
        else
        {
            // non-parsable, and not a Type
            usingBasicLabel = true;

            inputField.Component.gameObject.SetActive(false);
            basicLabelHolder.SetActive(true);
            typeCompleter.Enabled = false;
            enumCompleter.Enabled = false;
            enumHelperButton.Component.gameObject.SetActive(false);

            SetDisplayedValueFromPaste();
        }
    }

        public void OnReturned()
    {
        paramInfo = null;

        enumCompleter.Enabled = false;
        typeCompleter.Enabled = false;

        inputField.Text = "";

        usingBasicLabel = false;
        basicValue = null;
    }

        public object Evaluate()
    {
        if (usingBasicLabel)
        {
            return basicValue;
        }

        var input = inputField.Text;

        if (typeof(Type).IsAssignableFrom(paramType))
        {
            return ReflectionUtility.GetTypeByName(input);
        }

        if (paramType == typeof(string))
        {
            return input;
        }

        if (string.IsNullOrEmpty(input))
        {
            if (paramInfo.IsOptional)
            {
                return paramInfo.DefaultValue;
            }

            return null;
        }

        if (!ParseUtility.TryParse(input, paramType, out var parsed, out var ex))
        {
            ExplorerCore.LogWarning($"Cannot parse argument '{paramInfo.Name}' ({paramInfo.ParameterType.Name})" + $"{(ex == null ? "" : $", {ex.GetType().Name}: {ex.Message}")}");

            return null;
        }

        return parsed;
    }

        private void OnPasteClicked()
    {
        if (ClipboardPanel.TryPaste(paramType, out var paste))
        {
            basicValue = paste;
            SetDisplayedValueFromPaste();
        }
    }

        private void SetDisplayedValueFromPaste()
    {
        if (usingBasicLabel)
        {
            basicLabel.text = ToStringUtility.ToStringWithType(basicValue, paramType, false);
        }
        else
        {
            if (typeof(Type).IsAssignableFrom(paramType))
            {
                inputField.Text = (basicValue as Type).FullDescription();
            }
            else
            {
                inputField.Text = ParseUtility.ToStringForInput(basicValue, paramType);
            }
        }
    }

        public override void CreateSpecialContent()
    {
        enumCompleter = new EnumCompleter(paramType, inputField)
        {
            Enabled = false,
        };

        enumHelperButton = UIFactory.CreateButton(UIRoot, "EnumHelper", "▼");
        UIFactory.SetLayoutElement(enumHelperButton.Component.gameObject, 25, 25, 0, 0);
        enumHelperButton.OnClick += enumCompleter.HelperButtonClicked;

        basicLabelHolder = UIFactory.CreateHorizontalGroup(UIRoot, "BasicLabelHolder", true, true, true, true, bgColor: new Color(0.1f, 0.1f, 0.1f));
        UIFactory.SetLayoutElement(basicLabelHolder, minHeight: 25, flexibleHeight: 50, minWidth: 100, flexibleWidth: 1000);
        basicLabel = UIFactory.CreateLabel(basicLabelHolder, "BasicLabel", "null");
        basicLabel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        pasteButton = UIFactory.CreateButton(UIRoot, "PasteButton", "Paste", new Color(0.13f, 0.13f, 0.13f, 1f));
        UIFactory.SetLayoutElement(pasteButton.Component.gameObject, minHeight: 25, minWidth: 28, flexibleWidth: 0);
        pasteButton.ButtonText.color = Color.green;
        pasteButton.ButtonText.fontSize = 10;
        pasteButton.OnClick += OnPasteClicked;
    }
    }
}
