using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Config;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels;
using UnityExplorerForLobotomyCorporation.UniverseLib;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.Runtime;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.ObjectPool;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;
using Object = UnityEngine.Object;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.UnityObjects
{
    public class Texture2DWidget : UnityObjectWidget
    {
        private Image image;
        private LayoutElement imageLayout;
        private InputFieldRef savePathInput;
        private bool shouldDestroyTexture;
        private Texture2D texture;

        private GameObject textureViewerRoot;

        private bool textureViewerWanted;
        private ButtonRef toggleButton;

        public override void OnBorrowed(object target,
            Type targetType,
            ReflectionInspector inspector)
        {
            base.OnBorrowed(target, targetType, inspector);

            if (target.TryCast<Cubemap>() is Cubemap cubemap)
            {
                texture = TextureHelper.UnwrapCubemap(cubemap);
                shouldDestroyTexture = true;
            }
            else if (target.TryCast<Sprite>() is Sprite sprite)
            {
                if (sprite.packingMode == SpritePackingMode.Tight)
                {
                    texture = sprite.texture;
                }
                else
                {
                    texture = TextureHelper.CopyTexture(sprite.texture, sprite.textureRect);
                    shouldDestroyTexture = true;
                }
            }
            else if (target.TryCast<Image>() is Image image)
            {
                if (image.sprite.packingMode == SpritePackingMode.Tight)
                {
                    texture = image.sprite.texture;
                }
                else
                {
                    texture = TextureHelper.CopyTexture(image.sprite.texture, image.sprite.textureRect);
                    shouldDestroyTexture = true;
                }
            }
            else
            {
                texture = target.TryCast<Texture2D>();
            }

            if (textureViewerRoot)
            {
                textureViewerRoot.transform.SetParent(inspector.UIRoot.transform);
            }

            InspectorPanel.Instance.Dragger.OnFinishResize += OnInspectorFinishResize;
        }

        public override void OnReturnToPool()
        {
            InspectorPanel.Instance.Dragger.OnFinishResize -= OnInspectorFinishResize;

            if (shouldDestroyTexture)
            {
                Object.Destroy(texture);
            }

            texture = null;
            shouldDestroyTexture = false;

            if (image.sprite)
            {
                Object.Destroy(image.sprite);
            }

            if (textureViewerWanted)
            {
                ToggleTextureViewer();
            }

            if (textureViewerRoot)
            {
                textureViewerRoot.transform.SetParent(Pool<Texture2DWidget>.Instance.InactiveHolder.transform);
            }

            base.OnReturnToPool();
        }

        private void ToggleTextureViewer()
        {
            if (textureViewerWanted)
            {
                // disable
                textureViewerWanted = false;
                textureViewerRoot.SetActive(false);
                toggleButton.ButtonText.text = "View Texture";

                owner.ContentRoot.SetActive(true);
            }
            else
            {
                // enable
                if (!image.sprite)
                {
                    SetupTextureViewer();
                }

                SetImageSize();

                textureViewerWanted = true;
                textureViewerRoot.SetActive(true);
                toggleButton.ButtonText.text = "Hide Texture";

                owner.ContentRoot.gameObject.SetActive(false);
            }
        }

        private void SetupTextureViewer()
        {
            if (!texture)
            {
                return;
            }

            var name = texture.name;
            if (string.IsNullOrEmpty(name))
            {
                name = "untitled";
            }

            savePathInput.Text = Path.Combine(ConfigManager.Default_Output_Path.Value, $"{name}.png");

            var sprite = TextureHelper.CreateSprite(texture);
            image.sprite = sprite;
        }

        private void OnInspectorFinishResize()
        {
            SetImageSize();
        }

        private void SetImageSize()
        {
            if (!imageLayout)
            {
                return;
            }

            RuntimeHelper.StartCoroutine(SetImageSizeCoro());
        }

        private IEnumerator SetImageSizeCoro()
        {
            // let unity rebuild layout etc
            yield return null;

            var imageRect = InspectorPanel.Instance.Rect;

            var rectWidth = imageRect.rect.width - 25;
            var rectHeight = imageRect.rect.height - 196;

            // If our image is smaller than the viewport, just use 100% scaling
            if (texture.width < rectWidth && texture.height < rectHeight)
            {
                imageLayout.minWidth = texture.width;
                imageLayout.minHeight = texture.height;
            }
            else // we will need to scale down the image to fit
            {
                // get the ratio of our viewport dimensions to width and height
                var viewWidthRatio = (float)((decimal)rectWidth / texture.width);
                var viewHeightRatio = (float)((decimal)rectHeight / texture.height);

                // if width needs to be scaled more than height
                if (viewWidthRatio < viewHeightRatio)
                {
                    imageLayout.minWidth = texture.width * viewWidthRatio;
                    imageLayout.minHeight = texture.height * viewWidthRatio;
                }
                else // if height needs to be scaled more than width
                {
                    imageLayout.minWidth = texture.width * viewHeightRatio;
                    imageLayout.minHeight = texture.height * viewHeightRatio;
                }
            }
        }

        private void OnSaveTextureClicked()
        {
            if (!texture)
            {
                ExplorerCore.LogWarning("Texture is null, maybe it was destroyed?");

                return;
            }

            if (string.IsNullOrEmpty(savePathInput.Text))
            {
                ExplorerCore.LogWarning("Save path cannot be empty!");

                return;
            }

            var path = savePathInput.Text;
            if (!path.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
            {
                path += ".png";
            }

            path = IOUtility.EnsureValidFilePath(path);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            TextureHelper.SaveTextureAsPNG(texture, path);
        }

        public override GameObject CreateContent(GameObject uiRoot)
        {
            var ret = base.CreateContent(uiRoot);

            // Button

            toggleButton = UIFactory.CreateButton(UIRoot, "TextureButton", "View Texture", new Color(0.2f, 0.3f, 0.2f));
            toggleButton.Transform.SetSiblingIndex(0);
            UIFactory.SetLayoutElement(toggleButton.Component.gameObject, minHeight: 25, minWidth: 150);
            toggleButton.OnClick += ToggleTextureViewer;

            // Texture viewer

            textureViewerRoot = UIFactory.CreateVerticalGroup(uiRoot, "TextureViewer", false, false, true, true, 2, new Vector4(5, 5, 5, 5), new Color(0.1f, 0.1f, 0.1f), TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(textureViewerRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            // Save helper

            var saveRowObj = UIFactory.CreateHorizontalGroup(textureViewerRoot, "SaveRow", false, false, true, true, 2, new Vector4(2, 2, 2, 2), new Color(0.1f, 0.1f, 0.1f));

            var saveBtn = UIFactory.CreateButton(saveRowObj, "SaveButton", "Save .PNG", new Color(0.2f, 0.25f, 0.2f));
            UIFactory.SetLayoutElement(saveBtn.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            saveBtn.OnClick += OnSaveTextureClicked;

            savePathInput = UIFactory.CreateInputField(saveRowObj, "SaveInput", "...");
            UIFactory.SetLayoutElement(savePathInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 9999);

            // Actual texture viewer

            var imageViewport = UIFactory.CreateVerticalGroup(textureViewerRoot, "ImageViewport", false, false, true, true, bgColor: new Color(1, 1, 1, 0), childAlignment: TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(imageViewport, flexibleWidth: 9999, flexibleHeight: 9999);

            var imageHolder = UIFactory.CreateUIObject("ImageHolder", imageViewport);
            imageLayout = UIFactory.SetLayoutElement(imageHolder, 1, 1, 0, 0);

            var actualImageObj = UIFactory.CreateUIObject("ActualImage", imageHolder);
            var actualRect = actualImageObj.GetComponent<RectTransform>();
            actualRect.anchorMin = new Vector2(0, 0);
            actualRect.anchorMax = new Vector2(1, 1);
            image = actualImageObj.AddComponent<Image>();

            textureViewerRoot.SetActive(false);

            return ret;
        }
    }
}
