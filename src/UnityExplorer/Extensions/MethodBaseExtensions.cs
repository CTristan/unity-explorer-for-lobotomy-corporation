// // SPDX-License-Identifier: MIT

using System.Linq;
using System.Reflection;
using System.Text;
using Harmony;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.Extensions
{
    public static class MethodBaseExtensions
    {
        /// <summary>A a full description of a method or a constructor without assembly details but with generics</summary>
        /// <param name="member">The method/constructor</param>
        /// <returns>A human readable description</returns>
        public static string FullDescription(this MethodBase member)
        {
            if (member is null)
            {
                return "null";
            }

            var returnType = AccessTools.GetReturnedType(member);

            var result = new StringBuilder();
            if (member.IsStatic)
            {
                _ = result.Append("static ");
            }

            if (member.IsAbstract)
            {
                _ = result.Append("abstract ");
            }

            if (member.IsVirtual)
            {
                _ = result.Append("virtual ");
            }

            _ = result.Append($"{returnType.FullDescription()} ");
            if (member.DeclaringType != null)
            {
                _ = result.Append($"{member.DeclaringType.FullDescription()}::");
            }

            var parameters = member.GetParameters();
            var parameterDescriptions = parameters.Select(p => $"{p.ParameterType.FullDescription()} {p.Name}").ToArray();
            var parameterString = string.Join(", ", parameterDescriptions);
            _ = result.Append($"{member.Name}({parameterString})");

            return result.ToString();
        }
    }
}
