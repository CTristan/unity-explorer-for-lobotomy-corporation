using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UnityExplorer.ObjectExplorer;
using UnityExplorerForLobotomyCorporation.UniverseLib;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels
{
    public class ObjectExplorerPanel : UEPanel
    {
        private readonly List<ButtonRef> tabButtons = new List<ButtonRef>();
        private readonly List<UIModel> tabPages = new List<UIModel>();
        public ObjectSearch ObjectSearch;

        public SceneExplorer SceneExplorer;

        public int SelectedTab;

        public ObjectExplorerPanel(UIBase owner) : base(owner)
        {
        }

        public override string Name =>
            "Object Explorer";

        public override UIManager.Panels PanelType =>
            UIManager.Panels.ObjectExplorer;

        public override int MinWidth =>
            350;

        public override int MinHeight =>
            200;

        public override Vector2 DefaultAnchorMin =>
            new Vector2(0.125f, 0.175f);

        public override Vector2 DefaultAnchorMax =>
            new Vector2(0.325f, 0.925f);

        public override bool ShowByDefault =>
            true;

        public override bool ShouldSaveActiveState =>
            true;

        public void SetTab(int tabIndex)
        {
            if (SelectedTab != -1)
            {
                DisableTab(SelectedTab);
            }

            var content = tabPages[tabIndex];
            content.SetActive(true);

            var button = tabButtons[tabIndex];
            RuntimeHelper.SetColorBlock(button.Component, UniversalUI.EnabledButtonColor, UniversalUI.EnabledButtonColor * 1.2f);

            SelectedTab = tabIndex;
            SaveInternalData();
        }

        private void DisableTab(int tabIndex)
        {
            tabPages[tabIndex].SetActive(false);
            RuntimeHelper.SetColorBlock(tabButtons[tabIndex].Component, UniversalUI.DisabledButtonColor, UniversalUI.DisabledButtonColor * 1.2f);
        }

        public override void Update()
        {
            if (SelectedTab == 0)
            {
                SceneExplorer.Update();
            }
            else
            {
                ObjectSearch.Update();
            }
        }

        public override string ToSaveData()
        {
            return string.Join("|", new[]
            {
                base.ToSaveData(), SelectedTab.ToString(),
            });
        }

        protected override void ApplySaveData(string data)
        {
            base.ApplySaveData(data);

            try
            {
                var tab = int.Parse(data.Split('|').Last());
                SelectedTab = tab;
            }
            catch
            {
                SelectedTab = 0;
            }

            SelectedTab = Math.Max(0, SelectedTab);
            SelectedTab = Math.Min(1, SelectedTab);

            SetTab(SelectedTab);
        }

        protected override void ConstructPanelContent()
        {
            // Tab bar
            var tabGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "TabBar", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(tabGroup, minHeight: 25, flexibleHeight: 0);

            // Scene Explorer
            SceneExplorer = new SceneExplorer(this);
            SceneExplorer.ConstructUI(ContentRoot);
            tabPages.Add(SceneExplorer);

            // Object search
            ObjectSearch = new ObjectSearch(this);
            ObjectSearch.ConstructUI(ContentRoot);
            tabPages.Add(ObjectSearch);

            // set up tabs
            AddTabButton(tabGroup, "Scene Explorer");
            AddTabButton(tabGroup, "Object Search");

            // default active state: Active
            SetActive(true);
        }

        private void AddTabButton(GameObject tabGroup,
            string label)
        {
            var button = UIFactory.CreateButton(tabGroup, $"Button_{label}", label);

            var idx = tabButtons.Count;
            //button.onClick.AddListener(() => { SetTab(idx); });
            button.OnClick += () =>
            {
                SetTab(idx);
            };

            tabButtons.Add(button);

            DisableTab(tabButtons.Count - 1);
        }
    }
}
