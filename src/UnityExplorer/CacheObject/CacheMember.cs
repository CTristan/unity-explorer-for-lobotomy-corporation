using System;
using System.Reflection;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.Views;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.EvaluateWidget;
using UnityExplorerForLobotomyCorporation.UniverseLib;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.ObjectPool;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject
{
    public abstract class CacheMember : CacheObjectBase
    {
        private static readonly Color evalEnabledColor = new Color(0.15f, 0.25f, 0.15f);
        private static readonly Color evalDisabledColor = new Color(0.15f, 0.15f, 0.15f);
        private object m_declaringInstance;
        public abstract Type DeclaringType { get; }
        public string NameForFiltering { get; protected set; }

        public object DeclaringInstance =>
            IsStatic ? null : m_declaringInstance = m_declaringInstance ?? Owner.Target.TryCast(DeclaringType);

        public abstract bool IsStatic { get; }

        public override bool HasArguments =>
            Arguments?.Length > 0 || GenericArguments.Length > 0;

        public ParameterInfo[] Arguments { get; protected set; } = new ParameterInfo[0];
        public Type[] GenericArguments { get; protected set; } = ArgumentUtility.EmptyTypes;
        public EvaluateWidget Evaluator { get; protected set; }

        public bool Evaluating =>
            Evaluator != null && Evaluator.UIRoot.activeSelf;

        public virtual void SetInspectorOwner(ReflectionInspector inspector,
            MemberInfo member)
        {
            Owner = inspector;
            if (this is CacheMethod)
            {
                NameLabelText = SignatureHighlighter.ParseMethod(member as MethodInfo);
            }
            else if (this is CacheConstructor)
            {
                NameLabelText = SignatureHighlighter.ParseConstructor(member as ConstructorInfo);
            }
            else
            {
                NameLabelText = SignatureHighlighter.Parse(member.DeclaringType, false, member);
            }

            NameForFiltering = SignatureHighlighter.RemoveHighlighting(NameLabelText);
            NameLabelTextRaw = NameForFiltering;
        }

        public override void ReleasePooledObjects()
        {
            base.ReleasePooledObjects();

            if (Evaluator != null)
            {
                Evaluator.OnReturnToPool();
                Pool<EvaluateWidget>.Return(Evaluator);
                Evaluator = null;
            }
        }

        public override void UnlinkFromView()
        {
            if (Evaluator != null)
            {
                Evaluator.UIRoot.transform.SetParent(Pool<EvaluateWidget>.Instance.InactiveHolder.transform, false);
            }

            base.UnlinkFromView();
        }

        protected abstract object TryEvaluate();

        protected abstract void TrySetValue(object value);

        /// <summary>Evaluate is called when first shown (if ShouldAutoEvaluate), or else when Evaluate button is clicked, or auto-updated.</summary>
        public void Evaluate()
        {
            SetValueFromSource(TryEvaluate());
        }

        /// <summary>Called when user presses the Evaluate button.</summary>
        public void EvaluateAndSetCell()
        {
            Evaluate();
            if (CellView != null)
            {
                SetDataToCell(CellView);
            }
        }

        public override void TrySetUserValue(object value)
        {
            TrySetValue(value);
            Evaluate();
        }

        protected override void SetValueState(CacheObjectCell cell,
            ValueStateArgs args)
        {
            base.SetValueState(cell, args);
        }

        protected override bool TryAutoEvaluateIfUnitialized(CacheObjectCell objectcell)
        {
            var cell = objectcell as CacheMemberCell;

            cell.EvaluateHolder.SetActive(!ShouldAutoEvaluate);
            if (!ShouldAutoEvaluate)
            {
                cell.EvaluateButton.Component.gameObject.SetActive(true);
                if (HasArguments)
                {
                    if (!Evaluating)
                    {
                        cell.EvaluateButton.ButtonText.text = $"Evaluate ({Arguments.Length + GenericArguments.Length})";
                    }
                    else
                    {
                        cell.EvaluateButton.ButtonText.text = "Hide";
                        Evaluator.UIRoot.transform.SetParent(cell.EvaluateHolder.transform, false);
                        RuntimeHelper.SetColorBlock(cell.EvaluateButton.Component, evalEnabledColor, evalEnabledColor * 1.3f);
                    }
                }
                else
                {
                    cell.EvaluateButton.ButtonText.text = "Evaluate";
                }

                if (!Evaluating)
                {
                    RuntimeHelper.SetColorBlock(cell.EvaluateButton.Component, evalDisabledColor, evalDisabledColor * 1.3f);
                }
            }

            if (State == ValueState.NotEvaluated && !ShouldAutoEvaluate)
            {
                SetValueState(cell, ValueStateArgs.Default);
                cell.RefreshSubcontentButton();

                return false;
            }

            if (State == ValueState.NotEvaluated)
            {
                Evaluate();
            }

            return true;
        }

        public void OnEvaluateClicked()
        {
            if (!HasArguments)
            {
                EvaluateAndSetCell();
            }
            else
            {
                if (Evaluator == null)
                {
                    Evaluator = Pool<EvaluateWidget>.Borrow();
                    Evaluator.OnBorrowedFromPool(this);
                    Evaluator.UIRoot.transform.SetParent((CellView as CacheMemberCell).EvaluateHolder.transform, false);
                    TryAutoEvaluateIfUnitialized(CellView);
                }
                else
                {
                    if (Evaluator.UIRoot.activeSelf)
                    {
                        Evaluator.UIRoot.SetActive(false);
                    }
                    else
                    {
                        Evaluator.UIRoot.SetActive(true);
                    }

                    TryAutoEvaluateIfUnitialized(CellView);
                }
            }
        }
    }
}
