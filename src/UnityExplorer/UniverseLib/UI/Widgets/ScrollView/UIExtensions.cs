using UnityEngine;

namespace UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView
{
    public static class UIExtension
    {
        public static void GetCorners(this RectTransform rect,
            Vector3[] corners)
        {
            var bottomLeft = new Vector3(rect.position.x, rect.position.y - rect.rect.height, 0);

            corners[0] = bottomLeft;
            corners[1] = bottomLeft + new Vector3(0, rect.rect.height, 0);
            corners[2] = bottomLeft + new Vector3(rect.rect.width, rect.rect.height, 0);
            corners[3] = bottomLeft + new Vector3(rect.rect.width, 0, 0);
        }

        // again, using position and rect instead of

        public static float MaxY(this RectTransform rect)
        {
            return rect.position.y - rect.rect.height;
        }

        public static float MinY(this RectTransform rect)
        {
            return rect.position.y;
        }

        public static float MaxX(this RectTransform rect)
        {
            return rect.position.x + rect.rect.width;
        }

        public static float MinX(this RectTransform rect)
        {
            return rect.position.x;
        }
    }
}
