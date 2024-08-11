using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.Views;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Config;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.EvaluateWidget;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.UnityObjects;
using UnityExplorerForLobotomyCorporation.UniverseLib;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.ObjectPool;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors
{
    [Flags]
    public enum MemberFilter
    {
        None = 0,
        Property = 1,
        Field = 2,
        Constructor = 4,
        Method = 8,
        All = Property | Field | Method | Constructor,
    }

    public class ReflectionInspector : InspectorBase, ICellPoolDataSource<CacheMemberCell>, ICacheObjectController
    {
        private static readonly Color disabledButtonColor = new Color(0.24f, 0.24f, 0.24f);
        private static readonly Color enabledButtonColor = new Color(0.2f, 0.27f, 0.2f);
        private readonly List<CacheMember> filteredMembers = new List<CacheMember>();
        private readonly List<Toggle> memberTypeToggles = new List<Toggle>();
        private readonly Dictionary<BindingFlags, ButtonRef> scopeFilterButtons = new Dictionary<BindingFlags, ButtonRef>();
        private Text assemblyText;
        private Toggle autoUpdateToggle;

        private ButtonRef dnSpyButton;

        private InputFieldRef filterInputField;
        private GenericConstructorWidget genericConstructor;

        private InputFieldRef hiddenNameText;
        private BindingFlags lastFlagsFilter;
        private MemberFilter lastMemberFilter = MemberFilter.All;
        private string lastNameFilter;

        private ButtonRef makeGenericButton;
        private MemberFilter memberFilter = MemberFilter.All;

        private List<CacheMember> members = new List<CacheMember>();

        private string nameFilter;
        private Text nameText;

        // Updating

        private bool refreshWanted;
        private BindingFlags scopeFlagsFilter;
        private float timeOfLastAutoUpdate;
        public bool StaticOnly { get; internal set; }

        public bool AutoUpdateWanted =>
            autoUpdateToggle.isOn;

        // UI

        private static int LeftGroupWidth { get; set; }
        private static int RightGroupWidth { get; set; }

        public GameObject ContentRoot { get; private set; }
        public ScrollPool<CacheMemberCell> MemberScrollPool { get; private set; }
        public UnityObjectWidget UnityWidget { get; private set; }
        public string TabButtonText { get; set; }
        public CacheObjectBase ParentCacheObject { get; set; }

        public bool CanWrite =>
            true;

        public int ItemCount =>
            filteredMembers.Count;

        // Member cells

        public void OnCellBorrowed(CacheMemberCell cell)
        {
        } // not needed

        public void SetCell(CacheMemberCell cell,
            int index)
        {
            CacheObjectControllerHelper.SetCell(cell, index, filteredMembers, SetCellLayout);
        }

        // Setup

        public override void OnBorrowedFromPool(object target)
        {
            base.OnBorrowedFromPool(target);
            CalculateLayouts();

            SetTarget(target);

            RuntimeHelper.StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(InspectorPanel.Instance.ContentRect);
        }

        public override void CloseInspector()
        {
            InspectorManager.ReleaseInspector(this);
        }

        public override void OnReturnToPool()
        {
            foreach (var member in members)
            {
                member.UnlinkFromView();
                member.ReleasePooledObjects();
            }

            members.Clear();
            filteredMembers.Clear();

            autoUpdateToggle.isOn = false;

            if (UnityWidget != null)
            {
                UnityWidget.OnReturnToPool();
                Pool.Return(UnityWidget.GetType(), UnityWidget);
                UnityWidget = null;
            }

            genericConstructor?.Cancel();

            base.OnReturnToPool();
        }

        // Setting target

        private void SetTarget(object target)
        {
            string prefix;
            if (StaticOnly)
            {
                Target = null;
                TargetType = target as Type;
                prefix = "[S]";

                makeGenericButton.GameObject.SetActive(TargetType.IsGenericTypeDefinition);
            }
            else
            {
                TargetType = target.GetActualType();
                prefix = "[R]";
            }

            // Setup main labels and tab text
            TabButtonText = $"{prefix} {SignatureHighlighter.Parse(TargetType, false)}";
            Tab.TabText.text = TabButtonText;
            nameText.text = SignatureHighlighter.Parse(TargetType, true);
            hiddenNameText.Text = SignatureHighlighter.RemoveHighlighting(nameText.text);

            string asmText;
            if (TargetType.Assembly is AssemblyBuilder || string.IsNullOrEmpty(TargetType.Assembly.Location))
            {
                asmText = $"{TargetType.Assembly.GetName().Name} <color=grey><i>(in memory)</i></color>";
                dnSpyButton.GameObject.SetActive(false);
            }
            else
            {
                asmText = Path.GetFileName(TargetType.Assembly.Location);
                dnSpyButton.GameObject.SetActive(true);
            }

            assemblyText.text = $"<color=grey>Assembly:</color> {asmText}";

            // Unity object helper widget

            if (!StaticOnly)
            {
                UnityWidget = UnityObjectWidget.GetUnityWidget(target, TargetType, this);
            }

            // Get cache members

            members = CacheMemberFactory.GetCacheMembers(TargetType, this);

            // reset filters

            filterInputField.Text = string.Empty;

            SetFilter(string.Empty, StaticOnly ? BindingFlags.Static : BindingFlags.Default);
            scopeFilterButtons[BindingFlags.Default].Component.gameObject.SetActive(!StaticOnly);
            scopeFilterButtons[BindingFlags.Instance].Component.gameObject.SetActive(!StaticOnly);

            foreach (var toggle in memberTypeToggles)
            {
                toggle.isOn = true;
            }

            refreshWanted = true;
        }

        // Updating

        public override void Update()
        {
            if (!IsActive)
            {
                return;
            }

            if (!StaticOnly && Target.IsNullOrDestroyed(false))
            {
                InspectorManager.ReleaseInspector(this);

                return;
            }

            // check filter changes or force-refresh
            if (refreshWanted || nameFilter != lastNameFilter || scopeFlagsFilter != lastFlagsFilter || lastMemberFilter != memberFilter)
            {
                lastNameFilter = nameFilter;
                lastFlagsFilter = scopeFlagsFilter;
                lastMemberFilter = memberFilter;

                FilterMembers();
                MemberScrollPool.Refresh(true, true);
                refreshWanted = false;
            }

            // once-per-second updates
            if (timeOfLastAutoUpdate.OccuredEarlierThan(1))
            {
                timeOfLastAutoUpdate = Time.realtimeSinceStartup;

                if (UnityWidget != null)
                {
                    UnityWidget.Update();
                }

                if (AutoUpdateWanted)
                {
                    UpdateDisplayedMembers();
                }
            }
        }

        // Filtering

        public void SetFilter(string name,
            BindingFlags flags)
        {
            nameFilter = name;

            if (flags != scopeFlagsFilter)
            {
                var btn = scopeFilterButtons[scopeFlagsFilter].Component;
                RuntimeHelper.SetColorBlock(btn, disabledButtonColor, disabledButtonColor * 1.3f);

                scopeFlagsFilter = flags;
                btn = scopeFilterButtons[scopeFlagsFilter].Component;
                RuntimeHelper.SetColorBlock(btn, enabledButtonColor, enabledButtonColor * 1.3f);
            }
        }

        private void FilterMembers()
        {
            filteredMembers.Clear();

            for (var i = 0; i < members.Count; i++)
            {
                var member = members[i];

                if (scopeFlagsFilter != BindingFlags.Default)
                {
                    if ((scopeFlagsFilter == BindingFlags.Instance && member.IsStatic) || (scopeFlagsFilter == BindingFlags.Static && !member.IsStatic))
                    {
                        continue;
                    }
                }

                if ((member is CacheMethod && !memberFilter.HasFlag(MemberFilter.Method)) || (member is CacheField && !memberFilter.HasFlag(MemberFilter.Field)) ||
                    (member is CacheProperty && !memberFilter.HasFlag(MemberFilter.Property)) || (member is CacheConstructor && !memberFilter.HasFlag(MemberFilter.Constructor)))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(nameFilter) && !member.NameForFiltering.ContainsIgnoreCase(nameFilter))
                {
                    continue;
                }

                filteredMembers.Add(member);
            }
        }

        private void UpdateDisplayedMembers()
        {
            var shouldRefresh = false;
            foreach (var cell in MemberScrollPool.CellPool)
            {
                if (!cell.Enabled || cell.Occupant == null)
                {
                    continue;
                }

                var member = cell.MemberOccupant;
                if (member.ShouldAutoEvaluate)
                {
                    shouldRefresh = true;
                    member.Evaluate();
                    member.SetDataToCell(member.CellView);
                }
            }

            if (shouldRefresh)
            {
                MemberScrollPool.Refresh(false);
            }
        }

        // Cell layout (fake table alignment)

        internal void SetLayouts()
        {
            CalculateLayouts();

            foreach (var cell in MemberScrollPool.CellPool)
            {
                SetCellLayout(cell);
            }
        }

        private void CalculateLayouts()
        {
            LeftGroupWidth = (int)Math.Max(200, 0.4f * InspectorManager.PanelWidth - 5);
            RightGroupWidth = (int)Math.Max(200, InspectorManager.PanelWidth - LeftGroupWidth - 65);
        }

        private void SetCellLayout(CacheObjectCell cell)
        {
            cell.NameLayout.minWidth = LeftGroupWidth;
            cell.RightGroupLayout.minWidth = RightGroupWidth;

            if (cell.Occupant?.IValue != null)
            {
                cell.Occupant.IValue.SetLayout();
            }
        }

        // UI listeners

        private void OnUpdateClicked()
        {
            UpdateDisplayedMembers();
        }

        public void OnSetNameFilter(string name)
        {
            SetFilter(name, scopeFlagsFilter);
        }

        public void OnSetFlags(BindingFlags flags)
        {
            SetFilter(nameFilter, flags);
        }

        private void OnMemberTypeToggled(MemberFilter flag,
            bool val)
        {
            if (!val)
            {
                memberFilter &= ~flag;
            }
            else
            {
                memberFilter |= flag;
            }
        }

        private void OnCopyClicked()
        {
            ClipboardPanel.Copy(Target ?? TargetType);
        }

        private void OnDnSpyButtonClicked()
        {
            var path = ConfigManager.DnSpy_Path.Value;
            if (File.Exists(path) && path.EndsWith("dnspy.exe", StringComparison.OrdinalIgnoreCase))
            {
                var type = TargetType;
                // if constructed generic type, use the generic type definition
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    type = type.GetGenericTypeDefinition();
                }

                var args = $"\"{type.Assembly.Location}\" --select T:{type.FullName}";
                Process.Start(path, args);
            }
            else
            {
                Notification.ShowMessage("Please set a valid dnSpy path in UnityExplorer Settings.");
            }
        }

        private void OnMakeGenericClicked()
        {
            ContentRoot.SetActive(false);

            if (genericConstructor == null)
            {
                genericConstructor = new GenericConstructorWidget();
                genericConstructor.ConstructUI(UIRoot);
            }

            genericConstructor.UIRoot.SetActive(true);
            genericConstructor.Show(OnGenericSubmit, OnGenericCancel, TargetType);
        }

        private void OnGenericSubmit(Type[] args)
        {
            ContentRoot.SetActive(true);
            genericConstructor.UIRoot.SetActive(false);

            var newType = TargetType.MakeGenericType(args);
            InspectorManager.Inspect(newType);
            //InspectorManager.ReleaseInspector(this);
        }

        private void OnGenericCancel()
        {
            ContentRoot.SetActive(true);
            genericConstructor.UIRoot.SetActive(false);
        }

        // UI Construction

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "ReflectionInspector", true, true, true, true, 5, new Vector4(4, 4, 4, 4), new Color(0.065f, 0.065f, 0.065f));

            // Class name, assembly

            var topRow = UIFactory.CreateHorizontalGroup(UIRoot, "TopRow", false, false, true, true, 4, default, new Color(0.1f, 0.1f, 0.1f), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(topRow, minHeight: 25, flexibleWidth: 9999);

            var titleHolder = UIFactory.CreateUIObject("TitleHolder", topRow);
            UIFactory.SetLayoutElement(titleHolder, minHeight: 35, flexibleHeight: 0, flexibleWidth: 9999);

            nameText = UIFactory.CreateLabel(titleHolder, "VisibleTitle", "NotSet");
            var namerect = nameText.GetComponent<RectTransform>();
            namerect.anchorMin = new Vector2(0, 0);
            namerect.anchorMax = new Vector2(1, 1);
            nameText.fontSize = 17;
            UIFactory.SetLayoutElement(nameText.gameObject, minHeight: 35, flexibleHeight: 0, minWidth: 300, flexibleWidth: 9999);

            hiddenNameText = UIFactory.CreateInputField(titleHolder, "Title", "not set");
            var hiddenrect = hiddenNameText.Component.gameObject.GetComponent<RectTransform>();
            hiddenrect.anchorMin = new Vector2(0, 0);
            hiddenrect.anchorMax = new Vector2(1, 1);
            hiddenNameText.Component.readOnly = true;
            hiddenNameText.Component.lineType = InputField.LineType.MultiLineNewline;
            hiddenNameText.Component.gameObject.GetComponent<Image>().color = Color.clear;
            hiddenNameText.Component.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            hiddenNameText.Component.textComponent.fontSize = 17;
            hiddenNameText.Component.textComponent.color = Color.clear;
            UIFactory.SetLayoutElement(hiddenNameText.Component.gameObject, minHeight: 35, flexibleHeight: 0, flexibleWidth: 9999);

            makeGenericButton = UIFactory.CreateButton(topRow, "MakeGenericButton", "Construct Generic", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(makeGenericButton.GameObject, 140, 25);
            makeGenericButton.OnClick += OnMakeGenericClicked;
            makeGenericButton.GameObject.SetActive(false);

            var copyButton = UIFactory.CreateButton(topRow, "CopyButton", "Copy to Clipboard", new Color(0.2f, 0.2f, 0.2f, 1));
            copyButton.ButtonText.color = Color.yellow;
            UIFactory.SetLayoutElement(copyButton.Component.gameObject, minHeight: 25, minWidth: 120, flexibleWidth: 0);
            copyButton.OnClick += OnCopyClicked;

            // Assembly row

            var asmRow = UIFactory.CreateHorizontalGroup(UIRoot, "AssemblyRow", false, false, true, true, 5, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(asmRow, flexibleWidth: 9999, minHeight: 25);

            assemblyText = UIFactory.CreateLabel(asmRow, "AssemblyLabel", "not set");
            UIFactory.SetLayoutElement(assemblyText.gameObject, minHeight: 25, flexibleWidth: 9999);

            dnSpyButton = UIFactory.CreateButton(asmRow, "DnSpyButton", "View in dnSpy");
            UIFactory.SetLayoutElement(dnSpyButton.GameObject, 120, 25);
            dnSpyButton.OnClick += OnDnSpyButtonClicked;

            // Content

            ContentRoot = UIFactory.CreateVerticalGroup(UIRoot, "ContentRoot", false, false, true, true, 5, new Vector4(2, 2, 2, 2), new Color(0.12f, 0.12f, 0.12f));
            UIFactory.SetLayoutElement(ContentRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            ConstructFirstRow(ContentRoot);

            ConstructSecondRow(ContentRoot);

            // Member scroll pool

            var memberBorder = UIFactory.CreateVerticalGroup(ContentRoot, "ScrollPoolHolder", false, false, true, true, padding: new Vector4(2, 2, 2, 2), bgColor: new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutElement(memberBorder, flexibleWidth: 9999, flexibleHeight: 9999);

            MemberScrollPool = UIFactory.CreateScrollPool<CacheMemberCell>(memberBorder, "MemberList", out var scrollObj, out var _, new Color(0.09f, 0.09f, 0.09f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            MemberScrollPool.Initialize(this);

            // For debugging scroll pool
            //InspectorPanel.Instance.UIRoot.GetComponent<Mask>().enabled = false;
            //MemberScrollPool.Viewport.GetComponent<Mask>().enabled = false;
            //MemberScrollPool.Viewport.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);

            return UIRoot;
        }

        // First row

        private void ConstructFirstRow(GameObject parent)
        {
            var rowObj = UIFactory.CreateUIObject("FirstRow", parent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(rowObj, true, true, true, true, 5, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(rowObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            var nameLabel = UIFactory.CreateLabel(rowObj, "NameFilterLabel", "Filter names:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(nameLabel.gameObject, minHeight: 25, minWidth: 90, flexibleWidth: 0);

            filterInputField = UIFactory.CreateInputField(rowObj, "NameFilterInput", "...");
            UIFactory.SetLayoutElement(filterInputField.UIRoot, minHeight: 25, flexibleWidth: 300);
            filterInputField.OnValueChanged += val =>
            {
                OnSetNameFilter(val);
            };

            var spacer = UIFactory.CreateUIObject("Spacer", rowObj);
            UIFactory.SetLayoutElement(spacer, 25);

            // Update button and toggle

            var updateButton = UIFactory.CreateButton(rowObj, "UpdateButton", "Update displayed values", new Color(0.22f, 0.28f, 0.22f));
            UIFactory.SetLayoutElement(updateButton.Component.gameObject, minHeight: 25, minWidth: 175, flexibleWidth: 0);
            updateButton.OnClick += OnUpdateClicked;

            var toggleObj = UIFactory.CreateToggle(rowObj, "AutoUpdateToggle", out autoUpdateToggle, out var toggleText);
            UIFactory.SetLayoutElement(toggleObj, 125, 25);
            autoUpdateToggle.isOn = false;
            toggleText.text = "Auto-update";
        }

        // Second row

        private void ConstructSecondRow(GameObject parent)
        {
            var rowObj = UIFactory.CreateUIObject("SecondRow", parent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(rowObj, false, false, true, true, 5, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(rowObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Scope buttons

            var scopeLabel = UIFactory.CreateLabel(rowObj, "ScopeLabel", "Scope:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(scopeLabel.gameObject, minHeight: 25, minWidth: 60, flexibleWidth: 0);
            AddScopeFilterButton(rowObj, BindingFlags.Default, true);
            AddScopeFilterButton(rowObj, BindingFlags.Instance);
            AddScopeFilterButton(rowObj, BindingFlags.Static);

            var spacer = UIFactory.CreateUIObject("Spacer", rowObj);
            UIFactory.SetLayoutElement(spacer, 15);

            // Member type toggles

            AddMemberTypeToggle(rowObj, MemberTypes.Property, 90);
            AddMemberTypeToggle(rowObj, MemberTypes.Field, 70);
            AddMemberTypeToggle(rowObj, MemberTypes.Method, 90);
            AddMemberTypeToggle(rowObj, MemberTypes.Constructor, 110);
        }

        private void AddScopeFilterButton(GameObject parent,
            BindingFlags flags,
            bool setAsActive = false)
        {
            var lbl = flags == BindingFlags.Default ? "All" : flags.ToString();
            var color = setAsActive ? enabledButtonColor : disabledButtonColor;

            var button = UIFactory.CreateButton(parent, "Filter_" + flags, lbl, color);
            UIFactory.SetLayoutElement(button.Component.gameObject, minHeight: 25, flexibleHeight: 0, minWidth: 70, flexibleWidth: 0);
            scopeFilterButtons.Add(flags, button);

            button.OnClick += () =>
            {
                OnSetFlags(flags);
            };
        }

        private void AddMemberTypeToggle(GameObject parent,
            MemberTypes type,
            int width)
        {
            var toggleObj = UIFactory.CreateToggle(parent, "Toggle_" + type, out var toggle, out var toggleText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, minWidth: width);
            string color;
            if (type == MemberTypes.Method)
            {
                color = SignatureHighlighter.METHOD_INSTANCE;
            }
            else if (type == MemberTypes.Field)
            {
                color = SignatureHighlighter.FIELD_INSTANCE;
            }
            else if (type == MemberTypes.Property)
            {
                color = SignatureHighlighter.PROP_INSTANCE;
            }
            else if (type == MemberTypes.Constructor)
            {
                color = SignatureHighlighter.CLASS_INSTANCE;
            }
            else
            {
                throw new NotImplementedException();
            }

            toggleText.text = $"<color={color}>{type}</color>";

            toggle.graphic.TryCast<Image>().color = color.ToColor() * 0.65f;

            MemberFilter flag;
            if (type == MemberTypes.Method)
            {
                flag = MemberFilter.Method;
            }
            else if (type == MemberTypes.Property)
            {
                flag = MemberFilter.Property;
            }
            else if (type == MemberTypes.Field)
            {
                flag = MemberFilter.Field;
            }
            else if (type == MemberTypes.Constructor)
            {
                flag = MemberFilter.Constructor;
            }
            else
            {
                throw new NotImplementedException();
            }

            toggle.onValueChanged.AddListener(val =>
            {
                OnMemberTypeToggled(flag, val);
            });

            memberTypeToggles.Add(toggle);
        }
    }
}
