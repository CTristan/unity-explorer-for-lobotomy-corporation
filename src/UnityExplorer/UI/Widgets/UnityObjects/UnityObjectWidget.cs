using System;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.ObjectPool;
using Object = UnityEngine.Object;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.UnityObjects
{
    public class UnityObjectWidget : IPooledObject
    {
        public Component component;

        protected ButtonRef gameObjectButton;
        protected InputFieldRef instanceIdInput;
        protected InputFieldRef nameInput;
        public ReflectionInspector owner;
        public Object unityObject;

        // IPooledObject
        public GameObject UIRoot { get; set; }

        public float DefaultHeight =>
            -1;

        // UI construction

        public virtual GameObject CreateContent(GameObject uiRoot)
        {
            UIRoot = UIFactory.CreateUIObject("UnityObjectRow", uiRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(UIRoot, false, false, true, true, 5);
            UIFactory.SetLayoutElement(UIRoot, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            var nameLabel = UIFactory.CreateLabel(UIRoot, "NameLabel", "Name:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(nameLabel.gameObject, minHeight: 25, minWidth: 45, flexibleWidth: 0);

            nameInput = UIFactory.CreateInputField(UIRoot, "NameInput", "untitled");
            UIFactory.SetLayoutElement(nameInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 1000);
            nameInput.Component.readOnly = true;

            gameObjectButton = UIFactory.CreateButton(UIRoot, "GameObjectButton", "Inspect GameObject", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(gameObjectButton.Component.gameObject, minHeight: 25, minWidth: 160);
            gameObjectButton.OnClick += OnGameObjectButtonClicked;

            var instanceLabel = UIFactory.CreateLabel(UIRoot, "InstanceLabel", "Instance ID:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(instanceLabel.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);

            instanceIdInput = UIFactory.CreateInputField(UIRoot, "InstanceIDInput", "ERROR");
            UIFactory.SetLayoutElement(instanceIdInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            instanceIdInput.Component.readOnly = true;

            UIRoot.SetActive(false);

            return UIRoot;
        }

        public static UnityObjectWidget GetUnityWidget(object target,
            Type targetType,
            ReflectionInspector inspector)
        {
            if (!typeof(Object).IsAssignableFrom(targetType))
            {
                return null;
            }

            UnityObjectWidget widget;
            if (target is Texture2D || target is Cubemap || (target is Sprite s && s.texture) || (target is Image i && i.sprite?.texture))
            {
                widget = Pool<Texture2DWidget>.Borrow();
            }
            else if (target is Material && MaterialWidget.MaterialWidgetSupported)
            {
                widget = Pool<MaterialWidget>.Borrow();
            }
            else if (target is AudioClip)
            {
                widget = Pool<AudioClipWidget>.Borrow();
            }
            else
            {
                widget = Pool<UnityObjectWidget>.Borrow();
            }

            widget.OnBorrowed(target, targetType, inspector);

            return widget;
        }

        public virtual void OnBorrowed(object target,
            Type targetType,
            ReflectionInspector inspector)
        {
            owner = inspector;

            if (!UIRoot)
            {
                CreateContent(inspector.UIRoot);
            }
            else
            {
                UIRoot.transform.SetParent(inspector.UIRoot.transform);
            }

            UIRoot.transform.SetSiblingIndex(inspector.UIRoot.transform.childCount - 2);

            unityObject = target.TryCast<Object>();
            UIRoot.SetActive(true);

            nameInput.Text = unityObject.name;
            instanceIdInput.Text = unityObject.GetInstanceID().ToString();

            if (typeof(Component).IsAssignableFrom(targetType))
            {
                component = (Component)target.TryCast(typeof(Component));
                gameObjectButton.Component.gameObject.SetActive(true);
            }
            else
            {
                gameObjectButton.Component.gameObject.SetActive(false);
            }
        }

        public virtual void OnReturnToPool()
        {
            unityObject = null;
            component = null;
            owner = null;
        }

        // Update

        public virtual void Update()
        {
            if (unityObject)
            {
                nameInput.Text = unityObject.name;

                owner.Tab.TabText.text = $"{owner.TabButtonText} \"{unityObject.name}\"";
            }
        }

        // UI Listeners

        private void OnGameObjectButtonClicked()
        {
            if (!component)
            {
                ExplorerCore.LogWarning("Component reference is null or destroyed!");

                return;
            }

            InspectorManager.Inspect(component.gameObject);
        }
    }
}
