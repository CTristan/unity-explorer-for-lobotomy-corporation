using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels;
using UnityExplorerForLobotomyCorporation.UniverseLib;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.Hooks
{
    public class HookList : ICellPoolDataSource<HookCell>
    {
        internal static readonly HashSet<string> hookedSignatures = new HashSet<string>();
        internal static readonly OrderedDictionary currentHooks = new OrderedDictionary();

        internal static GameObject UIRoot;
        internal static ScrollPool<HookCell> HooksScrollPool;

        public int ItemCount =>
            currentHooks.Count;

        // Set current hook cell

        public void OnCellBorrowed(HookCell cell)
        {
        }

        public void SetCell(HookCell cell,
            int index)
        {
            if (index >= currentHooks.Count)
            {
                cell.Disable();

                return;
            }

            cell.CurrentDisplayedIndex = index;
            var hook = (HookInstance)currentHooks[index];

            cell.MethodNameLabel.text = SignatureHighlighter.ParseMethod(hook.TargetMethod);

            cell.ToggleActiveButton.ButtonText.text = hook.Enabled ? "On" : "Off";
            RuntimeHelper.SetColorBlockAuto(cell.ToggleActiveButton.Component, hook.Enabled ? new Color(0.15f, 0.2f, 0.15f) : new Color(0.2f, 0.2f, 0.15f));
        }

        // public static void EnableOrDisableHookClicked(int index)
        // {
        //     var hook = (HookInstance)currentHooks[index];
        //     hook.TogglePatch();
        //
        //     HooksScrollPool.Refresh(true);
        // }

        // public static void DeleteHookClicked(int index)
        // {
        //     var hook = (HookInstance)currentHooks[index];
        //
        //     if (HookCreator.CurrentEditedHook == hook)
        //     {
        //         HookCreator.EditorInputCancel();
        //     }
        //
        //     hook.Unpatch();
        //     currentHooks.RemoveAt(index);
        //     hookedSignatures.Remove(hook.TargetMethod.FullDescription());
        //
        //     HooksScrollPool.Refresh(true);
        // }

        public static void EditPatchClicked(int index)
        {
            if (HookCreator.PendingGeneric)
            {
                HookManagerPanel.genericArgsHandler.Cancel();
            }

            HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.HookSourceEditor);
            var hook = (HookInstance)currentHooks[index];
            HookCreator.SetEditedHook(hook);
        }

        // UI

        internal void ConstructUI(GameObject leftGroup)
        {
            UIRoot = UIFactory.CreateUIObject("CurrentHooksPanel", leftGroup);
            UIFactory.SetLayoutElement(UIRoot, preferredHeight: 150, flexibleHeight: 0, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(UIRoot, true, true, true, true);

            var hooksLabel = UIFactory.CreateLabel(UIRoot, "HooksLabel", "Current Hooks", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(hooksLabel.gameObject, minHeight: 30, flexibleWidth: 9999);

            HooksScrollPool = UIFactory.CreateScrollPool<HookCell>(UIRoot, "HooksScrollPool", out var hooksScroll, out var hooksContent);
            UIFactory.SetLayoutElement(hooksScroll, flexibleHeight: 9999);
            HooksScrollPool.Initialize(this);
        }
    }
}
