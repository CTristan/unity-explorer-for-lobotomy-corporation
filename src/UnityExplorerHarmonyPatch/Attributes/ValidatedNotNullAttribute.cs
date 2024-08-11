// SPDX-License-Identifier: MIT

#region

using System;

#endregion

namespace UnityExplorerHarmonyPatch.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ValidatedNotNullAttribute : Attribute
    {
    }
}
