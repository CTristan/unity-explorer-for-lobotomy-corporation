﻿using System;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ButtonList
{
    /// <summary>Represents the base cell used by a <see cref="ButtonListHandler{TData, TCell}" />.</summary>
    public class ButtonCell : ICell
    {
        public Action<int> OnClick;

        public int CurrentDataIndex { get; set; }
        public ButtonRef Button { get; private set; }

        // ICell
        public float DefaultHeight =>
            25f;

        public GameObject UIRoot { get; set; }
        public RectTransform Rect { get; set; }

        public bool Enabled =>
            UIRoot.activeSelf;

        public void Enable()
        {
            UIRoot.SetActive(true);
        }

        public void Disable()
        {
            UIRoot.SetActive(false);
        }

        public virtual GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateHorizontalGroup(parent, "ButtonCell", true, false, true, true, 2, default, new Color(0.11f, 0.11f, 0.11f), TextAnchor.MiddleCenter);
            Rect = UIRoot.GetComponent<RectTransform>();
            Rect.anchorMin = new Vector2(0, 1);
            Rect.anchorMax = new Vector2(0, 1);
            Rect.pivot = new Vector2(0.5f, 1);
            Rect.sizeDelta = new Vector2(25, 25);
            UIFactory.SetLayoutElement(UIRoot, 100, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);

            UIRoot.SetActive(false);

            Button = UIFactory.CreateButton(UIRoot, "NameButton", "Name");
            UIFactory.SetLayoutElement(Button.Component.gameObject, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);
            var buttonText = Button.Component.GetComponentInChildren<Text>();
            buttonText.horizontalOverflow = HorizontalWrapMode.Overflow;
            buttonText.alignment = TextAnchor.MiddleLeft;

            var normal = new Color(0.11f, 0.11f, 0.11f);
            var highlight = new Color(0.16f, 0.16f, 0.16f);
            var pressed = new Color(0.05f, 0.05f, 0.05f);
            var disabled = new Color(1, 1, 1, 0);
            RuntimeHelper.Instance.Internal_SetColorBlock(Button.Component, normal, highlight, pressed, disabled);

            Button.OnClick += () =>
            {
                OnClick?.Invoke(CurrentDataIndex);
            };

            return UIRoot;
        }
    }
}
