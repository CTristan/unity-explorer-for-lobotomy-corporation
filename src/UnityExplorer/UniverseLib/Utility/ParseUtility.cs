using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Harmony;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;

namespace UnityExplorerForLobotomyCorporation.UniverseLib.Utility
{
    public static class ParseUtility
    {
        /// <summary>Equivalent to <c>$"0.####"</c>.</summary>
        public static readonly string NumberFormatString = "0.####";

        private static readonly Dictionary<int, string> numSequenceStrings = new Dictionary<int, string>();

        private static readonly HashSet<Type> nonPrimitiveTypes = new HashSet<Type>
        {
            typeof(string),
            typeof(decimal),
            typeof(DateTime),
        };

        private static readonly HashSet<Type> formattedTypes = new HashSet<Type>
        {
            typeof(float),
            typeof(double),
            typeof(decimal),
        };

        private static readonly Dictionary<string, string> typeInputExamples = new Dictionary<string, string>();

        private static readonly Dictionary<string, ParseMethod> customTypes = new Dictionary<string, ParseMethod>
        {
            {
                typeof(Vector2).FullName, TryParseVector2
            },
            {
                typeof(Vector3).FullName, TryParseVector3
            },
            {
                typeof(Vector4).FullName, TryParseVector4
            },
            {
                typeof(Quaternion).FullName, TryParseQuaternion
            },
            {
                typeof(Rect).FullName, TryParseRect
            },
            {
                typeof(Color).FullName, TryParseColor
            },
            {
                typeof(Color32).FullName, TryParseColor32
            },
            {
                typeof(LayerMask).FullName, TryParseLayerMask
            },
        };

        private static readonly Dictionary<string, ToStringMethod> customTypesToString = new Dictionary<string, ToStringMethod>
        {
            {
                typeof(Vector2).FullName, Vector2ToString
            },
            {
                typeof(Vector3).FullName, Vector3ToString
            },
            {
                typeof(Vector4).FullName, Vector4ToString
            },
            {
                typeof(Quaternion).FullName, QuaternionToString
            },
            {
                typeof(Rect).FullName, RectToString
            },
            {
                typeof(Color).FullName, ColorToString
            },
            {
                typeof(Color32).FullName, Color32ToString
            },
            {
                typeof(LayerMask).FullName, LayerMaskToString
            },
        };

        // Helper for formatting float/double/decimal numbers to maximum of 4 decimal points.
        // And also for formatting a sequence of those numbers, ie a Vector3, Color etc

        /// <summary>Formats the array of float, double or decimal numbers into a formatted string.</summary>
        /// <param name="numbers"></param>
        /// <returns></returns>
        public static string FormatDecimalSequence(params object[] numbers)
        {
            if (numbers.Length <= 0)
            {
                return null;
            }

            return string.Format(CultureInfo.CurrentCulture, GetSequenceFormatString(numbers.Length), numbers);
        }

        internal static string GetSequenceFormatString(int count)
        {
            if (count <= 0)
            {
                return null;
            }

            if (numSequenceStrings.ContainsKey(count))
            {
                return numSequenceStrings[count];
            }

            var strings = new string[count];

            for (var i = 0; i < count; i++) strings[i] = $"{{{i}:{NumberFormatString}}}";

            var ret = string.Join(" ", strings);
            numSequenceStrings.Add(count, ret);

            return ret;
        }

        // Main parsing API

        /// <summary>Returns true if ParseUtility is able to parse the provided Type.</summary>
        public static bool CanParse(Type type)
        {
            return !string.IsNullOrEmpty(type?.FullName) && (type.IsPrimitive || type.IsEnum || nonPrimitiveTypes.Contains(type) || customTypes.ContainsKey(type.FullName));
        }

        /// <summary>Returns true if ParseUtility is able to parse the provided Type.</summary>
        public static bool CanParse<T>()
        {
            return CanParse(typeof(T));
        }

        /// <summary>Attempt to parse the provided input into an object of the provided Type. Returns true if successful, false if not.</summary>
        public static bool TryParse<T>(string input,
            out T obj,
            out Exception parseException)
        {
            var result = TryParse(input, typeof(T), out var parsed, out parseException);
            if (parsed != null)
            {
                obj = (T)parsed;
            }
            else
            {
                obj = default;
            }

            return result;
        }

        /// <summary>Attempt to parse the provided input into an object of the provided Type. Returns true if successful, false if not.</summary>
        public static bool TryParse(string input,
            Type type,
            out object obj,
            out Exception parseException)
        {
            obj = null;
            parseException = null;

            if (type == null)
            {
                return false;
            }

            if (type == typeof(string))
            {
                obj = input;

                return true;
            }

            if (type.IsEnum)
            {
                try
                {
                    obj = Enum.Parse(type, input);

                    return true;
                }
                catch (Exception ex)
                {
                    parseException = ex.GetInnerMostException();

                    return false;
                }
            }

            try
            {
                if (customTypes.ContainsKey(type.FullName))
                {
                    obj = customTypes[type.FullName].Invoke(input);
                }
                else
                {
                    obj = AccessTools.Method(type, "Parse", ArgumentUtility.ParseArgs).Invoke(null, new object[]
                    {
                        input,
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                ex = ex.GetInnerMostException();
                parseException = ex;
            }

            return false;
        }

        /// <summary>Returns the obj.ToString() result, formatted into the format which ParseUtility would expect for user input.</summary>
        public static string ToStringForInput<T>(object obj)
        {
            return ToStringForInput(obj, typeof(T));
        }

        /// <summary>Returns the obj.ToString() result, formatted into the format which ParseUtility would expect for user input.</summary>
        public static string ToStringForInput(object obj,
            Type type)
        {
            if (type == null || obj == null)
            {
                return null;
            }

            if (type == typeof(string))
            {
                return obj as string;
            }

            if (type.IsEnum)
            {
                return Enum.IsDefined(type, obj) ? Enum.GetName(type, obj) : obj.ToString();
            }

            try
            {
                if (customTypes.ContainsKey(type.FullName))
                {
                    return customTypesToString[type.FullName].Invoke(obj);
                }

                if (formattedTypes.Contains(type))
                {
                    return AccessTools.Method(type, "ToString", new[]
                    {
                        typeof(string), typeof(IFormatProvider),
                    }).Invoke(obj, new object[]
                    {
                        NumberFormatString, CultureInfo.CurrentCulture,
                    }) as string;
                }

                return obj.ToString();
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception formatting object for input: {ex}");

                return null;
            }
        }

        /// <summary>Gets a default example input which can be displayed to users, for example for Vector2 this would return "0 0".</summary>
        public static string GetExampleInput<T>()
        {
            return GetExampleInput(typeof(T));
        }

        /// <summary>Gets a default example input which can be displayed to users, for example for Vector2 this would return "0 0".</summary>
        public static string GetExampleInput(Type type)
        {
            if (!typeInputExamples.ContainsKey(type.AssemblyQualifiedName))
            {
                try
                {
                    if (type.IsEnum)
                    {
                        typeInputExamples.Add(type.AssemblyQualifiedName, Enum.GetNames(type).First());
                    }
                    else
                    {
                        var instance = Activator.CreateInstance(type);
                        typeInputExamples.Add(type.AssemblyQualifiedName, ToStringForInput(instance, type));
                    }
                }
                catch (Exception ex)
                {
                    Universe.LogWarning("Exception generating default instance for example input for '" + type.FullName + "'");
                    Universe.Log(ex);

                    return "";
                }
            }

            return typeInputExamples[type.AssemblyQualifiedName];
        }

        internal delegate object ParseMethod(string input);

        internal delegate string ToStringMethod(object obj);

        #region Custom parse methods

        // Vector2

        internal static object TryParseVector2(string input)
        {
            Vector2 vector = default;

            var split = input.Split(' ');

            vector.x = float.Parse(split[0].Trim(), CultureInfo.CurrentCulture);
            vector.y = float.Parse(split[1].Trim(), CultureInfo.CurrentCulture);

            return vector;
        }

        internal static string Vector2ToString(object obj)
        {
            if (!(obj is Vector2 vector))
            {
                return null;
            }

            return FormatDecimalSequence(vector.x, vector.y);
        }

        // Vector3

        internal static object TryParseVector3(string input)
        {
            Vector3 vector = default;

            var split = input.Split(' ');

            vector.x = float.Parse(split[0].Trim(), CultureInfo.CurrentCulture);
            vector.y = float.Parse(split[1].Trim(), CultureInfo.CurrentCulture);
            vector.z = float.Parse(split[2].Trim(), CultureInfo.CurrentCulture);

            return vector;
        }

        internal static string Vector3ToString(object obj)
        {
            if (!(obj is Vector3 vector))
            {
                return null;
            }

            return FormatDecimalSequence(vector.x, vector.y, vector.z);
        }

        // Vector4

        internal static object TryParseVector4(string input)
        {
            Vector4 vector = default;

            var split = input.Split(' ');

            vector.x = float.Parse(split[0].Trim(), CultureInfo.CurrentCulture);
            vector.y = float.Parse(split[1].Trim(), CultureInfo.CurrentCulture);
            vector.z = float.Parse(split[2].Trim(), CultureInfo.CurrentCulture);
            vector.w = float.Parse(split[3].Trim(), CultureInfo.CurrentCulture);

            return vector;
        }

        internal static string Vector4ToString(object obj)
        {
            if (!(obj is Vector4 vector))
            {
                return null;
            }

            return FormatDecimalSequence(vector.x, vector.y, vector.z, vector.w);
        }

        // Quaternion

        internal static object TryParseQuaternion(string input)
        {
            Vector3 vector = default;

            var split = input.Split(' ');

            if (split.Length == 4)
            {
                Quaternion quat = default;
                quat.x = float.Parse(split[0].Trim(), CultureInfo.CurrentCulture);
                quat.y = float.Parse(split[1].Trim(), CultureInfo.CurrentCulture);
                quat.z = float.Parse(split[2].Trim(), CultureInfo.CurrentCulture);
                quat.w = float.Parse(split[3].Trim(), CultureInfo.CurrentCulture);

                return quat;
            }

            vector.x = float.Parse(split[0].Trim(), CultureInfo.CurrentCulture);
            vector.y = float.Parse(split[1].Trim(), CultureInfo.CurrentCulture);
            vector.z = float.Parse(split[2].Trim(), CultureInfo.CurrentCulture);

            return Quaternion.Euler(vector);
        }

        internal static string QuaternionToString(object obj)
        {
            if (!(obj is Quaternion quaternion))
            {
                return null;
            }

            var vector = quaternion.eulerAngles;

            return FormatDecimalSequence(vector.x, vector.y, vector.z);
        }

        // Rect

        internal static object TryParseRect(string input)
        {
            Rect rect = default;

            var split = input.Split(' ');

            rect.x = float.Parse(split[0].Trim(), CultureInfo.CurrentCulture);
            rect.y = float.Parse(split[1].Trim(), CultureInfo.CurrentCulture);
            rect.width = float.Parse(split[2].Trim(), CultureInfo.CurrentCulture);
            rect.height = float.Parse(split[3].Trim(), CultureInfo.CurrentCulture);

            return rect;
        }

        internal static string RectToString(object obj)
        {
            if (!(obj is Rect rect))
            {
                return null;
            }

            return FormatDecimalSequence(rect.x, rect.y, rect.width, rect.height);
        }

        // Color

        internal static object TryParseColor(string input)
        {
            Color color = default;

            var split = input.Split(' ');

            color.r = float.Parse(split[0].Trim(), CultureInfo.CurrentCulture);
            color.g = float.Parse(split[1].Trim(), CultureInfo.CurrentCulture);
            color.b = float.Parse(split[2].Trim(), CultureInfo.CurrentCulture);
            if (split.Length > 3)
            {
                color.a = float.Parse(split[3].Trim(), CultureInfo.CurrentCulture);
            }
            else
            {
                color.a = 1;
            }

            return color;
        }

        internal static string ColorToString(object obj)
        {
            if (!(obj is Color color))
            {
                return null;
            }

            return FormatDecimalSequence(color.r, color.g, color.b, color.a);
        }

        // Color32

        internal static object TryParseColor32(string input)
        {
            Color32 color = default;

            var split = input.Split(' ');

            color.r = byte.Parse(split[0].Trim(), CultureInfo.CurrentCulture);
            color.g = byte.Parse(split[1].Trim(), CultureInfo.CurrentCulture);
            color.b = byte.Parse(split[2].Trim(), CultureInfo.CurrentCulture);
            if (split.Length > 3)
            {
                color.a = byte.Parse(split[3].Trim(), CultureInfo.CurrentCulture);
            }
            else
            {
                color.a = 255;
            }

            return color;
        }

        internal static string Color32ToString(object obj)
        {
            if (!(obj is Color32 color))
            {
                return null;
            }

            // ints, this is fine
            return $"{color.r} {color.g} {color.b} {color.a}";
        }

        // Layermask (Int32)

        internal static object TryParseLayerMask(string input)
        {
            return (LayerMask)int.Parse(input);
        }

        internal static string LayerMaskToString(object obj)
        {
            if (!(obj is LayerMask mask))
            {
                return null;
            }

            return mask.value.ToString();
        }

        #endregion
    }
}
