using System;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.ObjectPool;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.IValues
{
    public abstract class InteractiveValue : IPooledObject
    {
        public bool PendingValueWanted;

        public virtual bool CanWrite =>
            CurrentOwner.CanWrite;

        public CacheObjectBase CurrentOwner { get; private set; }

        public GameObject UIRoot { get; set; }

        public float DefaultHeight =>
            -1f;

        public abstract GameObject CreateContent(GameObject parent);

        public static Type GetIValueTypeForState(ValueState state)
        {
            if (state is ValueState.Exception || state is ValueState.String)
            {
                return typeof(InteractiveString);
            }

            if (state == ValueState.Enum)
            {
                return typeof(InteractiveEnum);
            }

            if (state == ValueState.Collection)
            {
                return typeof(InteractiveList);
            }

            if (state == ValueState.Dictionary)
            {
                return typeof(InteractiveDictionary);
            }

            if (state == ValueState.ValueStruct)
            {
                return typeof(InteractiveValueStruct);
            }

            if (state == ValueState.Color)
            {
                return typeof(InteractiveColor);
            }

            return null;
        }

        public virtual void OnBorrowed(CacheObjectBase owner)
        {
            if (CurrentOwner != null)
            {
                ExplorerCore.LogWarning("Setting an IValue's owner but there is already one set. Maybe it wasn't cleaned up?");
                ReleaseFromOwner();
            }

            CurrentOwner = owner;
        }

        public virtual void ReleaseFromOwner()
        {
            if (CurrentOwner == null)
            {
                return;
            }

            CurrentOwner = null;
        }

        public abstract void SetValue(object value);

        public virtual void SetLayout()
        {
        }
    }
}
