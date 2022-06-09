using System;
using System.Linq;
using UnityEditor.Callbacks;
using UnityEngine;

namespace CaravanSerialization
{
    internal class CaravanScriptReloadValidations
    {
        //TODO do the same to check IDs
        //It's better to test everytime than only when we save or load.

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
    }
}
