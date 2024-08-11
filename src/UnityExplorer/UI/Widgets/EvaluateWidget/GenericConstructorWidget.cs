using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.ObjectPool;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.EvaluateWidget
{
    public class GenericConstructorWidget
    {
        private GameObject ArgsHolder;

        private Type[] currentGenericParameters;
        private Action currentOnCancel;
        private Action<Type[]> currentOnSubmit;
        private GenericArgumentHandler[] handlers;
        private Text Title;

        public GameObject UIRoot;

        public void Show(Action<Type[]> onSubmit,
            Action onCancel,
            Type genericTypeDefinition)
        {
            Title.text = $"Setting generic arguments for {SignatureHighlighter.Parse(genericTypeDefinition, false)}...";

            OnShow(onSubmit, onCancel, genericTypeDefinition.GetGenericArguments());
        }

        public void Show(Action<Type[]> onSubmit,
            Action onCancel,
            MethodInfo genericMethodDefinition)
        {
            Title.text = $"Setting generic arguments for {SignatureHighlighter.ParseMethod(genericMethodDefinition)}...";

            OnShow(onSubmit, onCancel, genericMethodDefinition.GetGenericArguments());
        }

        private void OnShow(Action<Type[]> onSubmit,
            Action onCancel,
            Type[] genericParameters)
        {
            currentOnSubmit = onSubmit;
            currentOnCancel = onCancel;

            SetGenericParameters(genericParameters);
        }

        private void SetGenericParameters(Type[] genericParameters)
        {
            currentGenericParameters = genericParameters;

            handlers = new GenericArgumentHandler[genericParameters.Length];
            for (var i = 0; i < genericParameters.Length; i++)
            {
                var type = genericParameters[i];

                var holder = handlers[i] = Pool<GenericArgumentHandler>.Borrow();
                holder.UIRoot.transform.SetParent(ArgsHolder.transform, false);
                holder.OnBorrowed(type);
            }
        }

        public void TrySubmit()
        {
            var args = new Type[currentGenericParameters.Length];

            for (var i = 0; i < args.Length; i++)
            {
                var handler = handlers[i];
                Type arg;
                try
                {
                    arg = handler.Evaluate();
                    if (arg == null)
                    {
                        throw new Exception();
                    }
                }
                catch
                {
                    ExplorerCore.LogWarning($"Generic argument '{handler.inputField.Text}' is not a valid type.");

                    return;
                }

                args[i] = arg;
            }

            OnClose();
            currentOnSubmit(args);
        }

        public void Cancel()
        {
            OnClose();

            currentOnCancel?.Invoke();
        }

        private void OnClose()
        {
            if (handlers != null)
            {
                foreach (var widget in handlers)
                {
                    widget.OnReturned();
                    Pool<GenericArgumentHandler>.Return(widget);
                }

                handlers = null;
            }
        }

        // UI Construction

        internal void ConstructUI(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "GenericArgsHandler", false, false, true, true, 5, new Vector4(5, 5, 5, 5), childAlignment: TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(UIRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            var submitButton = UIFactory.CreateButton(UIRoot, "SubmitButton", "Submit", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(submitButton.GameObject, minHeight: 25, minWidth: 200);
            submitButton.OnClick += TrySubmit;

            var cancelButton = UIFactory.CreateButton(UIRoot, "CancelButton", "Cancel", new Color(0.3f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(cancelButton.GameObject, minHeight: 25, minWidth: 200);
            cancelButton.OnClick += Cancel;

            Title = UIFactory.CreateLabel(UIRoot, "Title", "Generic Arguments", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(Title.gameObject, minHeight: 25, flexibleWidth: 9999);

            var scrollview = UIFactory.CreateScrollView(UIRoot, "GenericArgsScrollView", out ArgsHolder, out _, new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(scrollview, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ArgsHolder, padTop: 5, padLeft: 5, padBottom: 5, padRight: 5);
        }
    }
}
