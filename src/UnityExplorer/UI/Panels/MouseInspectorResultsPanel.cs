using System.Collections.Generic;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors.MouseInspectors;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ButtonList;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels
{
    public class MouseInspectorResultsPanel : UEPanel
    {
        private ScrollPool<ButtonCell> buttonScrollPool;

        private ButtonListHandler<GameObject, ButtonCell> dataHandler;

        public MouseInspectorResultsPanel(UIBase owner) : base(owner)
        {
        }

        public override UIManager.Panels PanelType =>
            UIManager.Panels.UIInspectorResults;

        public override string Name =>
            "UI Inspector Results";

        public override int MinWidth =>
            500;

        public override int MinHeight =>
            500;

        public override Vector2 DefaultAnchorMin =>
            new Vector2(0.5f, 0.5f);

        public override Vector2 DefaultAnchorMax =>
            new Vector2(0.5f, 0.5f);

        public override bool CanDragAndResize =>
            true;

        public override bool NavButtonWanted =>
            false;

        public override bool ShouldSaveActiveState =>
            false;

        public override bool ShowByDefault =>
            false;

        public void ShowResults()
        {
            dataHandler.RefreshData();
            buttonScrollPool.Refresh(true, true);
        }

        private List<GameObject> GetEntries()
        {
            return UiInspector.LastHitObjects;
        }

        private bool ShouldDisplayCell(object cell,
            string filter)
        {
            return true;
        }

        private void OnCellClicked(int index)
        {
            if (index >= UiInspector.LastHitObjects.Count)
            {
                return;
            }

            InspectorManager.Inspect(UiInspector.LastHitObjects[index]);
        }

        private void SetCell(ButtonCell cell,
            int index)
        {
            if (index >= UiInspector.LastHitObjects.Count)
            {
                return;
            }

            var obj = UiInspector.LastHitObjects[index];
            cell.Button.ButtonText.text = $"<color=cyan>{obj.name}</color> ({obj.transform.GetTransformPath(true)})";
        }

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();

            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 500f);
        }

        protected override void ConstructPanelContent()
        {
            dataHandler = new ButtonListHandler<GameObject, ButtonCell>(buttonScrollPool, GetEntries, SetCell, ShouldDisplayCell, OnCellClicked);

            buttonScrollPool = UIFactory.CreateScrollPool<ButtonCell>(ContentRoot, "ResultsList", out var scrollObj, out var scrollContent);

            buttonScrollPool.Initialize(dataHandler);
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
        }
    }
}
