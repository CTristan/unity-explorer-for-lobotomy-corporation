using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.IValues;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.Views
{
    public class CacheKeyValuePairCell : CacheObjectCell
    {
        public static Color EvenColor = new Color(0.07f, 0.07f, 0.07f);
        public static Color OddColor = new Color(0.063f, 0.063f, 0.063f);

        public LayoutElement KeyGroupLayout;
        public InputFieldRef KeyInputField;
        public Text KeyInputTypeLabel;
        public ButtonRef KeyInspectButton;
        public Text KeyLabel;
        public Image Image { get; private set; }

        public InteractiveDictionary DictOwner =>
            Occupant.Owner as InteractiveDictionary;

        public int AdjustedWidth =>
            (int)Rect.rect.width - 70;

        //public int HalfWidth => (int)(0.5f * Rect.rect.width) - 75;
        //public int AdjustedKeyWidth => HalfWidth - 50;
        //public int AdjustedRightWidth => HalfWidth;

        private void KeyInspectClicked()
        {
            InspectorManager.Inspect((Occupant as CacheKeyValuePair).DictKey, Occupant);
        }

        public override GameObject CreateContent(GameObject parent)
        {
            var root = base.CreateContent(parent);

            Image = root.AddComponent<Image>();

            NameLayout.minWidth = 70;
            NameLayout.flexibleWidth = 0;
            NameLayout.minHeight = 30;
            NameLayout.flexibleHeight = 0;
            NameLabel.alignment = TextAnchor.MiddleRight;

            RightGroupLayout.minWidth = AdjustedWidth * 0.55f;

            // Key area
            var keyGroup = UIFactory.CreateUIObject("KeyHolder", root.transform.Find("HoriGroup").gameObject);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(keyGroup, false, false, true, true, 2, 0, 0, 4, 4, TextAnchor.MiddleLeft);
            KeyGroupLayout = UIFactory.SetLayoutElement(keyGroup, minHeight: 30, minWidth: (int)(AdjustedWidth * 0.44f), flexibleWidth: 0);

            // set to be after the NameLabel (our index label), and before the main horizontal group.
            keyGroup.transform.SetSiblingIndex(1);

            // key Inspect

            KeyInspectButton = UIFactory.CreateButton(keyGroup, "KeyInspectButton", "Inspect", new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(KeyInspectButton.Component.gameObject, 60, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);
            KeyInspectButton.OnClick += KeyInspectClicked;

            // label

            KeyLabel = UIFactory.CreateLabel(keyGroup, "KeyLabel", "<i>empty</i>");
            UIFactory.SetLayoutElement(KeyLabel.gameObject, 50, flexibleWidth: 999, minHeight: 25);

            // Type label for input field

            KeyInputTypeLabel = UIFactory.CreateLabel(keyGroup, "InputTypeLabel", "<i>null</i>");
            UIFactory.SetLayoutElement(KeyInputTypeLabel.gameObject, 55, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            // input field

            KeyInputField = UIFactory.CreateInputField(keyGroup, "KeyInput", "empty");
            UIFactory.SetLayoutElement(KeyInputField.UIRoot, minHeight: 25, flexibleHeight: 0, flexibleWidth: 0, preferredWidth: 200);
            //KeyInputField.lineType = InputField.LineType.MultiLineNewline;
            KeyInputField.Component.readOnly = true;

            return root;
        }

        protected override void ConstructEvaluateHolder(GameObject parent)
        {
            // not used
        }
    }
}
