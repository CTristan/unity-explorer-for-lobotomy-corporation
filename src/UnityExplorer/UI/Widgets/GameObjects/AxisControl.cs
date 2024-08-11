using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.GameObjects
{
    // Handles the slider and +/- buttons for a specific axis of a transform property.

    public class AxisControl
    {
        public readonly int axis;
        public readonly Vector3Control parent;
        public readonly Slider slider;

        public AxisControl(int axis,
            Slider slider,
            Vector3Control parentControl)
        {
            parent = parentControl;
            this.axis = axis;
            this.slider = slider;
        }

        private void OnVectorSliderChanged(float value)
        {
            parent.Owner.CurrentSlidingAxisControl = value == 0f ? null : this;
        }

        private void OnVectorMinusClicked()
        {
            parent.Owner.AxisControlOperation(-parent.Increment, parent, axis);
        }

        private void OnVectorPlusClicked()
        {
            parent.Owner.AxisControlOperation(parent.Increment, parent, axis);
        }

        public static AxisControl Create(GameObject parent,
            string title,
            int axis,
            Vector3Control owner)
        {
            var label = UIFactory.CreateLabel(parent, $"Label_{title}", $"{title}:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(label.gameObject, minHeight: 25, minWidth: 30);

            var sliderObj = UIFactory.CreateSlider(parent, $"Slider_{title}", out var slider);
            UIFactory.SetLayoutElement(sliderObj, minHeight: 25, minWidth: 75, flexibleWidth: 0);
            slider.m_FillImage.color = Color.clear;

            slider.minValue = -0.1f;
            slider.maxValue = 0.1f;

            var sliderControl = new AxisControl(axis, slider, owner);

            slider.onValueChanged.AddListener(sliderControl.OnVectorSliderChanged);

            var minusButton = UIFactory.CreateButton(parent, "MinusIncrementButton", "-");
            UIFactory.SetLayoutElement(minusButton.GameObject, 20, flexibleWidth: 0, minHeight: 25);
            minusButton.OnClick += sliderControl.OnVectorMinusClicked;

            var plusButton = UIFactory.CreateButton(parent, "PlusIncrementButton", "+");
            UIFactory.SetLayoutElement(plusButton.GameObject, 20, flexibleWidth: 0, minHeight: 25);
            plusButton.OnClick += sliderControl.OnVectorPlusClicked;

            return sliderControl;
        }
    }
}
