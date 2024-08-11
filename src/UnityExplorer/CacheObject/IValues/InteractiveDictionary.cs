using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.Views;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.IValues
{
    public class InteractiveDictionary : InteractiveValue, ICellPoolDataSource<CacheKeyValuePairCell>, ICacheObjectController
    {
        private readonly List<CacheKeyValuePair> cachedEntries = new List<CacheKeyValuePair>();

        public Type KeysType;

        public LayoutElement KeyTitleLayout;

        private Text NotSupportedLabel;
        public IDictionary RefIDictionary;

        private LayoutElement scrollLayout;

        public Text TopLabel;
        private RectTransform UIRect;
        public Type ValuesType;
        public LayoutElement ValueTitleLayout;

        public ScrollPool<CacheKeyValuePairCell> DictScrollPool { get; private set; }

        public int AdjustedWidth =>
            (int)UIRect.rect.width - 80;

        CacheObjectBase ICacheObjectController.ParentCacheObject =>
            CurrentOwner;

        object ICacheObjectController.Target =>
            CurrentOwner.Value;

        public Type TargetType { get; }

        public override bool CanWrite =>
            base.CanWrite && RefIDictionary != null && !RefIDictionary.IsReadOnly;

        public int ItemCount =>
            cachedEntries.Count;

        // KVP entry scroll pool

        public void OnCellBorrowed(CacheKeyValuePairCell cell)
        {
        }

        public void SetCell(CacheKeyValuePairCell cell,
            int index)
        {
            CacheObjectControllerHelper.SetCell(cell, index, cachedEntries, SetCellLayout);
        }

        public override void OnBorrowed(CacheObjectBase owner)
        {
            base.OnBorrowed(owner);

            DictScrollPool.Refresh(true, true);
        }

        public override void ReleaseFromOwner()
        {
            base.ReleaseFromOwner();

            ClearAndRelease();
        }

        private void ClearAndRelease()
        {
            RefIDictionary = null;

            foreach (var entry in cachedEntries)
            {
                entry.UnlinkFromView();
                entry.ReleasePooledObjects();
            }

            cachedEntries.Clear();
        }

        public override void SetValue(object value)
        {
            if (value == null)
            {
                // should never be null
                ClearAndRelease();

                return;
            }

            var type = value.GetActualType();
            ReflectionUtility.TryGetEntryTypes(type, out KeysType, out ValuesType);

            CacheEntries(value);

            TopLabel.text = $"[{cachedEntries.Count}] {SignatureHighlighter.Parse(type, false)}";

            DictScrollPool.Refresh(true);
        }

        private void CacheEntries(object value)
        {
            RefIDictionary = value as IDictionary;

            if (ReflectionUtility.TryGetDictEnumerator(value, out var dictEnumerator))
            {
                NotSupportedLabel.gameObject.SetActive(false);

                var idx = 0;
                while (dictEnumerator.MoveNext())
                {
                    CacheKeyValuePair cache;
                    if (idx >= cachedEntries.Count)
                    {
                        cache = new CacheKeyValuePair();
                        cache.SetDictOwner(this, idx);
                        cachedEntries.Add(cache);
                    }
                    else
                    {
                        cache = cachedEntries[idx];
                    }

                    cache.SetFallbackType(ValuesType);
                    cache.SetKey(dictEnumerator.Current.Key);
                    cache.SetValueFromSource(dictEnumerator.Current.Value);

                    idx++;
                }

                // Remove excess cached entries if dict count decreased
                if (cachedEntries.Count > idx)
                {
                    for (var i = cachedEntries.Count - 1; i >= idx; i--)
                    {
                        var cache = cachedEntries[i];
                        if (cache.CellView != null)
                        {
                            cache.UnlinkFromView();
                        }

                        cache.ReleasePooledObjects();
                        cachedEntries.RemoveAt(i);
                    }
                }
            }
            else
            {
                NotSupportedLabel.gameObject.SetActive(true);
            }
        }

        // Setting value to dictionary

        public void TrySetValueToKey(object key,
            object value,
            int keyIndex)
        {
            try
            {
                if (!RefIDictionary.Contains(key))
                {
                    ExplorerCore.LogWarning("Unable to set key! Key may have been boxed to/from Il2Cpp Object.");

                    return;
                }

                RefIDictionary[key] = value;

                var entry = cachedEntries[keyIndex];
                entry.SetValueFromSource(value);
                if (entry.CellView != null)
                {
                    entry.SetDataToCell(entry.CellView);
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception setting IDictionary key! {ex}");
            }
        }

        public override void SetLayout()
        {
            var minHeight = 5f;

            KeyTitleLayout.minWidth = AdjustedWidth * 0.44f;
            ValueTitleLayout.minWidth = AdjustedWidth * 0.55f;

            foreach (var cell in DictScrollPool.CellPool)
            {
                SetCellLayout(cell);
                if (cell.Enabled)
                {
                    minHeight += cell.Rect.rect.height;
                }
            }

            scrollLayout.minHeight = Math.Min(InspectorPanel.CurrentPanelHeight - 400f, minHeight);
        }

        private void SetCellLayout(CacheObjectCell objcell)
        {
            var cell = objcell as CacheKeyValuePairCell;
            cell.KeyGroupLayout.minWidth = cell.AdjustedWidth * 0.44f;
            cell.RightGroupLayout.minWidth = cell.AdjustedWidth * 0.55f;

            if (cell.Occupant?.IValue != null)
            {
                cell.Occupant.IValue.SetLayout();
            }
        }

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "InteractiveDict", true, true, true, true, 6, new Vector4(10, 3, 15, 4), new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutElement(UIRoot, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 475);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UIRect = UIRoot.GetComponent<RectTransform>();

            // Entries label

            TopLabel = UIFactory.CreateLabel(UIRoot, "EntryLabel", "not set", fontSize: 16);
            TopLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            // key / value titles

            var titleGroup = UIFactory.CreateUIObject("TitleGroup", UIRoot);
            UIFactory.SetLayoutElement(titleGroup, minHeight: 25, flexibleWidth: 9999, flexibleHeight: 0);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(titleGroup, false, true, true, true, padLeft: 65, padRight: 0, childAlignment: TextAnchor.LowerLeft);

            var keyTitle = UIFactory.CreateLabel(titleGroup, "KeyTitle", "Keys");
            UIFactory.SetLayoutElement(keyTitle.gameObject, 100, flexibleWidth: 0);
            KeyTitleLayout = keyTitle.GetComponent<LayoutElement>();

            var valueTitle = UIFactory.CreateLabel(titleGroup, "ValueTitle", "Values");
            UIFactory.SetLayoutElement(valueTitle.gameObject, 100, flexibleWidth: 0);
            ValueTitleLayout = valueTitle.GetComponent<LayoutElement>();

            // entry scroll pool

            DictScrollPool = UIFactory.CreateScrollPool<CacheKeyValuePairCell>(UIRoot, "EntryList", out var scrollObj, out var _, new Color(0.09f, 0.09f, 0.09f));
            UIFactory.SetLayoutElement(scrollObj, minHeight: 150, flexibleHeight: 0);
            DictScrollPool.Initialize(this, SetLayout);
            scrollLayout = scrollObj.GetComponent<LayoutElement>();

            NotSupportedLabel = UIFactory.CreateLabel(DictScrollPool.Content.gameObject, "NotSupportedMessage",
                "The IDictionary failed to enumerate. This is likely due to an issue with Unhollowed interfaces.", TextAnchor.MiddleLeft, Color.red);

            UIFactory.SetLayoutElement(NotSupportedLabel.gameObject, minHeight: 25, flexibleWidth: 9999);
            NotSupportedLabel.gameObject.SetActive(false);

            return UIRoot;
        }
    }
}
