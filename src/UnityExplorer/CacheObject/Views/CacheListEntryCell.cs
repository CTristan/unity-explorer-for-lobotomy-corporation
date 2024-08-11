using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.IValues;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject.Views
{
    public class CacheListEntryCell : CacheObjectCell
    {
        public static Color EvenColor = new Color(0.12f, 0.12f, 0.12f);
        public static Color OddColor = new Color(0.1f, 0.1f, 0.1f);
        public Image Image { get; private set; }

        public InteractiveList ListOwner =>
            Occupant.Owner as InteractiveList;

        public override GameObject CreateContent(GameObject parent)
        {
            var root = base.CreateContent(parent);

            Image = root.AddComponent<Image>();

            NameLayout.minWidth = 40;
            NameLayout.flexibleWidth = 50;
            NameLayout.minHeight = 25;
            NameLayout.flexibleHeight = 0;
            NameLabel.alignment = TextAnchor.MiddleRight;

            return root;
        }

        protected override void ConstructEvaluateHolder(GameObject parent)
        {
            // not used
        }

        //protected override void ConstructUpdateToggle(GameObject parent)
        //{
        //    // not used
        //}
    }
}
