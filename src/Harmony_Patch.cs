// // SPDX-License-Identifier: MIT

using System.Reflection;
using Harmony;

namespace UnityExplorerForLobotomyCorporation
{
    // ReSharper disable once InconsistentNaming
    public sealed class Harmony_Patch
    {
        public static readonly Harmony_Patch Instance = new Harmony_Patch();

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        // https://jonskeet.uk/csharp/singleton.html
        static Harmony_Patch()
        {
            var harmony = HarmonyInstance.Create(nameof(UnityExplorerForLobotomyCorporation));
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
