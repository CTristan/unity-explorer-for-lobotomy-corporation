using System;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UniverseLib.Input;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.GameObjects
{
    // Handles axis change operations and holds references to the Vector3Controls for each transform property

    public class TransformControls
    {
        public GameObjectControls Owner { get; }
        GameObject Target => Owner.Target;

        public AxisControl CurrentSlidingAxisControl { get; set; }

        Vector3Control PositionControl;
        Vector3Control LocalPositionControl;
        Vector3Control RotationControl;
        Vector3Control ScaleControl;

        public TransformControls(GameObjectControls owner)
        {
            this.Owner = owner;
            Create();
        }

        public void UpdateTransformControlValues(bool force)
        {
            PositionControl.Update(force);
            LocalPositionControl.Update(force);
            RotationControl.Update(force);
            ScaleControl.Update(force);
        }

        public void UpdateVectorSlider()
        {
            AxisControl control = CurrentSlidingAxisControl;

            if (control == null)
                return;

            if (!InputManager.GetMouseButton(0))
            {
                control.slider.value = 0f;
                control = null;
                return;
            }

            AxisControlOperation(control.slider.value, control.parent, control.axis);
        }

        public void AxisControlOperation(float value, Vector3Control parent, int axis)
        {
            Transform transform = Target.transform;

            Vector3 vector;
            if (parent.Type == TransformType.Position)
            {
                vector = transform.position;
            }
            else if (parent.Type == TransformType.LocalPosition)
            {
                vector = transform.localPosition;
            }
            else if (parent.Type == TransformType.Rotation)
            {
                vector = transform.localEulerAngles;
            }
            else if (parent.Type == TransformType.Scale)
            {
                vector = transform.localScale;
            }
            else
            {
                throw new NotImplementedException();
            }

            // apply vector value change
            switch (axis)
            {
                case 0:
                    vector.x += value; break;
                case 1:
                    vector.y += value; break;
                case 2:
                    vector.z += value; break;
            }

            // set vector back to transform
            switch (parent.Type)
            {
                case TransformType.Position:
                    transform.position = vector; break;
                case TransformType.LocalPosition:
                    transform.localPosition = vector; break;
                case TransformType.Rotation:
                    transform.localEulerAngles = vector; break;
                case TransformType.Scale:
                    transform.localScale = vector; break;
            }

            UpdateTransformControlValues(false);
        }

        public void Create()
        {
            GameObject transformGroup = UIFactory.CreateVerticalGroup(Owner.Parent.Content, "TransformControls", false, false, true, true, 2,
                new Vector4(2, 2, 0, 0), new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(transformGroup, minHeight: 100, flexibleWidth: 9999);

            PositionControl = Vector3Control.Create(this, transformGroup, "Position:", TransformType.Position);
            LocalPositionControl = Vector3Control.Create(this, transformGroup, "Local Position:", TransformType.LocalPosition);
            RotationControl = Vector3Control.Create(this, transformGroup, "Rotation:", TransformType.Rotation);
            ScaleControl = Vector3Control.Create(this, transformGroup, "Scale:", TransformType.Scale);
        }
    }
}
