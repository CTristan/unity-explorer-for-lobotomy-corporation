// // SPDX-License-Identifier: MIT

using System;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>A full description of a type</summary>
        /// <param name="type">The type</param>
        /// <returns>A human readable description</returns>
        public static string FullDescription(this Type type)
        {
            if (type is null)
            {
                return "null";
            }

            var ns = type.Namespace;
            if (string.IsNullOrEmpty(ns) is false)
            {
                ns += ".";
            }

            var result = ns + type.Name;

            if (type.IsGenericType)
            {
                result += "<";
                var subTypes = type.GetGenericArguments();
                for (var i = 0; i < subTypes.Length; i++)
                {
                    if (result
#if NET8_0_OR_GREATER
					.EndsWith('<')
#else
                            .EndsWith("<", StringComparison.Ordinal)
#endif
                        is false)
                    {
                        result += ", ";
                    }

                    result += subTypes[i].FullDescription();
                }

                result += ">";
            }

            return result;
        }
    }
}
