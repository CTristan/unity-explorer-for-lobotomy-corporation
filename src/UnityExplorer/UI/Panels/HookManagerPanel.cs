using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Hooks;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.EvaluateWidget;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels
{
    public class HookManagerPanel : UEPanel
    {
        public enum Pages
        {
            ClassMethodSelector,
            HookSourceEditor,
            GenericArgsSelector,
        }

        public static HookCreator hookCreator;
        public static HookList hookList;
        public static GenericConstructorWidget genericArgsHandler;

        public HookManagerPanel(UIBase owner) : base(owner)
        {
        }

        public static HookManagerPanel Instance { get; private set; }

        // Panel
        public override UIManager.Panels PanelType =>
            UIManager.Panels.HookManager;

        public override string Name =>
            "Hooks";

        public override bool ShowByDefault =>
            false;

        public override int MinWidth =>
            400;

        public override int MinHeight =>
            400;

        public override Vector2 DefaultAnchorMin =>
            new Vector2(0.5f, 0.5f);

        public override Vector2 DefaultAnchorMax =>
            new Vector2(0.5f, 0.5f);

        public Pages CurrentPage { get; private set; } = Pages.ClassMethodSelector;

        public void SetPage(Pages page)
        {
            switch (page)
            {
                case Pages.ClassMethodSelector:
                    HookCreator.AddHooksRoot.SetActive(true);
                    HookCreator.EditorRoot.SetActive(false);
                    genericArgsHandler.UIRoot.SetActive(false);

                    break;

                case Pages.HookSourceEditor:
                    HookCreator.AddHooksRoot.SetActive(false);
                    HookCreator.EditorRoot.SetActive(true);
                    genericArgsHandler.UIRoot.SetActive(false);

                    break;

                case Pages.GenericArgsSelector:
                    HookCreator.AddHooksRoot.SetActive(false);
                    HookCreator.EditorRoot.SetActive(false);
                    genericArgsHandler.UIRoot.SetActive(true);

                    break;
            }
        }

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();

            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);
        }

        protected override void ConstructPanelContent()
        {
            Instance = this;
            hookList = new HookList();
            hookCreator = new HookCreator();
            genericArgsHandler = new GenericConstructorWidget();

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ContentRoot, true, false);

            // GameObject baseHoriGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "HoriGroup", true, true, true, true);
            // UIFactory.SetLayoutElement(baseHoriGroup, flexibleWidth: 9999, flexibleHeight: 9999);

            // // Left Group

            //GameObject leftGroup = UIFactory.CreateVerticalGroup(ContentRoot, "LeftGroup", true, true, true, true);
            UIFactory.SetLayoutElement(ContentRoot.gameObject, 300, flexibleWidth: 9999, flexibleHeight: 9999);

            hookList.ConstructUI(ContentRoot);

            // // Right Group

            //GameObject rightGroup = UIFactory.CreateVerticalGroup(ContentRoot, "RightGroup", true, true, true, true);
            UIFactory.SetLayoutElement(ContentRoot, 300, flexibleWidth: 9999, flexibleHeight: 9999);

            hookCreator.ConstructAddHooksView(ContentRoot);

            hookCreator.ConstructEditor(ContentRoot);
            HookCreator.EditorRoot.SetActive(false);

            genericArgsHandler.ConstructUI(ContentRoot);
            genericArgsHandler.UIRoot.SetActive(false);
        }
    }
}
