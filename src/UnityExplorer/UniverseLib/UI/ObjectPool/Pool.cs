using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityExplorerForLobotomyCorporation.UniverseLib.UI.ObjectPool
{
    /// <summary>Abstract base class to handle interfacing with a generic <see cref="Pool{T}" />, without the generic parameter at compile time.</summary>
    public abstract class Pool
    {
        protected static readonly Dictionary<Type, Pool> pools = new Dictionary<Type, Pool>();

        public static Pool GetPool(Type type)
        {
            if (!pools.TryGetValue(type, out var pool))
            {
                pool = CreatePool(type);
            }

            return pool;
        }

        protected static Pool CreatePool(Type type)
        {
            var pool = (Pool)Activator.CreateInstance(typeof(Pool<>).MakeGenericType(type));
            pools.Add(type, pool);

            return pool;
        }

        /// <summary>Borrow an object from the pool, creating a new object if none are available.</summary>
        public static IPooledObject Borrow(Type type)
        {
            return GetPool(type).DoBorrow();
        }

        /// <summary>Borrow an object from the pool, creating a new object if none are available.</summary>
        public static T Borrow<T>() where T : IPooledObject
        {
            return (T)GetPool(typeof(T)).DoBorrow();
        }

        /// <summary>Return the object to the pool.</summary>
        public static void Return(Type type,
            IPooledObject obj)
        {
            GetPool(type).DoReturn(obj);
        }

        /// <summary>Return the object to the pool.</summary>
        public static void Return<T>(T obj) where T : IPooledObject
        {
            GetPool(typeof(T)).DoReturn(obj);
        }

        protected abstract IPooledObject DoBorrow();
        protected abstract void DoReturn(IPooledObject obj);
    }

    /// <summary>Handles object pooling for all <typeparamref name="T" /> objects. Each <typeparamref name="T" /> has its own <see cref="Pool{T}" /> instance.</summary>
    public class Pool<T> : Pool where T : IPooledObject
    {
        private static Pool<T> instance;

        private readonly HashSet<T> available = new HashSet<T>();
        private readonly HashSet<T> borrowed = new HashSet<T>();

        public Pool()
        {
            instance = this;

            //UniverseLib.Log($"Creating Pool<{typeof(T).Name}>");

            InactiveHolder = new GameObject($"PoolHolder_{typeof(T).Name}");
            InactiveHolder.transform.parent = UniversalUI.PoolHolder.transform;
            InactiveHolder.hideFlags |= HideFlags.HideAndDontSave;
            InactiveHolder.SetActive(false);

            // Create an instance (just the C# class, not content) to grab the default height
            var obj = (T)Activator.CreateInstance(typeof(T));
            DefaultHeight = obj.DefaultHeight;
        }
        // Static

        public static Pool<T> Instance =>
            instance ?? (Pool<T>)CreatePool(typeof(T));

        // Instance

        /// <summary>Holds all returned objects in the pool.</summary>
        public GameObject InactiveHolder { get; }

        /// <summary>Optional default height for the object, necessary if this object can be used by a <see cref="UniverseLib.UI.Widgets.ScrollView.ScrollPool{T}" />.</summary>
        public float DefaultHeight { get; }

        /// <summary>How many objects are available in the pool.</summary>
        public int AvailableCount =>
            available.Count;

        public static Pool<T> GetPool()
        {
            return (Pool<T>)GetPool(typeof(T));
        }

        /// <summary>Borrow an object from the pool, creating a new object if none are available.</summary>
        public static T Borrow()
        {
            return Instance.BorrowObject();
        }

        /// <summary>Return the object to the pool.</summary>
        public static void Return(T obj)
        {
            Instance.ReturnObject(obj);
        }

        protected override IPooledObject DoBorrow()
        {
            return BorrowObject();
        }

        /// <summary>Borrow an object from the pool, creating a new object if none are available.</summary>
        public T BorrowObject()
        {
            if (available.Count <= 0)
            {
                IncrementPool();
            }

            var obj = available.First();
            available.Remove(obj);
            borrowed.Add(obj);

            return obj;
        }

        private void IncrementPool()
        {
            var obj = (T)Activator.CreateInstance(typeof(T));
            obj.CreateContent(InactiveHolder);
            available.Add(obj);
        }

        protected override void DoReturn(IPooledObject obj)
        {
            Return((T)obj);
        }

        /// <summary>Return the object to the pool.</summary>
        public void ReturnObject(T obj)
        {
            if (!borrowed.Contains(obj))
            {
                Universe.LogWarning($"Returning an item to object pool ({typeof(T).Name}) but the item didn't exist in the borrowed list?");
            }
            else
            {
                borrowed.Remove(obj);
            }

            available.Add(obj);
            obj.UIRoot.transform.SetParent(InactiveHolder.transform, false);
        }
    }
}
