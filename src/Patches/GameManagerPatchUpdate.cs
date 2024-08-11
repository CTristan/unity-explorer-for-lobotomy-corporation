// SPDX-License-Identifier: MIT

using System;
using Harmony;
using JetBrains.Annotations;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.Extensions;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Loader.Standalone;

namespace UnityExplorerForLobotomyCorporation.Patches
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
