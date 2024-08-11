using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels;
using UnityExplorerForLobotomyCorporation.UniverseLib;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.Views
{
    public abstract class CacheObjectCell : ICell
    {
        public readonly Color subActiveColor = new Color(0.23f, 0.33f, 0.23f);

        public readonly Color subInactiveColor = new Color(0.23f, 0.23f, 0.23f);
        public ButtonRef ApplyButton;

        public ButtonRef CopyButton;
        public InputFieldRef HiddenNameLabel; // for selecting the name label
        public InputFieldRef InputField;

        public ButtonRef InspectButton;

        public Text NameLabel;

        public LayoutElement NameLayout;
        public ButtonRef PasteButton;
        public GameObject RightGroupContent;
        public LayoutElement RightGroupLayout;
        public ButtonRef SubContentButton;
        public GameObject SubContentHolder;
        public Toggle Toggle;
        public Text ToggleText;
        public Text TypeLabel;
        public Text ValueLabel;

        public CacheObjectBase Occupant { get; set; }

        public bool SubContentActive =>
            SubContentHolder.activeSelf;

        public virtual GameObject CreateContent(GameObject parent)
        {
            // Main layout

            UIRoot = UIFactory.CreateUIObject(GetType().Name, parent, new Vector2(100, 30));
            Rect = UIRoot.GetComponent<RectTransform>();
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(UIRoot, false, false, true, true, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(UIRoot, 100, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 600);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var horiRow = UIFactory.CreateUIObject("HoriGroup", UIRoot);
            UIFactory.SetLayoutElement(horiRow, minHeight: 29, flexibleHeight: 150, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(horiRow, false, false, true, true, 5, 2, childAlignment: TextAnchor.UpperLeft);
            horiRow.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Left name label

            NameLabel = UIFactory.CreateLabel(horiRow, "NameLabel", "<notset>");
            NameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            NameLayout = UIFactory.SetLayoutElement(NameLabel.gameObject, minHeight: 25, minWidth: 20, flexibleHeight: 300, flexibleWidth: 0);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(NameLabel.gameObject, true, true, true, true);

            HiddenNameLabel = UIFactory.CreateInputField(NameLabel.gameObject, "HiddenNameLabel", "");
            var hiddenRect = HiddenNameLabel.Component.GetComponent<RectTransform>();
            hiddenRect.anchorMin = Vector2.zero;
            hiddenRect.anchorMax = Vector2.one;
            HiddenNameLabel.Component.readOnly = true;
            HiddenNameLabel.Component.lineType = UnityEngine.UI.InputField.LineType.MultiLineNewline;
            HiddenNameLabel.Component.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            HiddenNameLabel.Component.gameObject.GetComponent<Image>().color = Color.clear;
            HiddenNameLabel.Component.textComponent.color = Color.clear;
            UIFactory.SetLayoutElement(HiddenNameLabel.Component.gameObject, minHeight: 25, minWidth: 20, flexibleHeight: 300, flexibleWidth: 0);

            // Right vertical group

            RightGroupContent = UIFactory.CreateUIObject("RightGroup", horiRow);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(RightGroupContent, false, false, true, true, 4, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(RightGroupContent, minHeight: 25, minWidth: 200, flexibleWidth: 9999, flexibleHeight: 800);
            RightGroupLayout = RightGroupContent.GetComponent<LayoutElement>();

            ConstructEvaluateHolder(RightGroupContent);

            // Right horizontal group

            var rightHoriGroup = UIFactory.CreateUIObject("RightHoriGroup", RightGroupContent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(rightHoriGroup, false, false, true, true, 4, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(rightHoriGroup, minHeight: 25, minWidth: 200, flexibleWidth: 9999, flexibleHeight: 800);

            SubContentButton = UIFactory.CreateButton(rightHoriGroup, "SubContentButton", "▲", subInactiveColor);
            UIFactory.SetLayoutElement(SubContentButton.Component.gameObject, 25, 25, 0, 0);
            SubContentButton.OnClick += SubContentClicked;

            // Type label

            TypeLabel = UIFactory.CreateLabel(rightHoriGroup, "ReturnLabel", "<notset>");
            TypeLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(TypeLabel.gameObject, minHeight: 25, flexibleHeight: 150, minWidth: 45, flexibleWidth: 0);

            // Bool and number value interaction

            var toggleObj = UIFactory.CreateToggle(rightHoriGroup, "Toggle", out Toggle, out ToggleText);
            UIFactory.SetLayoutElement(toggleObj, 70, 25, 0, 0);
            ToggleText.color = SignatureHighlighter.KeywordBlue;
            Toggle.onValueChanged.AddListener(ToggleClicked);

            InputField = UIFactory.CreateInputField(rightHoriGroup, "InputField", "...");
            UIFactory.SetLayoutElement(InputField.UIRoot, 150, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            // Apply

            ApplyButton = UIFactory.CreateButton(rightHoriGroup, "ApplyButton", "Apply", new Color(0.15f, 0.19f, 0.15f));
            UIFactory.SetLayoutElement(ApplyButton.Component.gameObject, 70, 25, 0, 0);
            ApplyButton.OnClick += ApplyClicked;

            // Inspect

            InspectButton = UIFactory.CreateButton(rightHoriGroup, "InspectButton", "Inspect", new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(InspectButton.Component.gameObject, 70, flexibleWidth: 0, minHeight: 25);
            InspectButton.OnClick += InspectClicked;

            // Main value label

            ValueLabel = UIFactory.CreateLabel(rightHoriGroup, "ValueLabel", "Value goes here");
            ValueLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(ValueLabel.gameObject, minHeight: 25, flexibleHeight: 150, flexibleWidth: 9999);

            // Copy and Paste buttons

            var buttonHolder = UIFactory.CreateHorizontalGroup(rightHoriGroup, "CopyPasteButtons", false, false, true, true, 4, bgColor: new Color(1, 1, 1, 0), childAlignment: TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(buttonHolder, 60, flexibleWidth: 0);

            CopyButton = UIFactory.CreateButton(buttonHolder, "CopyButton", "Copy", new Color(0.13f, 0.13f, 0.13f, 1f));
            UIFactory.SetLayoutElement(CopyButton.Component.gameObject, minHeight: 25, minWidth: 28, flexibleWidth: 0);
            CopyButton.ButtonText.color = Color.yellow;
            CopyButton.ButtonText.fontSize = 10;
            CopyButton.OnClick += OnCopyClicked;

            PasteButton = UIFactory.CreateButton(buttonHolder, "PasteButton", "Paste", new Color(0.13f, 0.13f, 0.13f, 1f));
            UIFactory.SetLayoutElement(PasteButton.Component.gameObject, minHeight: 25, minWidth: 28, flexibleWidth: 0);
            PasteButton.ButtonText.color = Color.green;
            PasteButton.ButtonText.fontSize = 10;
            PasteButton.OnClick += OnPasteClicked;

            // Subcontent

            SubContentHolder = UIFactory.CreateUIObject("SubContent", UIRoot);
            UIFactory.SetLayoutElement(SubContentHolder.gameObject, minHeight: 30, flexibleHeight: 600, minWidth: 100, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(SubContentHolder, true, true, true, true, 2, childAlignment: TextAnchor.UpperLeft);
            //SubContentHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.MinSize;
            SubContentHolder.SetActive(false);

            // Bottom separator
            var separator = UIFactory.CreateUIObject("BottomSeperator", UIRoot);
            UIFactory.SetLayoutElement(separator, minHeight: 1, flexibleHeight: 0, flexibleWidth: 9999);
            separator.AddComponent<Image>().color = Color.black;

            return UIRoot;
        }

        protected virtual void ApplyClicked()
        {
            Occupant.OnCellApplyClicked();
        }

        protected virtual void InspectClicked()
        {
            InspectorManager.Inspect(Occupant.Value, Occupant);
        }

        protected virtual void ToggleClicked(bool value)
        {
            ToggleText.text = value.ToString();
        }

        protected virtual void SubContentClicked()
        {
            Occupant.OnCellSubContentToggle();
        }

        protected virtual void OnCopyClicked()
        {
            ClipboardPanel.Copy(Occupant.Value);
        }

        protected virtual void OnPasteClicked()
        {
            if (ClipboardPanel.TryPaste(Occupant.FallbackType, out var paste))
            {
                Occupant.SetUserValue(paste);
            }
        }

        public void RefreshSubcontentButton()
        {
            SubContentButton.ButtonText.text = SubContentHolder.activeSelf ? "▼" : "▲";
            var color = SubContentHolder.activeSelf ? subActiveColor : subInactiveColor;
            RuntimeHelper.SetColorBlock(SubContentButton.Component, color, color * 1.3f);
        }

        protected abstract void ConstructEvaluateHolder(GameObject parent);

        #region ICell

        public float DefaultHeight =>
            30f;

        public GameObject UIRoot { get; set; }

        public bool Enabled { get; private set; }

        public RectTransform Rect { get; set; }

        public void Disable()
        {
            Enabled = false;
            UIRoot.SetActive(false);
        }

        public void Enable()
        {
            Enabled = true;
            UIRoot.SetActive(true);
        }

        #endregion
    }
}
