// SPDX-License-Identifier: MIT

#region

using System;

#endregion

namespace UnityExplorerForLobotomyCorporation.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ValidatedNotNullAttribute : Attribute
    {
    }
}
