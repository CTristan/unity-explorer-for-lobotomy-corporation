using System;
using System.Collections.Generic;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ButtonList;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;
using Object = UnityEngine.Object;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.GameObjects
{
    public class ComponentList : ButtonListHandler<Component, ComponentCell>
    {
        private static readonly Dictionary<string, string> compToStringCache = new Dictionary<string, string>();
        public GameObjectInspector Parent;

        public ComponentList(ScrollPool<ComponentCell> scrollPool,
            Func<List<Component>> getEntriesMethod) : base(scrollPool, getEntriesMethod, null, null, null)
        {
            SetICell = SetComponentCell;
            ShouldDisplay = CheckShouldDisplay;
            OnCellClicked = OnComponentClicked;
        }

        public void Clear()
        {
            RefreshData();
            ScrollPool.Refresh(true, true);
        }

        private bool CheckShouldDisplay(Component _,
            string __)
        {
            return true;
        }

        public override void OnCellBorrowed(ComponentCell cell)
        {
            base.OnCellBorrowed(cell);

            cell.OnBehaviourToggled += OnBehaviourToggled;
            cell.OnDestroyClicked += OnDestroyClicked;
        }

        public override void SetCell(ComponentCell cell,
            int index)
        {
            base.SetCell(cell, index);
        }

        private void OnComponentClicked(int index)
        {
            var entries = GetEntries();

            if (index < 0 || index >= entries.Count)
            {
                return;
            }

            var comp = entries[index];
            if (comp)
            {
                InspectorManager.Inspect(comp);
            }
        }

        private void OnBehaviourToggled(bool value,
            int index)
        {
            try
            {
                var entries = GetEntries();
                var comp = entries[index];

                if (comp.TryCast<Behaviour>() is Behaviour behaviour)
                {
                    behaviour.enabled = value;
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception toggling Behaviour.enabled: {ex.ReflectionExToString()}");
            }
        }

        private void OnDestroyClicked(int index)
        {
            try
            {
                var entries = GetEntries();
                var comp = entries[index];

                Object.DestroyImmediate(comp);

                Parent.UpdateComponents();
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception destroying Component: {ex.ReflectionExToString()}");
            }
        }

        // Called from ButtonListHandler.SetCell, will be valid
        private void SetComponentCell(ComponentCell cell,
            int index)
        {
            var entries = GetEntries();
            cell.Enable();

            var comp = entries[index];
            var type = comp.GetActualType();

            if (!compToStringCache.ContainsKey(type.AssemblyQualifiedName))
            {
                compToStringCache.Add(type.AssemblyQualifiedName, SignatureHighlighter.Parse(type, true));
            }

            cell.Button.ButtonText.text = compToStringCache[type.AssemblyQualifiedName];

            if (typeof(Behaviour).IsAssignableFrom(type))
            {
                cell.BehaviourToggle.interactable = true;
                cell.BehaviourToggle.Set(comp.TryCast<Behaviour>().enabled, false);
                cell.BehaviourToggle.graphic.color = new Color(0.8f, 1, 0.8f, 0.3f);
            }
            else
            {
                cell.BehaviourToggle.interactable = false;
                cell.BehaviourToggle.Set(true, false);
                //RuntimeHelper.SetColorBlock(cell.BehaviourToggle,)
                cell.BehaviourToggle.graphic.color = new Color(0.2f, 0.2f, 0.2f);
            }

            // if component is the first index it must be the transform, dont show Destroy button for it.
            if (index == 0 && cell.DestroyButton.Component.gameObject.activeSelf)
            {
                cell.DestroyButton.Component.gameObject.SetActive(false);
            }
            else if (index > 0 && !cell.DestroyButton.Component.gameObject.activeSelf)
            {
                cell.DestroyButton.Component.gameObject.SetActive(true);
            }
        }
    }
}
