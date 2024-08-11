using System;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.GameObjects
{
    // Controls a Vector3 property of a Transform, and holds references to each AxisControl for X/Y/Z.

    public class Vector3Control
    {
        private Vector3 lastValue;

        public Vector3Control(TransformControls owner,
            TransformType type,
            InputFieldRef input)
        {
            Owner = owner;
            Type = type;
            MainInput = input;
        }

        public TransformControls Owner { get; }

        public GameObject Target =>
            Owner.Owner.Target;

        public Transform Transform =>
            Target.transform;

        public TransformType Type { get; }

        public InputFieldRef MainInput { get; }

        public AxisControl[] AxisControls { get; } = new AxisControl[3];

        public InputFieldRef IncrementInput { get; set; }
        public float Increment { get; set; } = 0.1f;

        private Vector3 CurrentValue
        {
            get
            {
                if (Type == TransformType.Position)
                {
                    return Transform.position;
                }

                if (Type == TransformType.LocalPosition)
                {
                    return Transform.localPosition;
                }

                if (Type == TransformType.Rotation)
                {
                    return Transform.localEulerAngles;
                }

                if (Type == TransformType.Scale)
                {
                    return Transform.localScale;
                }

                throw new NotImplementedException();
            }
        }

        public void Update(bool force)
        {
            var currValue = CurrentValue;
            if (force || (!MainInput.Component.isFocused && !lastValue.Equals(currValue)))
            {
                MainInput.Text = ParseUtility.ToStringForInput<Vector3>(currValue);
                lastValue = currValue;
            }
        }

        private void OnTransformInputEndEdit(TransformType type,
            string input)
        {
            switch (type)
            {
                case TransformType.Position:
                {
                    if (ParseUtility.TryParse(input, out Vector3 val, out _))
                    {
                        Target.transform.position = val;
                    }
                }

                    break;
                case TransformType.LocalPosition:
                {
                    if (ParseUtility.TryParse(input, out Vector3 val, out _))
                    {
                        Target.transform.localPosition = val;
                    }
                }

                    break;
                case TransformType.Rotation:
                {
                    if (ParseUtility.TryParse(input, out Vector3 val, out _))
                    {
                        Target.transform.localEulerAngles = val;
                    }
                }

                    break;
                case TransformType.Scale:
                {
                    if (ParseUtility.TryParse(input, out Vector3 val, out _))
                    {
                        Target.transform.localScale = val;
                    }
                }

                    break;
            }

            Owner.UpdateTransformControlValues(true);
        }

        private void IncrementInput_OnEndEdit(string value)
        {
            if (!ParseUtility.TryParse(value, out float increment, out _))
            {
                IncrementInput.Text = ParseUtility.ToStringForInput<float>(Increment);
            }
            else
            {
                Increment = increment;
                foreach (var slider in AxisControls)
                {
                    slider.slider.minValue = -increment;
                    slider.slider.maxValue = increment;
                }
            }
        }

        public static Vector3Control Create(TransformControls owner,
            GameObject transformGroup,
            string title,
            TransformType type)
        {
            var rowObj = UIFactory.CreateUIObject($"Row_{title}", transformGroup);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(rowObj, false, false, true, true, 5, 0, 0, 0, 0);
            UIFactory.SetLayoutElement(rowObj, minHeight: 25, flexibleWidth: 9999);

            var titleLabel = UIFactory.CreateLabel(rowObj, "PositionLabel", title, TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(titleLabel.gameObject, minHeight: 25, minWidth: 110);

            var inputField = UIFactory.CreateInputField(rowObj, "InputField", "...");
            UIFactory.SetLayoutElement(inputField.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 999);

            var control = new Vector3Control(owner, type, inputField);

            inputField.Component.GetOnEndEdit().AddListener(value =>
            {
                control.OnTransformInputEndEdit(type, value);
            });

            control.AxisControls[0] = AxisControl.Create(rowObj, "X", 0, control);
            control.AxisControls[1] = AxisControl.Create(rowObj, "Y", 1, control);
            control.AxisControls[2] = AxisControl.Create(rowObj, "Z", 2, control);

            control.IncrementInput = UIFactory.CreateInputField(rowObj, "IncrementInput", "...");
            control.IncrementInput.Text = "0.1";
            UIFactory.SetLayoutElement(control.IncrementInput.GameObject, 30, flexibleWidth: 0, minHeight: 25);
            control.IncrementInput.Component.GetOnEndEdit().AddListener(control.IncrementInput_OnEndEdit);

            return control;
        }
    }
}
