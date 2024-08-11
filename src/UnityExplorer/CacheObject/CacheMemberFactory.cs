using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Extensions;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Runtime;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.CacheObject
{
    public static class CacheMemberFactory
    {
        public static List<CacheMember> GetCacheMembers(Type type,
            ReflectionInspector inspector)
        {
            //var list = new List<CacheMember>();
            var cachedSigs = new HashSet<string>();
            var props = new List<CacheMember>();
            var fields = new List<CacheMember>();
            var ctors = new List<CacheMember>();
            var methods = new List<CacheMember>();

            var types = ReflectionUtility.GetAllBaseTypes(type);

            var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            if (!inspector.StaticOnly)
            {
                flags |= BindingFlags.Instance;
            }

            if (!type.IsAbstract)
            {
                // Get non-static constructors of the main type.
                // There's no reason to get the static cctor, it will be invoked when we inspect the class.
                // Also no point getting ctors on inherited types.
                foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    TryCacheMember(ctor, ctors, cachedSigs, type, inspector);
                }

                // structs always have a parameterless constructor
                if (type.IsValueType)
                {
                    var cached = new CacheConstructor(type);
                    cached.SetFallbackType(type);
                    cached.SetInspectorOwner(inspector, null);
                    ctors.Add(cached);
                }
            }

            foreach (var declaringType in types)
            {
                foreach (var prop in declaringType.GetProperties(flags))
                {
                    if (prop.DeclaringType == declaringType)
                    {
                        TryCacheMember(prop, props, cachedSigs, declaringType, inspector);
                    }
                }

                foreach (var field in declaringType.GetFields(flags))
                {
                    if (field.DeclaringType == declaringType)
                    {
                        TryCacheMember(field, fields, cachedSigs, declaringType, inspector);
                    }
                }

                foreach (var method in declaringType.GetMethods(flags))
                {
                    if (method.DeclaringType == declaringType)
                    {
                        TryCacheMember(method, methods, cachedSigs, declaringType, inspector);
                    }
                }
            }

            var sorted = new List<CacheMember>();
            sorted.AddRange(props.OrderBy(it => Array.IndexOf(types, it.DeclaringType)).ThenBy(it => it.NameForFiltering));
            sorted.AddRange(fields.OrderBy(it => Array.IndexOf(types, it.DeclaringType)).ThenBy(it => it.NameForFiltering));
            sorted.AddRange(ctors.OrderBy(it => Array.IndexOf(types, it.DeclaringType)).ThenBy(it => it.NameForFiltering));
            sorted.AddRange(methods.OrderBy(it => Array.IndexOf(types, it.DeclaringType)).ThenBy(it => it.NameForFiltering));

            return sorted;
        }

        private static void TryCacheMember<T>(MemberInfo member,
            List<T> list,
            HashSet<string> cachedSigs,
            Type declaringType,
            ReflectionInspector inspector,
            bool ignorePropertyMethodInfos = true) where T : CacheMember
        {
            try
            {
                if (UERuntimeHelper.IsBlacklisted(member))
                {
                    return;
                }

                string sig;
                if (member is MethodBase mb)
                {
                    sig = mb.FullDescription(); // (method or constructor)
                }
                else if (member is PropertyInfo || member is FieldInfo)
                {
                    sig = $"{member.DeclaringType.FullDescription()}.{member.Name}";
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (cachedSigs.Contains(sig))
                {
                    return;
                }

                // ExplorerCore.Log($"Trying to cache member {sig}... ({member.MemberType})");

                CacheMember cached;
                Type returnType;

                switch (member.MemberType)
                {
                    case MemberTypes.Constructor:
                    {
                        var ci = member as ConstructorInfo;
                        cached = new CacheConstructor(ci);
                        returnType = ci.DeclaringType;
                    }

                        break;

                    case MemberTypes.Method:
                    {
                        var mi = member as MethodInfo;
                        if (ignorePropertyMethodInfos && (mi.Name.StartsWith("get_") || mi.Name.StartsWith("set_")))
                        {
                            return;
                        }

                        cached = new CacheMethod(mi);
                        returnType = mi.ReturnType;

                        break;
                    }

                    case MemberTypes.Property:
                    {
                        var pi = member as PropertyInfo;

                        if (!pi.CanRead && pi.CanWrite)
                        {
                            // write-only property, cache the set method instead.
                            var setMethod = pi.GetSetMethod(true);
                            if (setMethod != null)
                            {
                                TryCacheMember(setMethod, list, cachedSigs, declaringType, inspector, false);
                            }

                            return;
                        }

                        cached = new CacheProperty(pi);
                        returnType = pi.PropertyType;

                        break;
                    }

                    case MemberTypes.Field:
                    {
                        var fi = member as FieldInfo;
                        cached = new CacheField(fi);
                        returnType = fi.FieldType;

                        break;
                    }

                    default:
                        throw new NotImplementedException();
                }

                cachedSigs.Add(sig);

                cached.SetFallbackType(returnType);
                cached.SetInspectorOwner(inspector, member);

                list.Add((T)cached);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"Exception caching member {member.DeclaringType.FullName}.{member.Name}!");
                ExplorerCore.Log(e);
            }
        }
    }
}
