using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.TransformTree
{
    public class TransformTree : ICellPoolDataSource<TransformCell>
    {
        private readonly HashSet<int> autoExpandedIDs = new HashSet<int>();

        // IMPORTANT CAVEAT WITH OrderedDictionary:
        // While the performance is mostly good, there are two methods we should NEVER use:
        // - Remove(object)
        // - set_Item[object]
        // These two methods have extremely bad performance due to using IndexOfKey(), which iterates the whole dictionary.
        // Currently we do not use either of these methods, so everything should be constant time lookups.
        // We DO make use of get_Item[object], get_Item[index], Add, Insert, Contains and RemoveAt, which OrderedDictionary meets our needs for.
        /// <summary>Key: UnityEngine.Transform instance ID<br /> Value: CachedTransform</summary>
        internal readonly OrderedDictionary cachedTransforms = new OrderedDictionary();

        // for keeping track of which actual transforms are expanded or not, outside of the cache data.
        private readonly HashSet<int> expandedInstanceIDs = new HashSet<int>();
        private readonly Stopwatch traversedThisFrame = new Stopwatch();

        // state for Traverse parse
        private readonly HashSet<int> visited = new HashSet<int>();
        private string currentFilter;
        private int displayIndex;

        /// <summary>The method used to retrieve the list of GameObjects in this TransformTree</summary>
        public Func<IEnumerable<GameObject>> GetRootEntriesMethod;

        private bool needRefreshUI;

        /// <summary>Invoked when a Transform is clicked on.</summary>
        public Action<GameObject> OnClickHandler;

        private Coroutine refreshCoroutine;
        private int rootIndex;

        /// <summary>The ScrollPool used by this TransformTree.</summary>
        public ScrollPool<TransformCell> ScrollPool;

        private bool wasFiltering;

        /// <summary>Create a new TransformTree for the provided <see cref="ScrollPool{TransformCell}" />. This constructor will call ScrollPool.Initialize(this).</summary>
        /// <param name="scrollPool">The already-created <see cref="ScrollPool{TransformCell}" /> which will be used by this TransformTree.</param>
        /// <param name="getRootEntriesMethod">Your method to provide GameObjects for this TransformTree.</param>
        /// <param name="onCellClicked">Your method to be invoked when a Transform cell is clicked on.</param>
        public TransformTree(ScrollPool<TransformCell> scrollPool,
            Func<IEnumerable<GameObject>> getRootEntriesMethod,
            Action<GameObject> onCellClicked)
        {
            ScrollPool = scrollPool;
            GetRootEntriesMethod = getRootEntriesMethod;
            OnClickHandler = onCellClicked;

            ScrollPool.Initialize(this);
        }

        // Search filter
        public bool Filtering =>
            !string.IsNullOrEmpty(currentFilter);

        /// <summary>Can set to filter the displayed Transforms in this tree. You must call RefreshData after changing the filter.</summary>
        public string CurrentFilter
        {
            get =>
                currentFilter;
            set
            {
                currentFilter = value ?? "";
                if (!wasFiltering && Filtering)
                {
                    wasFiltering = true;
                }
                else if (wasFiltering && !Filtering)
                {
                    wasFiltering = false;
                    autoExpandedIDs.Clear();
                }
            }
        }

        // ScrollPool item count. PrevDisplayIndex is the highest index + 1 from our last traverse.
        public int ItemCount { get; private set; }

        /// <summary>Called by ScrollPool, not necessary to ever call this directly.</summary>
        public void SetCell(TransformCell cell,
            int index)
        {
            if (index < cachedTransforms.Count)
            {
                cell.ConfigureCell((CachedTransform)cachedTransforms[index]);
                if (Filtering)
                {
                    if (cell.cachedTransform.Name.ContainsIgnoreCase(currentFilter))
                    {
                        cell.NameButton.ButtonText.color = Color.green;
                    }
                }
            }
            else
            {
                cell.Disable();
            }
        }

        /// <summary>Called by ScrollPool, not necessary to ever call this directly.</summary>
        public void OnCellBorrowed(TransformCell cell)
        {
            cell.OnExpandToggled += OnCellExpandToggled;
            cell.OnGameObjectClicked += OnGameObjectClicked;
            cell.OnEnableToggled += OnCellEnableToggled;
        }

        /// <summary>Completely reset the tree (ie. switching inspected GameObject)</summary>
        public void Rebuild()
        {
            autoExpandedIDs.Clear();
            expandedInstanceIDs.Clear();

            RefreshData(true, true, true, false);
        }

        /// <summary>Completely wipe the cached data (ie. GameObject inspector returning to pool)</summary>
        public void Clear()
        {
            cachedTransforms.Clear();
            displayIndex = 0;
            autoExpandedIDs.Clear();
            expandedInstanceIDs.Clear();
            ScrollPool.Refresh(true, true);
            if (refreshCoroutine != null)
            {
                RuntimeHelper.StopCoroutine(refreshCoroutine);
                refreshCoroutine = null;
            }
        }

        /// <summary>Check if the given Instance ID is expanded or not</summary>
        public bool IsTransformExpanded(int instanceID)
        {
            return Filtering ? autoExpandedIDs.Contains(instanceID) : expandedInstanceIDs.Contains(instanceID);
        }

        /// <summary>Jump to a specific Transform in the tree and highlight it.</summary>
        public void JumpAndExpandToTransform(Transform transform)
        {
            if (!transform)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            // Refresh cached transforms (no UI rebuild yet).
            // Stop existing coroutine and do it oneshot.
            RefreshData(false, false, true, true);

            var transformID = transform.GetInstanceID();

            // find the index of our transform in the list
            var idx = -1;
            for (idx = 0; idx < cachedTransforms.Count; idx++)
            {
                var cache = (CachedTransform)cachedTransforms[idx];
                if (cache.InstanceID == transformID)
                {
                    break;
                }
            }

            if (idx == -1)
            {
                throw new ArgumentException($"Transform {transform.name} is not cached in this TransformTree.");
            }

            // make sure all parents of the object are expanded
            var parent = transform.parent;
            while (parent)
            {
                var pid = parent.GetInstanceID();
                if (!expandedInstanceIDs.Contains(pid))
                {
                    expandedInstanceIDs.Add(pid);
                }

                parent = parent.parent;
            }

            ScrollPool.JumpToIndex(idx, OnCellJumpedTo);
        }

        private void OnCellJumpedTo(TransformCell cell)
        {
            RuntimeHelper.StartCoroutine(HighlightCellCoroutine(cell));
        }

        private IEnumerator HighlightCellCoroutine(TransformCell cell)
        {
            var button = cell.NameButton.Component;
            button.StartColorTween(new Color(0.2f, 0.3f, 0.2f), false);

            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 1.5f)
            {
                yield return null;
            }

            button.OnDeselect(null);
        }

        /// <summary>Perform an update of all Transforms in this tree.</summary>
        /// <param name="andUpdateScrollPool">If true, calls ScrollPool.Refresh</param>
        /// <param name="jumpToTop">Should the ScrollPool reset back to the top index?</param>
        /// <param name="stopExistingCoroutine">If false and there is a Refresh coroutine already running, this method will abort.</param>
        /// <param name="oneShot">If true, will not yield any frames in the coroutine (will all happen instantly, now)</param>
        public void RefreshData(bool andUpdateScrollPool,
            bool jumpToTop,
            bool stopExistingCoroutine,
            bool oneShot)
        {
            if (refreshCoroutine != null)
            {
                if (stopExistingCoroutine)
                {
                    RuntimeHelper.StopCoroutine(refreshCoroutine);
                    refreshCoroutine = null;
                }
                else
                {
                    return;
                }
            }

            visited.Clear();
            displayIndex = 0;
            rootIndex = 0;
            needRefreshUI = false;
            traversedThisFrame.Reset();
            traversedThisFrame.Start();

            refreshCoroutine = RuntimeHelper.StartCoroutine(RefreshCoroutine(andUpdateScrollPool, jumpToTop, oneShot));
        }

        private IEnumerator RefreshCoroutine(bool andRefreshUI,
            bool jumpToTop,
            bool oneShot)
        {
            // Instead of doing string.IsNullOrEmpty(CurrentFilter) many times, let's just do it once per update.
            var filtering = Filtering;

            var rootObjects = GetRootEntriesMethod();
            foreach (var gameObj in rootObjects)
            {
                if (!gameObj)
                {
                    continue;
                }

                var enumerator = Traverse(gameObj.transform, null, 0, oneShot, filtering);
                while (enumerator.MoveNext())
                {
                    if (!oneShot)
                    {
                        yield return enumerator.Current;
                    }
                }
            }

            // Prune displayed transforms that we didnt visit in that traverse
            for (var i = cachedTransforms.Count - 1; i >= 0; i--)
            {
                if (traversedThisFrame.ElapsedMilliseconds > 2)
                {
                    yield return null;
                    traversedThisFrame.Reset();
                    traversedThisFrame.Start();
                }

                var cached = (CachedTransform)cachedTransforms[i];
                if (!visited.Contains(cached.InstanceID))
                {
                    cachedTransforms.RemoveAt(i);
                    needRefreshUI = true;
                }
            }

            if (andRefreshUI && needRefreshUI)
            {
                ScrollPool.Refresh(true, jumpToTop);
            }

            ItemCount = displayIndex;
            refreshCoroutine = null;
        }

        // Recursive method to check a Transform and its children (if expanded).
        // Parent and depth can be null/default.
        private IEnumerator Traverse(Transform transform,
            CachedTransform parent,
            int depth,
            bool oneShot,
            bool filtering)
        {
            if (traversedThisFrame.ElapsedMilliseconds > 2)
            {
                yield return null;
                traversedThisFrame.Reset();
                traversedThisFrame.Start();
            }

            var instanceID = transform.GetInstanceID();

            // Unlikely, but since this method is async it could theoretically happen in extremely rare circumstances
            if (visited.Contains(instanceID))
            {
                yield break;
            }

            if (filtering)
            {
                if (!FilterHierarchy(transform))
                {
                    yield break;
                }

                if (!autoExpandedIDs.Contains(instanceID))
                {
                    autoExpandedIDs.Add(instanceID);
                }
            }

            visited.Add(instanceID);

            var isRootObject = transform.parent == null;

            CachedTransform cached;
            if (cachedTransforms.Contains(instanceID))
            {
                cached = (CachedTransform)cachedTransforms[(object)instanceID];
                var prevSiblingIdx = cached.SiblingIndex;

                var updated = cached.Update(transform, depth);

                if (isRootObject)
                {
                    cached.SiblingIndex = rootIndex;
                    rootIndex++;
                }

                if (updated || cached.SiblingIndex != prevSiblingIdx)
                {
                    needRefreshUI = true;

                    // If the sibling index changed, we need to shuffle it in our cached transforms list.
                    if (prevSiblingIdx != cached.SiblingIndex)
                    {
                        cachedTransforms.Remove(instanceID);
                        if (cachedTransforms.Count <= displayIndex)
                        {
                            cachedTransforms.Add(instanceID, cached);
                        }
                        else
                        {
                            cachedTransforms.Insert(displayIndex, instanceID, cached);
                        }
                    }
                }
            }
            else
            {
                needRefreshUI = true;
                cached = new CachedTransform(this, transform, depth, parent);
                if (cachedTransforms.Count <= displayIndex)
                {
                    cachedTransforms.Add(instanceID, cached);
                }
                else
                {
                    cachedTransforms.Insert(displayIndex, instanceID, cached);
                }

                if (isRootObject)
                {
                    cached.SiblingIndex = rootIndex;
                    rootIndex++;
                }
            }

            displayIndex++;

            if (IsTransformExpanded(instanceID) && cached.Value.childCount > 0)
            {
                for (var i = 0; i < transform.childCount; i++)
                {
                    var enumerator = Traverse(transform.GetChild(i), cached, depth + 1, oneShot, filtering);
                    while (enumerator.MoveNext())
                    {
                        if (!oneShot)
                        {
                            yield return enumerator.Current;
                        }
                    }
                }
            }
        }

        private bool FilterHierarchy(Transform obj)
        {
            if (obj.name.ContainsIgnoreCase(currentFilter))
            {
                return true;
            }

            if (obj.childCount <= 0)
            {
                return false;
            }

            for (var i = 0; i < obj.childCount; i++)
                if (FilterHierarchy(obj.GetChild(i)))
                {
                    return true;
                }

            return false;
        }

        private void OnGameObjectClicked(GameObject obj)
        {
            OnClickHandler?.Invoke(obj);
        }

        private void OnCellExpandToggled(CachedTransform cache)
        {
            var instanceID = cache.InstanceID;
            if (expandedInstanceIDs.Contains(instanceID))
            {
                expandedInstanceIDs.Remove(instanceID);
            }
            else
            {
                expandedInstanceIDs.Add(instanceID);
            }

            RefreshData(true, false, true, true);
        }

        private void OnCellEnableToggled(CachedTransform cache)
        {
            cache.Value.gameObject.SetActive(!cache.Value.gameObject.activeSelf);

            RefreshData(true, false, true, true);
        }
    }
}
