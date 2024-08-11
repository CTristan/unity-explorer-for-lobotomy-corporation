using System;
using System.Collections.Generic;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.Views;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Config;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels
{
    public class OptionsPanel : UEPanel, ICacheObjectController, ICellPoolDataSource<ConfigEntryCell>
    {
        // Entry holders
        private readonly List<CacheConfigEntry> configEntries = new List<CacheConfigEntry>();

        public OptionsPanel(UIBase owner) : base(owner)
        {
            foreach (var entry in ConfigManager.ConfigElements)
            {
                var cache = new CacheConfigEntry(entry.Value)
                {
                    Owner = this,
                };

                configEntries.Add(cache);
            }

            foreach (var config in configEntries)
            {
                config.UpdateValueFromSource();
            }
        }

        public override string Name =>
            "Options";

        public override UIManager.Panels PanelType =>
            UIManager.Panels.Options;

        public override int MinWidth =>
            600;

        public override int MinHeight =>
            200;

        public override Vector2 DefaultAnchorMin =>
            new Vector2(0.5f, 0.1f);

        public override Vector2 DefaultAnchorMax =>
            new Vector2(0.5f, 0.85f);

        public override bool ShouldSaveActiveState =>
            false;

        public override bool ShowByDefault =>
            false;

        // ICacheObjectController
        public CacheObjectBase ParentCacheObject =>
            null;

        public object Target =>
            null;

        public Type TargetType =>
            null;

        public bool CanWrite =>
            true;

        // ICellPoolDataSource
        public int ItemCount =>
            configEntries.Count;

        public void OnCellBorrowed(ConfigEntryCell cell)
        {
        }

        public void SetCell(ConfigEntryCell cell,
            int index)
        {
            CacheObjectControllerHelper.SetCell(cell, index, configEntries, null);
        }

        // UI Construction

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();

            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
        }

        protected override void ConstructPanelContent()
        {
            // Save button

            var saveBtn = UIFactory.CreateButton(ContentRoot, "Save", "Save Options", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(saveBtn.Component.gameObject, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 0);
            saveBtn.OnClick += ConfigManager.Handler.SaveConfig;

            // Config entries

            var scrollPool = UIFactory.CreateScrollPool<ConfigEntryCell>(ContentRoot, "ConfigEntries", out var scrollObj, out var scrollContent);

            scrollPool.Initialize(this);
        }
    }
}
