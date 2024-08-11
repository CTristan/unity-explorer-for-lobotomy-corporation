using System;
using System.Collections;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.IValues;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.Views;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.ObjectPool;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;
using Object = UnityEngine.Object;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject
{
    public enum ValueState
    {
        NotEvaluated,
        Exception,
        Boolean,
        Number,
        String,
        Enum,
        Collection,
        Dictionary,
        ValueStruct,
        Color,
        Unsupported,
    }

    public abstract class CacheObjectBase
    {
        protected const string NOT_YET_EVAL = "<color=grey>Not yet evaluated</color>";
        private static GameObject inactiveIValueHolder;
        private Type currentValueType;
        private bool valueIsNull;
        public ICacheObjectController Owner { get; set; }
        public CacheObjectCell CellView { get; internal set; }

        public object Value { get; protected set; }
        public Type FallbackType { get; protected set; }
        public ValueState State { get; set; }
        public Exception LastException { get; protected set; }

        // InteractiveValues
        public InteractiveValue IValue { get; private set; }
        public Type CurrentIValueType { get; private set; }
        public bool SubContentShowWanted { get; private set; }

        // UI
        public string NameLabelText { get; protected set; }
        public string NameLabelTextRaw { get; protected set; }
        public string ValueLabelText { get; protected set; }

        // Abstract
        public abstract bool ShouldAutoEvaluate { get; }
        public abstract bool HasArguments { get; }
        public abstract bool CanWrite { get; }

        internal static GameObject InactiveIValueHolder
        {
            get
            {
                if (!inactiveIValueHolder)
                {
                    inactiveIValueHolder = new GameObject("Temp_IValue_Holder");
                    Object.DontDestroyOnLoad(inactiveIValueHolder);
                    inactiveIValueHolder.hideFlags = HideFlags.HideAndDontSave;
                    inactiveIValueHolder.transform.parent = UniversalUI.PoolHolder.transform;
                    inactiveIValueHolder.SetActive(false);
                }

                return inactiveIValueHolder;
            }
        }

        public virtual void SetFallbackType(Type fallbackType)
        {
            FallbackType = fallbackType;
            ValueLabelText = GetValueLabel();
        }

        public virtual void SetView(CacheObjectCell cellView)
        {
            CellView = cellView;
            cellView.Occupant = this;
        }

        public virtual void UnlinkFromView()
        {
            if (CellView == null)
            {
                return;
            }

            CellView.Occupant = null;
            CellView = null;

            if (IValue != null)
            {
                IValue.UIRoot.transform.SetParent(InactiveIValueHolder.transform, false);
            }
        }

        public virtual void ReleasePooledObjects()
        {
            if (IValue != null)
            {
                ReleaseIValue();
            }

            if (CellView != null)
            {
                UnlinkFromView();
            }
        }

        // Updating and applying values

        // The only method which sets the CacheObjectBase.Value
        public virtual void SetValueFromSource(object value)
        {
            Value = value;

            if (!Value.IsNullOrDestroyed())
            {
                Value = Value.TryCast();
            }

            ProcessOnEvaluate();

            if (IValue != null)
            {
                if (SubContentShowWanted)
                {
                    IValue.SetValue(Value);
                }
                else
                {
                    IValue.PendingValueWanted = true;
                }
            }
        }

        public void SetUserValue(object value)
        {
            value = value.TryCast(FallbackType);

            TrySetUserValue(value);

            if (CellView != null)
            {
                SetDataToCell(CellView);
            }

            // If the owner's ParentCacheObject is set, we are setting the value of an inspected struct.
            // Set the inspector target as the value back to that parent.
            if (Owner.ParentCacheObject != null)
            {
                Owner.ParentCacheObject.SetUserValue(Owner.Target);
            }
        }

        public abstract void TrySetUserValue(object value);

        protected virtual void ProcessOnEvaluate()
        {
            var prevState = State;

            if (LastException != null)
            {
                valueIsNull = true;
                currentValueType = FallbackType;
                State = ValueState.Exception;
            }
            else if (Value.IsNullOrDestroyed())
            {
                valueIsNull = true;
                State = GetStateForType(FallbackType);
            }
            else
            {
                valueIsNull = false;
                State = GetStateForType(Value.GetActualType());
            }

            if (IValue != null)
            {
                // If we changed states (always needs IValue change)
                // or if the value is null, and the fallback type isnt string (we always want to edit strings).
                if (State != prevState || (State != ValueState.String && State != ValueState.Exception && Value.IsNullOrDestroyed()))
                {
                    // need to return IValue
                    ReleaseIValue();
                    SubContentShowWanted = false;
                }
            }

            // Set label text
            ValueLabelText = GetValueLabel();
        }

        public ValueState GetStateForType(Type type)
        {
            if (currentValueType == type && (State != ValueState.Exception || LastException != null))
            {
                return State;
            }

            currentValueType = type;
            if (type == typeof(bool))
            {
                return ValueState.Boolean;
            }

            if (type.IsPrimitive || type == typeof(decimal))
            {
                return ValueState.Number;
            }

            if (type == typeof(string))
            {
                return ValueState.String;
            }

            if (type.IsEnum)
            {
                return ValueState.Enum;
            }

            if (type == typeof(Color) || type == typeof(Color32))
            {
                return ValueState.Color;
            }

            if (InteractiveValueStruct.SupportsType(type))
            {
                return ValueState.ValueStruct;
            }

            if (ReflectionUtility.IsDictionary(type))
            {
                return ValueState.Dictionary;
            }

            if (!typeof(Transform).IsAssignableFrom(type) && ReflectionUtility.IsEnumerable(type))
            {
                return ValueState.Collection;
            }

            return ValueState.Unsupported;
        }

        protected string GetValueLabel()
        {
            var label = string.Empty;

            switch (State)
            {
                case ValueState.NotEvaluated:
                    return $"<i>{NOT_YET_EVAL} ({SignatureHighlighter.Parse(FallbackType, true)})</i>";

                case ValueState.Exception:
                    return $"<i><color=#eb4034>{LastException.ReflectionExToString()}</color></i>";

                // bool and number dont want the label for the value at all
                case ValueState.Boolean:
                case ValueState.Number:
                    return null;

                // and valuestruct also doesnt want it if we can parse it
                case ValueState.ValueStruct:
                    if (ParseUtility.CanParse(currentValueType))
                    {
                        return null;
                    }

                    break;

                // string wants it trimmed to max 200 chars
                case ValueState.String:
                    if (!valueIsNull)
                    {
                        return $"\"{ToStringUtility.PruneString(Value as string)}\"";
                    }

                    break;

                // try to prefix the count of the collection for lists and dicts
                case ValueState.Collection:
                    if (!valueIsNull)
                    {
                        if (Value is IList iList)
                        {
                            label = $"[{iList.Count}] ";
                        }
                        else if (Value is ICollection iCol)
                        {
                            label = $"[{iCol.Count}] ";
                        }
                        else
                        {
                            label = "[?] ";
                        }
                    }

                    break;

                case ValueState.Dictionary:
                    if (!valueIsNull)
                    {
                        if (Value is IDictionary iDict)
                        {
                            label = $"[{iDict.Count}] ";
                        }
                        else
                        {
                            label = "[?] ";
                        }
                    }

                    break;
            }

            // Cases which dont return will append to ToStringWithType

            return label += ToStringUtility.ToStringWithType(Value, FallbackType);
        }

        // Setting cell state from our model

        /// <summary>Return false if SetCell should abort, true if it should continue.</summary>
        protected abstract bool TryAutoEvaluateIfUnitialized(CacheObjectCell cell);

        public virtual void SetDataToCell(CacheObjectCell cell)
        {
            cell.NameLabel.text = NameLabelText;
            if (cell.HiddenNameLabel != null)
            {
                cell.HiddenNameLabel.Text = NameLabelTextRaw ?? string.Empty;
            }

            cell.ValueLabel.gameObject.SetActive(true);

            cell.SubContentHolder.gameObject.SetActive(SubContentShowWanted);
            if (IValue != null)
            {
                IValue.UIRoot.transform.SetParent(cell.SubContentHolder.transform, false);
                IValue.SetLayout();
            }

            var evaluated = TryAutoEvaluateIfUnitialized(cell);

            if (cell.CopyButton != null)
            {
                var canCopy = State != ValueState.NotEvaluated && State != ValueState.Exception;
                cell.CopyButton.Component.gameObject.SetActive(canCopy);
                cell.PasteButton.Component.gameObject.SetActive(canCopy && CanWrite);
            }

            if (!evaluated)
            {
                return;
            }

            // The following only executes if the object has evaluated.
            // For members and properties with args, they will return by default now.

            switch (State)
            {
                case ValueState.Exception:
                    SetValueState(cell, new ValueStateArgs(subContentButtonActive: true));

                    break;
                case ValueState.Boolean:
                    SetValueState(cell, new ValueStateArgs(false, toggleActive: true, applyActive: CanWrite));

                    break;
                case ValueState.Number:
                    SetValueState(cell, new ValueStateArgs(false, typeLabelActive: true, inputActive: true, applyActive: CanWrite));

                    break;
                case ValueState.String:
                    if (valueIsNull)
                    {
                        SetValueState(cell, new ValueStateArgs(subContentButtonActive: true));
                    }
                    else
                    {
                        SetValueState(cell, new ValueStateArgs(true, false, SignatureHighlighter.StringOrange, subContentButtonActive: true));
                    }

                    break;
                case ValueState.Enum:
                    SetValueState(cell, new ValueStateArgs(subContentButtonActive: CanWrite));

                    break;
                case ValueState.Color:
                case ValueState.ValueStruct:
                    if (ParseUtility.CanParse(currentValueType))
                    {
                        SetValueState(cell, new ValueStateArgs(false, false, null, true, false, true, CanWrite, true, true));
                    }
                    else
                    {
                        SetValueState(cell, new ValueStateArgs(inspectActive: true, subContentButtonActive: true));
                    }

                    break;
                case ValueState.Collection:
                case ValueState.Dictionary:
                    SetValueState(cell, new ValueStateArgs(inspectActive: !valueIsNull, subContentButtonActive: !valueIsNull));

                    break;
                case ValueState.Unsupported:
                    SetValueState(cell, new ValueStateArgs(inspectActive: !valueIsNull));

                    break;
            }

            cell.RefreshSubcontentButton();
        }

        protected virtual void SetValueState(CacheObjectCell cell,
            ValueStateArgs args)
        {
            // main value label
            if (args.valueActive)
            {
                cell.ValueLabel.text = ValueLabelText;
                cell.ValueLabel.supportRichText = args.valueRichText;
                cell.ValueLabel.color = args.valueColor;
            }
            else
            {
                cell.ValueLabel.text = "";
            }

            // Type label (for primitives)
            cell.TypeLabel.gameObject.SetActive(args.typeLabelActive);
            if (args.typeLabelActive)
            {
                cell.TypeLabel.text = SignatureHighlighter.Parse(currentValueType, false);
            }

            // toggle for bools
            cell.Toggle.gameObject.SetActive(args.toggleActive);
            if (args.toggleActive)
            {
                cell.Toggle.interactable = CanWrite;
                cell.Toggle.isOn = (bool)Value;
                cell.ToggleText.text = Value.ToString();
            }

            // inputfield for numbers
            cell.InputField.UIRoot.SetActive(args.inputActive);
            if (args.inputActive)
            {
                cell.InputField.Text = ParseUtility.ToStringForInput(Value, currentValueType);
                cell.InputField.Component.readOnly = !CanWrite;
            }

            // apply for bool and numbers
            cell.ApplyButton.Component.gameObject.SetActive(args.applyActive);

            // Inspect button only if last value not null.
            if (cell.InspectButton != null)
            {
                cell.InspectButton.Component.gameObject.SetActive(args.inspectActive && !valueIsNull);
            }

            // set subcontent button if needed, and for null strings and exceptions
            cell.SubContentButton.Component.gameObject.SetActive(args.subContentButtonActive && (!valueIsNull || State == ValueState.String || State == ValueState.Exception));
        }

        // CacheObjectCell Apply

        public virtual void OnCellApplyClicked()
        {
            if (State == ValueState.Boolean)
            {
                SetUserValue(CellView.Toggle.isOn);
            }
            else
            {
                if (ParseUtility.TryParse(CellView.InputField.Text, currentValueType, out var value, out var ex))
                {
                    SetUserValue(value);
                }
                else
                {
                    ExplorerCore.LogWarning("Unable to parse input!");
                    if (ex != null)
                    {
                        ExplorerCore.Log(ex.ReflectionExToString());
                    }
                }
            }

            SetDataToCell(CellView);
        }

        // IValues

        public virtual void OnCellSubContentToggle()
        {
            if (IValue == null)
            {
                var ivalueType = InteractiveValue.GetIValueTypeForState(State);

                if (ivalueType == null)
                {
                    return;
                }

                IValue = (InteractiveValue)Pool.Borrow(ivalueType);
                CurrentIValueType = ivalueType;

                IValue.OnBorrowed(this);
                IValue.SetValue(Value);
                IValue.UIRoot.transform.SetParent(CellView.SubContentHolder.transform, false);
                CellView.SubContentHolder.SetActive(true);
                SubContentShowWanted = true;

                // update our cell after creating the ivalue (the value may have updated, make sure its consistent)
                ProcessOnEvaluate();
                SetDataToCell(CellView);
            }
            else
            {
                SubContentShowWanted = !SubContentShowWanted;
                CellView.SubContentHolder.SetActive(SubContentShowWanted);

                if (SubContentShowWanted && IValue.PendingValueWanted)
                {
                    IValue.PendingValueWanted = false;
                    ProcessOnEvaluate();
                    SetDataToCell(CellView);
                    IValue.SetValue(Value);
                }
            }

            CellView.RefreshSubcontentButton();
        }

        public virtual void ReleaseIValue()
        {
            if (IValue == null)
            {
                return;
            }

            IValue.ReleaseFromOwner();
            Pool.Return(CurrentIValueType, IValue);

            IValue = null;
        }

        // Value state args helper

        public struct ValueStateArgs
        {
            public static ValueStateArgs Default { get; } = new ValueStateArgs(true);

            public Color valueColor;
            public bool valueActive, valueRichText, typeLabelActive, toggleActive, inputActive, applyActive, inspectActive, subContentButtonActive;

            public ValueStateArgs(bool valueActive = true,
                bool valueRichText = true,
                Color? valueColor = null,
                bool typeLabelActive = false,
                bool toggleActive = false,
                bool inputActive = false,
                bool applyActive = false,
                bool inspectActive = false,
                bool subContentButtonActive = false)
            {
                this.valueActive = valueActive;
                this.valueRichText = valueRichText;
                this.valueColor = valueColor == null ? Color.white : (Color)valueColor;
                this.typeLabelActive = typeLabelActive;
                this.toggleActive = toggleActive;
                this.inputActive = inputActive;
                this.applyActive = applyActive;
                this.inspectActive = inspectActive;
                this.subContentButtonActive = subContentButtonActive;
            }
        }
    }
}
