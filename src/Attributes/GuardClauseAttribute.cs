// SPDX-License-Identifier: MIT

#region

using System;

#endregion

namespace UnityExplorerForLobotomyCorporation.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    internal sealed class GuardClauseAttribute : Attribute
    {
    }
}
