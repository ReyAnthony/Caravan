using System;
using System.Linq;
using System.Reflection;
using CaravanSerialization.Attributes;
using CaravanSerialization.Substitutes;
using UnityEditor.Callbacks;
using UnityEngine;

namespace CaravanSerialization.Editor
{
    internal static class CaravanScriptReloadValidations
    {
        [DidReloadScripts]
        public static void CheckDuppedIds()
        {
            bool CheckAllIds(string _) => true;
            CaravanHelper.Instance.CheckDuplicatedIDs(CheckAllIds);
        }
        
        [DidReloadScripts]
        public static void CheckThatRequireSavedInInheritorsIsEnforced()
        {
            //get all scriptableObject inheritors that are Tagged RequireSavedInInheritorsAttribute
            var type = typeof(ScriptableObject);
            var typesWithRequired = AppDomain.CurrentDomain.GetAssemblies()
                               .SelectMany(ass => ass.GetTypes())
                               .Where(t => t.IsClass && type.IsAssignableFrom(t))
                               .Where(t => t.GetCustomAttributes(false).FirstOrDefault(a => a is RequireSavedInInheritorsAttribute) != null)
                               .ToList();

            //For each type that requires inheritors to have SavedAttributes
            foreach(var typeWithRequired in typesWithRequired)
            {
                //Check if any of them DO NOT have SavedAttributes
                var needSaved = AppDomain.CurrentDomain.GetAssemblies()
                             .SelectMany(ass => ass.GetTypes())
                             .Where(t => t.IsClass && typeWithRequired.IsAssignableFrom(t))
                             .Where(t => t != typeWithRequired)
                             .Where(t => t.GetCustomAttributes(false).FirstOrDefault(a => a is SavedAttribute) == null)
                             .ToList();


                foreach (var classes in needSaved)
                {
                    Debug.LogError($"{classes.Name} needs to be tagged with [{typeof(SavedAttribute).Name}] " +
                        $"because {typeWithRequired.Name} is [{typeof(RequireSavedInInheritorsAttribute).Name}]");
                }
            }
        }

        [DidReloadScripts]
        public static void CheckTypeSubstitutesAreValid()
        {
            var substitutes = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .AsEnumerable()
                .Where(PublicCaravanExtensions.IsAssemblyNotFromUnity)
                .SelectMany(PublicCaravanExtensions.GetTypesThatAreDecoratedBy<SerializationSubstituteForAttribute>)
                .Select(t => (Type: t, Attribute: t.GetCustomAttribute(typeof(SerializationSubstituteForAttribute)) as SerializationSubstituteForAttribute))
                .ToList();

            foreach (var typeAndAttr in substitutes)
            {
                var hasOpTo = typeAndAttr.Type.DeclaredMethods
                   .Where(m => m.Name == "op_Explicit")
                   .Where(m => m.ReturnType == typeAndAttr.Attribute.Type)
                   .Count(m => m.GetParameters().Count(p => p.ParameterType == typeAndAttr.Type) == 1) == 1;

               var hasOpFrom = typeAndAttr.Type.DeclaredMethods
                   .Where(m => m.Name == "op_Explicit")
                   .Where(m => m.ReturnType == typeAndAttr.Type)
                   .Count(m => m.GetParameters().Count(p => p.ParameterType == typeAndAttr.Attribute.Type) == 1) == 1;

               if (!(hasOpTo && hasOpFrom))
               {
                  Debug.LogError($"{typeAndAttr.Attribute.Type.Name} substitute has issues with its explicit operators. " +
                                 $"Save/Load will fail.");
               }
            }
        }
    }
}
