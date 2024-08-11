using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public class MaterialWidget : UnityObjectWidget
    {
        private static readonly MethodInfo mi_GetTexturePropertyNames;
        private readonly Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
        private readonly HashSet<Texture2D> texturesToDestroy = new HashSet<Texture2D>();
        private Texture2D activeTexture;
        private Image image;
        private LayoutElement imageLayout;

        private Material material;
        private InputFieldRef savePathInput;
        private Dropdown textureDropdown;

        private GameObject textureViewerRoot;

        private bool textureViewerWanted;
        private ButtonRef toggleButton;

        static MaterialWidget()
        {
            mi_GetTexturePropertyNames = typeof(Material).GetMethod("GetTexturePropertyNames", ArgumentUtility.EmptyTypes);
            MaterialWidgetSupported = mi_GetTexturePropertyNames != null;
        }

        internal static bool MaterialWidgetSupported { get; }

        public override void OnBorrowed(object target,
            Type targetType,
            ReflectionInspector inspector)
        {
            base.OnBorrowed(target, targetType, inspector);

            material = target.TryCast<Material>();

            if (material.mainTexture)
            {
                SetActiveTexture(material.mainTexture);
            }

            if (mi_GetTexturePropertyNames.Invoke(material, ArgumentUtility.EmptyArgs) is IEnumerable<string> propNames)
            {
                foreach (var property in propNames)
                {
                    if (material.GetTexture(property) is Texture texture)
                    {
                        if (texture.TryCast<Texture2D>() is null && texture.TryCast<Cubemap>() is null)
                        {
                            continue;
                        }

                        textures.Add(property, texture);

                        if (!activeTexture)
                        {
                            SetActiveTexture(texture);
                        }
                    }
                }
            }

            if (textureViewerRoot)
            {
                textureViewerRoot.transform.SetParent(inspector.UIRoot.transform);
                RefreshTextureDropdown();
            }

            InspectorPanel.Instance.Dragger.OnFinishResize += OnInspectorFinishResize;
        }

        private void SetActiveTexture(Texture texture)
        {
            if (texture.TryCast<Texture2D>() is Texture2D tex2D)
            {
                activeTexture = tex2D;
            }
            else if (texture.TryCast<Cubemap>() is Cubemap cubemap)
            {
                activeTexture = TextureHelper.UnwrapCubemap(cubemap);
                texturesToDestroy.Add(activeTexture);
            }
        }

        public override void OnReturnToPool()
        {
            InspectorPanel.Instance.Dragger.OnFinishResize -= OnInspectorFinishResize;

            if (texturesToDestroy.Any())
            {
                foreach (var tex in texturesToDestroy)
                {
                    Object.Destroy(tex);
                }

                texturesToDestroy.Clear();
            }

            material = null;
            activeTexture = null;
            textures.Clear();

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
                toggleButton.ButtonText.text = "View Material";

                owner.ContentRoot.SetActive(true);
            }
            else
            {
                // enable

                if (!image.sprite)
                {
                    RefreshTextureViewer();
                    RefreshTextureDropdown();
                }

                SetImageSize();

                textureViewerWanted = true;
                textureViewerRoot.SetActive(true);
                toggleButton.ButtonText.text = "Hide Material";

                owner.ContentRoot.gameObject.SetActive(false);
            }
        }

        private void RefreshTextureViewer()
        {
            if (!activeTexture)
            {
                ExplorerCore.LogWarning("Material has no active textures!");
                savePathInput.Text = string.Empty;

                return;
            }

            if (image.sprite)
            {
                Object.Destroy(image.sprite);
            }

            var name = activeTexture.name;
            if (string.IsNullOrEmpty(name))
            {
                name = "untitled";
            }

            savePathInput.Text = Path.Combine(ConfigManager.Default_Output_Path.Value, $"{name}.png");

            var sprite = TextureHelper.CreateSprite(activeTexture);
            image.sprite = sprite;
        }

        private void RefreshTextureDropdown()
        {
            if (!textureDropdown)
            {
                return;
            }

            textureDropdown.options.Clear();

            foreach (var key in textures.Keys)
            {
                textureDropdown.options.Add(new Dropdown.OptionData(key));
            }

            var i = 0;
            foreach (var value in textures.Values)
            {
                if (activeTexture.ReferenceEqual(value))
                {
                    textureDropdown.value = i;

                    break;
                }

                i++;
            }

            textureDropdown.RefreshShownValue();
        }

        private void OnTextureDropdownChanged(int value)
        {
            var tex = textures.ElementAt(value).Value;
            if (activeTexture.ReferenceEqual(tex))
            {
                return;
            }

            SetActiveTexture(tex);
            RefreshTextureViewer();
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
            if (!activeTexture)
            {
                yield break;
            }

            // let unity rebuild layout etc
            yield return null;

            var imageRect = InspectorPanel.Instance.Rect;

            var rectWidth = imageRect.rect.width - 25;
            var rectHeight = imageRect.rect.height - 196;

            // If our image is smaller than the viewport, just use 100% scaling
            if (activeTexture.width < rectWidth && activeTexture.height < rectHeight)
            {
                imageLayout.minWidth = activeTexture.width;
                imageLayout.minHeight = activeTexture.height;
            }
            else // we will need to scale down the image to fit
            {
                // get the ratio of our viewport dimensions to width and height
                var viewWidthRatio = (float)((decimal)rectWidth / activeTexture.width);
                var viewHeightRatio = (float)((decimal)rectHeight / activeTexture.height);

                // if width needs to be scaled more than height
                if (viewWidthRatio < viewHeightRatio)
                {
                    imageLayout.minWidth = activeTexture.width * viewWidthRatio;
                    imageLayout.minHeight = activeTexture.height * viewWidthRatio;
                }
                else // if height needs to be scaled more than width
                {
                    imageLayout.minWidth = activeTexture.width * viewHeightRatio;
                    imageLayout.minHeight = activeTexture.height * viewHeightRatio;
                }
            }
        }

        private void OnSaveTextureClicked()
        {
            if (!activeTexture)
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

            TextureHelper.SaveTextureAsPNG(activeTexture, path);
        }

        public override GameObject CreateContent(GameObject uiRoot)
        {
            var ret = base.CreateContent(uiRoot);

            // Button

            toggleButton = UIFactory.CreateButton(UIRoot, "MaterialButton", "View Material", new Color(0.2f, 0.3f, 0.2f));
            toggleButton.Transform.SetSiblingIndex(0);
            UIFactory.SetLayoutElement(toggleButton.Component.gameObject, minHeight: 25, minWidth: 150);
            toggleButton.OnClick += ToggleTextureViewer;

            // Texture viewer

            textureViewerRoot = UIFactory.CreateVerticalGroup(uiRoot, "MaterialViewer", false, false, true, true, 2, new Vector4(5, 5, 5, 5), new Color(0.1f, 0.1f, 0.1f), TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(textureViewerRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            // Buttons holder

            var dropdownRow = UIFactory.CreateHorizontalGroup(textureViewerRoot, "DropdownRow", false, true, true, true, 5, new Vector4(3, 3, 3, 3));
            UIFactory.SetLayoutElement(dropdownRow, minHeight: 30, flexibleWidth: 9999);

            var dropdownLabel = UIFactory.CreateLabel(dropdownRow, "DropdownLabel", "Texture:");
            UIFactory.SetLayoutElement(dropdownLabel.gameObject, 75, 25);

            var dropdownObj = UIFactory.CreateDropdown(dropdownRow, "TextureDropdown", out textureDropdown, "NOT SET", 13, OnTextureDropdownChanged);
            UIFactory.SetLayoutElement(dropdownObj, 350, 25);

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
