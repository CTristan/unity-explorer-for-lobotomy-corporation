// SPDX-License-Identifier: MIT

#region

using System;

#endregion

namespace UnityExplorerHarmonyPatch.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    internal sealed class GuardClauseAttribute : Attribute
    {
    }
}
