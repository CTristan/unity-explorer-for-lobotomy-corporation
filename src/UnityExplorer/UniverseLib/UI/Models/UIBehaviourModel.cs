using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models
{
    /// <summary>A class which can be used as an abstract UI object, which does not exist as a Component but which can receive Update calls.</summary>
    public abstract class UIBehaviourModel : UIModel
    {
        // Static
        private static readonly List<UIBehaviourModel> Instances = new List<UIBehaviourModel>();

        // Instance

        public UIBehaviourModel()
        {
            Instances.Add(this);
        }

        internal static void UpdateInstances()
        {
            if (!Instances.Any())
            {
                return;
            }

            try
            {
                for (var i = Instances.Count - 1; i >= 0; i--)
                {
                    var instance = Instances[i];
                    if (instance == null || !instance.UIRoot)
                    {
                        Instances.RemoveAt(i);

                        continue;
                    }

                    if (instance.Enabled)
                    {
                        instance.Update();
                    }
                }
            }
            catch (Exception ex)
            {
                Universe.Log(ex);
            }
        }

        public virtual void Update()
        {
        }

        public override void Destroy()
        {
            if (Instances.Contains(this))
            {
                Instances.Remove(this);
            }

            base.Destroy();
        }
    }
}
