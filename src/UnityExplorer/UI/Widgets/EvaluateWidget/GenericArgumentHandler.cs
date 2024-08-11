using System;
using System.Text;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets.EvaluateWidget
{
    public class GenericArgumentHandler : BaseArgumentHandler
    {
        private Type genericArgument;

        public void OnBorrowed(Type genericArgument)
        {
            this.genericArgument = genericArgument;

            typeCompleter.Enabled = true;
            typeCompleter.BaseType = this.genericArgument;
            typeCompleter.CacheTypes();

            var constraints = this.genericArgument.GetGenericParameterConstraints();

            var sb = new StringBuilder($"<color={SignatureHighlighter.CONST}>{this.genericArgument.Name}</color>");

            for (var i = 0; i < constraints.Length; i++)
            {
                if (i == 0)
                {
                    sb.Append(' ').Append('(');
                }
                else
                {
                    sb.Append(',').Append(' ');
                }

                sb.Append(SignatureHighlighter.Parse(constraints[i], false));

                if (i + 1 == constraints.Length)
                {
                    sb.Append(')');
                }
            }

            argNameLabel.text = sb.ToString();
        }

        public void OnReturned()
        {
            genericArgument = null;

            typeCompleter.Enabled = false;

            inputField.Text = "";
        }

        public Type Evaluate()
        {
            return ReflectionUtility.GetTypeByName(inputField.Text) ?? throw new Exception($"Could not find any type by name '{inputField.Text}'!");
        }

        public override void CreateSpecialContent()
        {
        }
    }
}
