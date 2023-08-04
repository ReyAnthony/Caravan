using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CaravanSerialization.Attributes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CaravanSerialization
{
    public static class PublicCaravanExtensions
    {
        public static T GetValue<T>(this FieldInfo fieldInfo, object obj) where T : class => fieldInfo.GetValue(obj) as T;
        
        public static IEnumerable<TypeInfo> GetTypesThatAreDecoratedBy<TAttribute>(this Assembly a) 
            where TAttribute : Attribute
        {
            return a.DefinedTypes.Where(t => t.GetCustomAttribute(typeof(TAttribute)) != null);
        }
        public static bool IsAssemblyNotFromUnity(this Assembly assembly)
        {
            return !assembly.FullName.Contains("UnityEditor")
                   && !assembly.FullName.Contains("UnityEngine")
                   && !assembly.FullName.Contains("Unity.")
                   && !assembly.FullName.Contains("System")
                   && !assembly.FullName.Contains("Mono.")
                   && !assembly.FullName.Contains("mscorlib")
                   && !assembly.FullName.Contains("netstandard")
                   && !assembly.FullName.Contains("nunit")
                   && !assembly.FullName.Contains("log4net")
                   && !assembly.FullName.Contains("Bee.BeeDriver")
                   && !assembly.FullName.Contains("ReportGeneratorMerged")
                   && !assembly.FullName.Contains("PlayerBuildProgramLibrary.Data")
                   && !assembly.FullName.Contains("ExCSS.Unity")
                   && !assembly.FullName.Contains("unityplastic")
                   && !assembly.FullName.Contains("Cinemachine");
        }
    }
    
    internal static class CaravanHelpers
    {
        internal static List<(FieldInfo Field, SaveThatAttribute Attr)> GetAllSaveThatFields(this object data)
        {
            return data.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .Select(f => (Field: f, Attr: f.GetCustomAttribute<SaveThatAttribute>()))
                .Where(tp => tp.Attr != null)
                .Where(tp => tp.Field.GetValue(data) != null)
                .ToList();
        }

        internal static List<ScriptableObject> GetAllScriptablesTaggedSaved()
        {
            var scriptableObjects = Resources.FindObjectsOfTypeAll<ScriptableObject>();
            var saved = scriptableObjects
#if UNITY_EDITOR
                //Don't instantiate SOs at runtime, it's a wasp nest.  
                .Where(AssetDatabase.Contains)
#endif
                .Where(so =>
                    so.GetType()
                        .GetCustomAttributes(false)
                        .FirstOrDefault(ca => ca is SavedAttribute) != null)
                .ToList();

            return saved;
        }
        internal static Action<TCallbackParam> GetCallback<TAttributeToFind, TCallbackParam>(this object obj) 
            where TAttributeToFind : Attribute
        {
            MethodInfo m = GetMethodWithAttribute<TAttributeToFind>(obj);

            //We are not required to implement them
            if (m == null)
            {
                return (cb) => { };
            }

            if (m.GetParameters().Count() != 1 || m.GetParameters()[0].ParameterType != typeof(TCallbackParam))
            {
                throw new UnityException($" [{typeof(TAttributeToFind).Name}] needs " +
                                         $"an {typeof(TCallbackParam).Name} Parameter, " +
                                         $"see {m.Name}() in {obj.GetType().Name}");
            }

            return (cb) =>
            {
                object[] @params = { cb };
                m.Invoke(obj, @params);
            };
        }

        internal static MethodInfo GetMethodWithAttribute<T>(this object obj) where T : Attribute
        {
            var result = FindMethodUpTheTypeHierarchy<T>(obj.GetType());
            return result.Item2;
        }
        
        internal static void FindIdAttributeAndField(this object o, out CaravanIdAttribute id, out FieldInfo field)
        {
            (id, field) = FindFieldUpTheTypeHierarchy<CaravanIdAttribute>(o.GetType());
        }

        internal static FieldInfo FindIdField(this object o)
        {
            return FindFieldUpTheTypeHierarchy<CaravanIdAttribute>(o.GetType()).Item2;
        }

        internal static string GetId(this object o)
        {
            return o.FindIdField().GetValue<string>(o);
        }
        
        private static (TAttribute, MethodInfo) FindMethodUpTheTypeHierarchy<TAttribute>(Type startType) 
            where TAttribute : Attribute
        {
            ValueTuple<TAttribute, MethodInfo> AttributeMetadata = (null, null);
            Type t = startType;

            //walk up the type hierarchy to find the CaravanId
            while (AttributeMetadata.Item1 == null && t != typeof(System.Object))
            {
                AttributeMetadata = t
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(f => f.GetCustomAttribute<TAttribute>() != null)
                    .Select(fi => (fi.GetCustomAttribute<TAttribute>(), fi))
                    .FirstOrDefault();
                t = t.BaseType;
            }

            return AttributeMetadata;
        }
        
        private static (TAttribute, FieldInfo) FindFieldUpTheTypeHierarchy<TAttribute>(Type startType) 
            where TAttribute : Attribute
        {
            ValueTuple<TAttribute, FieldInfo> AttributeMetadata = (null, null);
            Type t = startType;

            //walk up the type hierarchy to find the CaravanId
            while (AttributeMetadata.Item1 == null && t != typeof(System.Object))
            {
                AttributeMetadata = t
                    .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(f => f.GetCustomAttribute<TAttribute>() != null)
                    .Select(fi => (fi.GetCustomAttribute<TAttribute>(), fi))
                    .FirstOrDefault();
                t = t.BaseType;
            }

            return AttributeMetadata;
        }
    }
    
    //TODO Reload if domain reload is disabled
    public static class CaravanHelper
    {
        private static ICaravan _instance;

        //Call this at start of the game
        //If you need to avoid delay due to lazy loading
        public static void Preload()
        {
            var _ = Instance;
        }
        
        public static ICaravan Instance => _instance ??= new Caravan();
    }
}

