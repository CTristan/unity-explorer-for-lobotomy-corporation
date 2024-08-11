using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.ObjectPool;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors
{
    public class InspectorTab : IPooledObject
    {
        private static readonly Color enabledTabColor = new Color(0.15f, 0.22f, 0.15f);
        private static readonly Color disabledTabColor = new Color(0.13f, 0.13f, 0.13f);
        public ButtonRef CloseButton;

        public ButtonRef TabButton;
        public Text TabText;
        public GameObject UIRoot { get; set; }

        public float DefaultHeight =>
            25f;

        public GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateHorizontalGroup(parent, "TabObject", false, true, true, true, 1, default, new Color(0.13f, 0.13f, 0.13f), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(UIRoot, 200, flexibleWidth: 0);
            UIRoot.AddComponent<Mask>();
            UIRoot.AddComponent<Outline>();

            TabButton = UIFactory.CreateButton(UIRoot, "TabButton", "");
            UIFactory.SetLayoutElement(TabButton.Component.gameObject, 173, flexibleWidth: 0);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(TabButton.Component.gameObject, false, false, true, true, 0, 0, 0, 3);
            TabButton.GameObject.AddComponent<Mask>();

            TabText = TabButton.ButtonText;
            UIFactory.SetLayoutElement(TabText.gameObject, minHeight: 25, minWidth: 150, flexibleWidth: 0);
            TabText.alignment = TextAnchor.MiddleLeft;
            TabText.fontSize = 12;
            TabText.horizontalOverflow = HorizontalWrapMode.Overflow;

            CloseButton = UIFactory.CreateButton(UIRoot, "CloseButton", "X", new Color(0.15f, 0.15f, 0.15f, 1));
            UIFactory.SetLayoutElement(CloseButton.Component.gameObject, minHeight: 25, minWidth: 25, flexibleWidth: 0);
            CloseButton.ButtonText.color = Color.red;

            return UIRoot;
        }

        public void SetTabColor(bool active)
        {
            var color = active ? enabledTabColor : disabledTabColor;
            RuntimeHelper.SetColorBlock(TabButton.Component, color, color * 1.2f);
        }
    }
}
