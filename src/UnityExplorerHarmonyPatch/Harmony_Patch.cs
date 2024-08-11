// // SPDX-License-Identifier: MIT

using Harmony;
using UnityEngine;

#pragma warning disable CA1707
namespace UnityExplorerHarmonyPatch
{
    // ReSharper disable once InconsistentNaming
    public sealed class Harmony_Patch
    {
        public Harmony_Patch()
        {
            Debug.Log("Starting Unity Explorer patching...");
            var harmony = HarmonyInstance.Create("UnityExplorerHarmonyPatch.dll");
            harmony.PatchAll(typeof(Harmony_Patch).Assembly);
            Debug.Log("Unity Explorer patching complete.");
        }
    }
}
