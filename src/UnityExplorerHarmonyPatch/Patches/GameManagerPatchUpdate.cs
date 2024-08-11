// SPDX-License-Identifier: MIT

using System;
using Harmony;
using JetBrains.Annotations;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Loader.Standalone;
using UnityExplorerHarmonyPatch.Extensions;

#pragma warning disable CA1707
namespace UnityExplorerHarmonyPatch.Patches
{
    [HarmonyPatch(typeof(GameManager), nameof(PrivateMethods.GameManager.Update))]
    public static class GameManagerPatchUpdate
    {
        private static bool _initialLoad = true;

        private static void PatchAfterAwake([NotNull] this GameManager instance)
        {
            Guard.Against.Null(instance, nameof(instance));

            if (!Input.GetKeyDown(KeyCode.F7))
            {
                return;
            }

            Debug.Log("Creating Unity Explorer standalone instance...");
            ExplorerStandalone.CreateInstance();
            _initialLoad = false;
        }

        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming
        public static void Postfix(GameManager __instance)
        {
            try
            {
                if (!_initialLoad)
                {
                    return;
                }

                __instance.PatchAfterAwake();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                throw;
            }
        }
    }
}
