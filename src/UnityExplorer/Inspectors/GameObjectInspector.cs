using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.AutoComplete;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.GameObjects;
using UnityExplorerForLobotomyCorporation.UniverseLib;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.TransformTree;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors
{
    public class GameObjectInspector : InspectorBase
    {
        private readonly List<bool> behaviourEnabledStates = new List<bool>();
        private readonly List<Behaviour> behaviourEntries = new List<Behaviour>();
        private readonly List<GameObject> cachedChildren = new List<GameObject>();
        private readonly HashSet<int> compInstanceIDs = new HashSet<int>();

        private readonly List<Component> componentEntries = new List<Component>();

        private InputFieldRef addChildInput;
        private InputFieldRef addCompInput;

        public ComponentList ComponentList;
        private ScrollPool<ComponentCell> componentScroll;

        public GameObject Content;

        public GameObjectControls Controls;

        private float timeOfLastUpdate;
        private ScrollPool<TransformCell> transformScroll;

        public TransformTree TransformTree;

        public new GameObject Target =>
            base.Target as GameObject;

        public override void OnBorrowedFromPool(object target)
        {
            base.OnBorrowedFromPool(target);

            base.Target = target as GameObject;

            Controls.UpdateGameObjectInfo(true, true);
            Controls.TransformControl.UpdateTransformControlValues(true);

            RuntimeHelper.StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            yield return null;

            LayoutRebuilder.ForceRebuildLayoutImmediate(InspectorPanel.Instance.ContentRect);

            TransformTree.Rebuild();

            ComponentList.ScrollPool.Refresh(true, true);
            UpdateComponents();
        }

        public override void OnReturnToPool()
        {
            base.OnReturnToPool();

            addChildInput.Text = "";
            addCompInput.Text = "";

            TransformTree.Clear();
            UpdateComponents();
        }

        public override void CloseInspector()
        {
            InspectorManager.ReleaseInspector(this);
        }

        public void OnTransformCellClicked(GameObject newTarget)
        {
            base.Target = newTarget;
            Controls.UpdateGameObjectInfo(true, true);
            Controls.TransformControl.UpdateTransformControlValues(true);
            TransformTree.RefreshData(true, false, true, false);
            UpdateComponents();
        }

        public override void Update()
        {
            if (!IsActive)
            {
                return;
            }

            if (base.Target.IsNullOrDestroyed(false))
            {
                InspectorManager.ReleaseInspector(this);

                return;
            }

            Controls.UpdateVectorSlider();
            Controls.TransformControl.UpdateTransformControlValues(false);

            // Slow update
            if (timeOfLastUpdate.OccuredEarlierThan(1))
            {
                timeOfLastUpdate = Time.realtimeSinceStartup;

                Controls.UpdateGameObjectInfo(false, false);

                TransformTree.RefreshData(true, false, false, false);
                UpdateComponents();
            }
        }

        // Child and Component Lists

        private IEnumerable<GameObject> GetTransformEntries()
        {
            if (!Target)
            {
                return Enumerable.Empty<GameObject>();
            }

            cachedChildren.Clear();
            for (var i = 0; i < Target.transform.childCount; i++) cachedChildren.Add(Target.transform.GetChild(i).gameObject);

            return cachedChildren;
        }

        // ComponentList.GetRootEntriesMethod
        private List<Component> GetComponentEntries()
        {
            return Target ? componentEntries : Enumerable.Empty<Component>().ToList();
        }

        public void UpdateComponents()
        {
            if (!Target)
            {
                componentEntries.Clear();
                compInstanceIDs.Clear();
                behaviourEntries.Clear();
                behaviourEnabledStates.Clear();
                ComponentList.RefreshData();
                ComponentList.ScrollPool.Refresh(true, true);

                return;
            }

            // Check if we actually need to refresh the component cells or not.
            IEnumerable<Component> comps = Target.GetComponents<Component>();
            IEnumerable<Behaviour> behaviours = Target.GetComponents<Behaviour>();

            var needRefresh = false;

            var count = 0;
            foreach (var comp in comps)
            {
                if (!comp)
                {
                    continue;
                }

                count++;
                if (!compInstanceIDs.Contains(comp.GetInstanceID()))
                {
                    needRefresh = true;

                    break;
                }
            }

            if (!needRefresh)
            {
                if (count != componentEntries.Count)
                {
                    needRefresh = true;
                }
                else
                {
                    count = 0;
                    foreach (var behaviour in behaviours)
                    {
                        if (!behaviour)
                        {
                            continue;
                        }

                        if (count >= behaviourEnabledStates.Count || behaviour.enabled != behaviourEnabledStates[count])
                        {
                            needRefresh = true;

                            break;
                        }

                        count++;
                    }

                    if (!needRefresh && count != behaviourEntries.Count)
                    {
                        needRefresh = true;
                    }
                }
            }

            if (!needRefresh)
            {
                return;
            }

            componentEntries.Clear();
            compInstanceIDs.Clear();
            foreach (var comp in comps)
            {
                if (!comp)
                {
                    continue;
                }

                componentEntries.Add(comp);
                compInstanceIDs.Add(comp.GetInstanceID());
            }

            behaviourEntries.Clear();
            behaviourEnabledStates.Clear();
            foreach (var behaviour in behaviours)
            {
                if (!behaviour)
                {
                    continue;
                }

                // Don't ask me how, but in some games this can be true for certain components.
                // They get picked up from GetComponents<Behaviour>, but they are not actually Behaviour...?
                if (!typeof(Behaviour).IsAssignableFrom(behaviour.GetType()))
                {
                    continue;
                }

                try
                {
                    behaviourEntries.Add(behaviour);
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning(ex);
                }

                behaviourEnabledStates.Add(behaviour.enabled);
            }

            ComponentList.RefreshData();
            ComponentList.ScrollPool.Refresh(true);
        }


        private void OnAddChildClicked(string input)
        {
            var newObject = new GameObject(input);
            newObject.transform.parent = Target.transform;

            TransformTree.RefreshData(true, false, true, false);
        }

        private void OnAddComponentClicked(string input)
        {
            if (ReflectionUtility.GetTypeByName(input) is Type type)
            {
                try
                {
                    RuntimeHelper.AddComponent<Component>(Target, type);
                    UpdateComponents();
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning($"Exception adding component: {ex.ReflectionExToString()}");
                }
            }
            else
            {
                ExplorerCore.LogWarning($"Could not find any Type by the name '{input}'!");
            }
        }

        #region UI Construction

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "GameObjectInspector", true, false, true, true, 5, new Vector4(4, 4, 4, 4), new Color(0.065f, 0.065f, 0.065f));

            var scrollObj = UIFactory.CreateScrollView(UIRoot, "GameObjectInspector", out Content, out var scrollbar, new Color(0.065f, 0.065f, 0.065f));
            UIFactory.SetLayoutElement(scrollObj, minHeight: 250, preferredHeight: 300, flexibleHeight: 0, flexibleWidth: 9999);

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(Content, spacing: 3, padTop: 2, padBottom: 2, padLeft: 2, padRight: 2);

            // Construct GO Controls
            Controls = new GameObjectControls(this);

            ConstructLists();

            return UIRoot;
        }

        // Child and Comp Lists

        private void ConstructLists()
        {
            var listHolder = UIFactory.CreateUIObject("ListHolders", UIRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(listHolder, false, true, true, true, 8, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(listHolder, minHeight: 150, flexibleWidth: 9999, flexibleHeight: 9999);

            // Left group (Children)

            var leftGroup = UIFactory.CreateUIObject("ChildrenGroup", listHolder);
            UIFactory.SetLayoutElement(leftGroup, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(leftGroup, false, false, true, true, 2);

            var childrenLabel = UIFactory.CreateLabel(leftGroup, "ChildListTitle", "Children", TextAnchor.MiddleCenter, default, false, 16);
            UIFactory.SetLayoutElement(childrenLabel.gameObject, flexibleWidth: 9999);

            // Add Child
            var addChildRow = UIFactory.CreateUIObject("AddChildRow", leftGroup);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(addChildRow, false, false, true, true, 2);

            addChildInput = UIFactory.CreateInputField(addChildRow, "AddChildInput", "Enter a name...");
            UIFactory.SetLayoutElement(addChildInput.Component.gameObject, minHeight: 25, preferredWidth: 9999);

            var addChildButton = UIFactory.CreateButton(addChildRow, "AddChildButton", "Add Child");
            UIFactory.SetLayoutElement(addChildButton.Component.gameObject, minHeight: 25, minWidth: 80);
            addChildButton.OnClick += () =>
            {
                OnAddChildClicked(addChildInput.Text);
            };

            // TransformTree

            transformScroll = UIFactory.CreateScrollPool<TransformCell>(leftGroup, "TransformTree", out var transformObj, out var transformContent, new Color(0.11f, 0.11f, 0.11f));

            TransformTree = new TransformTree(transformScroll, GetTransformEntries, OnTransformCellClicked);

            // Right group (Components)

            var rightGroup = UIFactory.CreateUIObject("ComponentGroup", listHolder);
            UIFactory.SetLayoutElement(rightGroup, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(rightGroup, false, false, true, true, 2);

            var compLabel = UIFactory.CreateLabel(rightGroup, "CompListTitle", "Components", TextAnchor.MiddleCenter, default, false, 16);
            UIFactory.SetLayoutElement(compLabel.gameObject, flexibleWidth: 9999);

            // Add Comp
            var addCompRow = UIFactory.CreateUIObject("AddCompRow", rightGroup);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(addCompRow, false, false, true, true, 2);

            addCompInput = UIFactory.CreateInputField(addCompRow, "AddCompInput", "Enter a Component type...");
            UIFactory.SetLayoutElement(addCompInput.Component.gameObject, minHeight: 25, preferredWidth: 9999);

            var addCompButton = UIFactory.CreateButton(addCompRow, "AddCompButton", "Add Comp");
            UIFactory.SetLayoutElement(addCompButton.Component.gameObject, minHeight: 25, minWidth: 80);
            addCompButton.OnClick += () =>
            {
                OnAddComponentClicked(addCompInput.Text);
            };

            // comp autocompleter
            new TypeCompleter(typeof(Component), addCompInput, false, false, false);

            // Component List

            componentScroll = UIFactory.CreateScrollPool<ComponentCell>(rightGroup, "ComponentList", out var compObj, out var compContent, new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(compObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(compContent, flexibleHeight: 9999);

            ComponentList = new ComponentList(componentScroll, GetComponentEntries)
            {
                Parent = this,
            };

            componentScroll.Initialize(ComponentList);
        }

        #endregion
    }
}
