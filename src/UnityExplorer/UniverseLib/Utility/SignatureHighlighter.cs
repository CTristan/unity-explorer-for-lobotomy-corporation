using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Extensions;

namespace UnityExplorerForLobotomyCorporation.UniverseLib.Utility
{
    /// <summary>For Unity rich-text syntax highlighting of Types and Members.</summary>
    public static class SignatureHighlighter
    {
        public const string NAMESPACE = "#a8a8a8";

        public const string CONST = "#92c470";

        public const string CLASS_STATIC = "#3a8d71";
        public const string CLASS_INSTANCE = "#2df7b2";

        public const string STRUCT = "#0fba3a";
        public const string INTERFACE = "#9b9b82";

        public const string FIELD_STATIC = "#8d8dc6";
        public const string FIELD_INSTANCE = "#c266ff";

        public const string METHOD_STATIC = "#b55b02";
        public const string METHOD_INSTANCE = "#ff8000";

        public const string PROP_STATIC = "#588075";
        public const string PROP_INSTANCE = "#55a38e";

        public const string LOCAL_ARG = "#a6e9e9";

        public const string OPEN_COLOR = "<color=";
        public const string CLOSE_COLOR = "</color>";
        public const string OPEN_ITALIC = "<i>";
        public const string CLOSE_ITALIC = "</i>";

        public static readonly Regex ArrayTokenRegex = new Regex(@"\[,*?\]");

        private static readonly Regex colorTagRegex = new Regex(@"<color=#?[\d|\w]*>");

        public static readonly Color StringOrange = new Color(0.83f, 0.61f, 0.52f);
        public static readonly Color EnumGreen = new Color(0.57f, 0.76f, 0.43f);
        public static readonly Color KeywordBlue = new Color(0.3f, 0.61f, 0.83f);
        public static readonly string keywordBlueHex = KeywordBlue.ToHex();
        public static readonly Color NumberGreen = new Color(0.71f, 0.8f, 0.65f);

        private static readonly Dictionary<string, string> typeToRichType = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> highlightedMethods = new Dictionary<string, string>();

        private static readonly Dictionary<Type, string> builtInTypesToShorthand = new Dictionary<Type, string>
        {
            {
                typeof(object), "object"
            },
            {
                typeof(string), "string"
            },
            {
                typeof(bool), "bool"
            },
            {
                typeof(byte), "byte"
            },
            {
                typeof(sbyte), "sbyte"
            },
            {
                typeof(char), "char"
            },
            {
                typeof(decimal), "decimal"
            },
            {
                typeof(double), "double"
            },
            {
                typeof(float), "float"
            },
            {
                typeof(int), "int"
            },
            {
                typeof(uint), "uint"
            },
            {
                typeof(long), "long"
            },
            {
                typeof(ulong), "ulong"
            },
            {
                typeof(short), "short"
            },
            {
                typeof(ushort), "ushort"
            },
            {
                typeof(void), "void"
            },
        };

        /// <summary>Highlight the full signature of the Type, including optionally the Namespace, and optionally combined with a MemberInfo.</summary>
        public static string Parse(Type type,
            bool includeNamespace,
            MemberInfo memberInfo = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (memberInfo is MethodInfo mi)
            {
                return ParseMethod(mi);
            }

            if (memberInfo is ConstructorInfo ci)
            {
                return ParseConstructor(ci);
            }

            var sb = new StringBuilder();

            if (type.IsByRef)
            {
                AppendOpenColor(sb, $"#{keywordBlueHex}").Append("ref ").Append(CLOSE_COLOR);
            }

            // Namespace

            // Never include namespace for built-in types.
            var temp = type;
            while (temp.HasElementType)
            {
                temp = temp.GetElementType();
            }

            includeNamespace &= !builtInTypesToShorthand.ContainsKey(temp);

            if (!(type.IsGenericParameter || (type.HasElementType && type.GetElementType().IsGenericParameter)))
            {
                if (includeNamespace && TryGetNamespace(type, out var ns))
                {
                    AppendOpenColor(sb, NAMESPACE).Append(ns).Append(CLOSE_COLOR).Append('.');
                }
            }

            // Highlight the type and optional memberinfo

            sb.Append(ProcessType(type));

            if (memberInfo != null)
            {
                sb.Append('.');
                var start = sb.Length - 1;

                AppendOpenColor(sb, GetMemberInfoColor(memberInfo, out var isStatic)).Append(memberInfo.Name).Append(CLOSE_COLOR);

                if (isStatic)
                {
                    sb.Insert(start, OPEN_ITALIC);
                    sb.Append(CLOSE_ITALIC);
                }
            }

            return sb.ToString();
        }

        private static string ProcessType(Type type)
        {
            var key = type.ToString();
            if (typeToRichType.ContainsKey(key))
            {
                return typeToRichType[key];
            }

            var sb = new StringBuilder();

            if (!type.IsGenericParameter)
            {
                // Declaring type
                var start = sb.Length;
                var declaring = type.DeclaringType;
                while (declaring != null)
                {
                    sb.Insert(start, $"{HighlightType(declaring)}.");
                    declaring = declaring.DeclaringType;
                }

                // Type itself
                sb.Append(HighlightType(type));

                // Process generic arguments
                if (type.IsGenericType)
                {
                    ProcessGenericArguments(type, sb);
                }
            }
            else
            {
                sb.Append(OPEN_COLOR).Append(CONST).Append('>').Append(type.Name).Append(CLOSE_COLOR);
            }

            var ret = sb.ToString();
            typeToRichType.Add(key, ret);

            return ret;
        }

        internal static string GetClassColor(Type type)
        {
            if (type.IsAbstract && type.IsSealed)
            {
                return CLASS_STATIC;
            }

            if (type.IsEnum || type.IsGenericParameter)
            {
                return CONST;
            }

            if (type.IsValueType)
            {
                return STRUCT;
            }

            if (type.IsInterface)
            {
                return INTERFACE;
            }

            return CLASS_INSTANCE;
        }

        private static bool TryGetNamespace(Type type,
            out string ns)
        {
            return !string.IsNullOrEmpty(ns = type.Namespace?.Trim());
        }

        private static StringBuilder AppendOpenColor(StringBuilder sb,
            string color)
        {
            return sb.Append(OPEN_COLOR).Append(color).Append('>');
        }

        private static string HighlightType(Type type)
        {
            var sb = new StringBuilder();

            if (type.IsByRef)
            {
                type = type.GetElementType();
            }

            var arrayDimensions = 0;
            if (ArrayTokenRegex.Match(type.Name) is Match match && match.Success)
            {
                arrayDimensions = 1 + match.Value.Count(c => c == ',');
                type = type.GetElementType();
            }

            // Append type name, and replace with built-in shorthand name if applicable (eg System.String -> string)
            if (builtInTypesToShorthand.TryGetValue(type, out var builtInName))
            {
                AppendOpenColor(sb, $"#{keywordBlueHex}").Append(builtInName).Append(CLOSE_COLOR);
            }
            else // not a built-in type
            {
                sb.Append($"{OPEN_COLOR}{GetClassColor(type)}>").Append(type.Name).Append(CLOSE_COLOR);
            }

            if (arrayDimensions > 0)
            {
                sb.Append('[').Append(new string(',', arrayDimensions - 1)).Append(']');
            }

            return sb.ToString();
        }

        private static void ProcessGenericArguments(Type type,
            StringBuilder sb)
        {
            // This will include inherited generic arguments.
            // Eg, A<string>.B<int> would return [String, Int]
            var allArguments = type.GetGenericArguments().ToList();

            // Go through the StringBuilder and replace all `N to be <T> instead.
            // Eg A`2.B`1 -> A<string, bool>.B<int>

            var i = 0;
            while (i < sb.Length)
            {
                if (!allArguments.Any())
                {
                    break;
                }

                // Check for opening `
                if (sb[i] == '`')
                {
                    var start = i;
                    i++;
                    // Get the length string (it might not just be one digit)
                    var lenBuilder = new StringBuilder();
                    while (char.IsDigit(sb[i]))
                    {
                        lenBuilder.Append(sb[i]);
                        i++;
                    }

                    var lengthString = lenBuilder.ToString();
                    var argCount = int.Parse(lengthString);
                    // Remove the `N
                    sb.Remove(start, lengthString.Length + 1);

                    // move forward past the </color> tag
                    var closeColorIdx = 1;
                    start++;
                    while (closeColorIdx < CLOSE_COLOR.Length && sb[start] == CLOSE_COLOR[closeColorIdx])
                    {
                        closeColorIdx++;
                        start++;
                    }

                    // Insert the highlighted generic arguments
                    sb.Insert(start, '<');
                    start++;
                    var prevLen = sb.Length;
                    while (argCount > 0)
                    {
                        if (!allArguments.Any())
                        {
                            break;
                        }

                        argCount--;

                        var argument = allArguments.First();
                        allArguments.RemoveAt(0);

                        sb.Insert(start, ProcessType(argument));

                        // if still more arguments
                        if (argCount > 0)
                        {
                            start += sb.Length - prevLen;
                            sb.Insert(start, ", ");
                            start += 2;
                            prevLen = sb.Length;
                        }
                    }

                    sb.Insert(start + sb.Length - prevLen, '>');
                }

                i++;
            }
        }

        /// <summary>Removes highlighting from the string (color and italics only, as that is all this class handles).</summary>
        public static string RemoveHighlighting(string _string)
        {
            if (_string == null)
            {
                throw new ArgumentNullException(nameof(_string));
            }

            _string = _string.Replace(OPEN_ITALIC, string.Empty);
            _string = _string.Replace(CLOSE_ITALIC, string.Empty);

            _string = colorTagRegex.Replace(_string, string.Empty);
            _string = _string.Replace(CLOSE_COLOR, string.Empty);

            return _string;
        }

        [Obsolete("Use 'ParseMethod(MethodInfo)' instead (rename).")]
        public static string HighlightMethod(MethodInfo method)
        {
            return ParseMethod(method);
        }

        /// <summary>Highlight the provided method's signature with it's containing Type, and all arguments.</summary>
        public static string ParseMethod(MethodInfo method)
        {
            var sig = method.FullDescription();
            if (highlightedMethods.ContainsKey(sig))
            {
                return highlightedMethods[sig];
            }

            var sb = new StringBuilder();

            // highlight declaring type
            sb.Append(Parse(method.DeclaringType, false));
            sb.Append('.');

            // method name
            var color = !method.IsStatic ? METHOD_INSTANCE : METHOD_STATIC;
            sb.Append($"<color={color}>{method.Name}</color>");

            // generic arguments
            if (method.IsGenericMethod)
            {
                sb.Append("<");
                var genericArgs = method.GetGenericArguments();
                for (var i = 0; i < genericArgs.Length; i++)
                {
                    var arg = genericArgs[i];
                    if (arg.IsGenericParameter)
                    {
                        sb.Append($"<color={CONST}>{genericArgs[i].Name}</color>");
                    }
                    else
                    {
                        sb.Append(Parse(arg, false));
                    }

                    if (i < genericArgs.Length - 1)
                    {
                        sb.Append(", ");
                    }
                }

                sb.Append(">");
            }

            // arguments
            sb.Append('(');
            var parameters = method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                sb.Append(Parse(param.ParameterType, false));
                if (i < parameters.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(')');

            var ret = sb.ToString();
            highlightedMethods.Add(sig, ret);

            return ret;
        }

        [Obsolete("Use 'ParseConstructor(ConstructorInfo)' instead (rename).")]
        public static string HighlightConstructor(ConstructorInfo ctor)
        {
            return ParseConstructor(ctor);
        }

        /// <summary>Highlight the provided constructors's signature with it's containing Type, and all arguments.</summary>
        public static string ParseConstructor(ConstructorInfo ctor)
        {
            var sig = ctor.FullDescription();
            if (highlightedMethods.ContainsKey(sig))
            {
                return highlightedMethods[sig];
            }

            var sb = new StringBuilder();

            // highlight declaring type, then again to signify the constructor
            sb.Append(Parse(ctor.DeclaringType, false));
            var copy = sb.ToString();
            sb.Append('.');
            sb.Append(copy);

            // arguments
            sb.Append('(');
            var parameters = ctor.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                sb.Append(Parse(param.ParameterType, false));
                if (i < parameters.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(')');

            var ret = sb.ToString();
            highlightedMethods.Add(sig, ret);

            return ret;
        }

        /// <summary>Get the color used by SignatureHighlighter for the provided member, and whether it is static or not.</summary>
        public static string GetMemberInfoColor(MemberInfo memberInfo,
            out bool isStatic)
        {
            isStatic = false;
            if (memberInfo is FieldInfo fi)
            {
                if (fi.IsStatic)
                {
                    isStatic = true;

                    return FIELD_STATIC;
                }

                return FIELD_INSTANCE;
            }

            if (memberInfo is MethodInfo mi)
            {
                if (mi.IsStatic)
                {
                    isStatic = true;

                    return METHOD_STATIC;
                }

                return METHOD_INSTANCE;
            }

            if (memberInfo is PropertyInfo pi)
            {
                if (pi.GetAccessors(true)[0].IsStatic)
                {
                    isStatic = true;

                    return PROP_STATIC;
                }

                return PROP_INSTANCE;
            }

            if (memberInfo is ConstructorInfo)
            {
                isStatic = true;

                return CLASS_INSTANCE;
            }

            throw new NotImplementedException(memberInfo.GetType().Name + " is not supported");
        }
    }
}
