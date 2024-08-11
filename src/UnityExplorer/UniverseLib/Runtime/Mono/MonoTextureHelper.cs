#if MONO
using System;
using System.Collections;
using System.Reflection;
using Harmony;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UniverseLib.Runtime.Mono
{
    internal class MonoTextureHelper : TextureHelper
    {
        private static MethodInfo mi_EncodeToPNG;
        private static MethodInfo mi_Graphics_CopyTexture;

        internal MonoTextureHelper()
        {
            RuntimeHelper.StartCoroutine(InitCoro());
        }

        internal override bool Internal_CanForceReadCubemaps =>
            mi_Graphics_CopyTexture != null;

        private static IEnumerator InitCoro()
        {
            while (ReflectionUtility.Initializing)
            {
                yield return null;
            }

            mi_Graphics_CopyTexture = AccessTools.Method(typeof(Graphics), "CopyTexture", new[]
            {
                typeof(Texture), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(Texture), typeof(int), typeof(int), typeof(int), typeof(int),
            });

            if (ReflectionUtility.GetTypeByName("UnityEngine.ImageConversion") is Type imageConversion)
            {
                mi_EncodeToPNG = imageConversion.GetMethod("EncodeToPNG", ReflectionUtility.FLAGS);
            }
            else
            {
                mi_EncodeToPNG = typeof(Texture2D).GetMethod("EncodeToPNG", ReflectionUtility.FLAGS);
            }
        }

        protected internal override void Internal_Blit(Texture tex,
            RenderTexture rt)
        {
            Graphics.Blit(tex, rt);
        }

        protected internal override Sprite Internal_CreateSprite(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }

        protected internal override Sprite Internal_CreateSprite(Texture2D texture,
            Rect rect,
            Vector2 pivot,
            float pixelsPerUnit,
            uint extrude,
            Vector4 border)
        {
            return Sprite.Create(texture, rect, pivot, pixelsPerUnit, extrude, SpriteMeshType.Tight, border);
        }

        protected internal override Texture2D Internal_NewTexture2D(int width,
            int height)
        {
            return new Texture2D(width, height);
        }

        protected internal override Texture2D Internal_NewTexture2D(int width,
            int height,
            TextureFormat textureFormat,
            bool mipChain)
        {
            return new Texture2D(width, height, textureFormat, mipChain);
        }

        protected internal override byte[] Internal_EncodeToPNG(Texture2D tex)
        {
            if (mi_EncodeToPNG == null)
            {
                throw new MissingMethodException("Could not find any Texture2D EncodeToPNG method!");
            }

            return mi_EncodeToPNG.IsStatic
                ? (byte[])mi_EncodeToPNG.Invoke(null, new object[]
                {
                    tex,
                })
                : (byte[])mi_EncodeToPNG.Invoke(tex, ArgumentUtility.EmptyArgs);
        }

        internal override Texture Internal_CopyTexture(Texture src,
            int srcElement,
            int srcMip,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            Texture dst,
            int dstElement,
            int dstMip,
            int dstX,
            int dstY)
        {
            if (mi_Graphics_CopyTexture == null)
            {
                throw new MissingMethodException("This game does not ship with the required method 'Graphics.CopyTexture'.");
            }

            mi_Graphics_CopyTexture.Invoke(null, new object[]
            {
                src, srcElement, srcMip, srcX, srcY, srcWidth, srcHeight, dst, dstElement, dstMip, dstX, dstY,
            });

            return dst;
        }
    }
}
#endif
