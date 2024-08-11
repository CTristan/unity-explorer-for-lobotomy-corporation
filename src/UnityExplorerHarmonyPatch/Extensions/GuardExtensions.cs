// SPDX-License-Identifier: MIT

using System;
using JetBrains.Annotations;
using UnityExplorerHarmonyPatch.Attributes;

namespace UnityExplorerHarmonyPatch.Extensions
{
    public static class GuardExtensions
    {
        [NotNull]
        public static T Null<T>([NotNull] [GuardClause] this Guard guardClause,
            [NotNull] [ValidatedNotNull] [NoEnumeration]
            T input,
            [NotNull] [InvokerParameterName] string parameterName) where T : class
        {
            if (input == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return input;
        }
    }

    public sealed class Guard
    {
        private Guard()
        {
        }

        [GuardClause] public static Guard Against { get; } = new Guard();
    }
}
