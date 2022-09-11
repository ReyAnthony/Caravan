using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CaravanSerialization.Substitutes
{
    public interface ISubstitutesFinder
    {
        public object GetSubstitutedTypeFor(Type original, object objectToCast);
        public object GetInstanceOfSubstitutedTypeFor(Type original);
        
        public object GetOriginalTypeFor(Type substituted, object objectToCast);
    }
    
    internal class SubstitutesFinder : ISubstitutesFinder
    {
        private readonly Dictionary<Type, Type> _substituteToOriginalTypes;
        private readonly List<Type> _substitutes;
        public SubstitutesFinder()
        {
            //Find all classes that have [SerializationSubstituteForAttribute]
            var substitutes = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .AsEnumerable()
                .Where(PublicCaravanExtensions.IsAssemblyNotFromUnity)
                .SelectMany(PublicCaravanExtensions.GetTypesThatAreDecoratedBy<SerializationSubstituteForAttribute>)
                .ToList();

            _substitutes = substitutes.Select(s => s as Type).ToList();
            _substituteToOriginalTypes = new Dictionary<Type, Type>();
            
            foreach (var substitute in substitutes)
            {
                var attribute = substitute.GetCustomAttribute<SerializationSubstituteForAttribute>();
                _substituteToOriginalTypes.Add(attribute.Type, substitute);
            }
        }
        
        public object GetSubstitutedTypeFor(Type original, object objectToCast)
        {
            _substituteToOriginalTypes.TryGetValue(original, out var substituteType);
            if (substituteType == null)
            {
                throw new UnityException($"No substitute for {original}");
            }
            
            var method = substituteType.GetMethod("op_Explicit", new[] { original });
            if (method == null)
            {
                throw new UnityException($"op_Explicit {original} should be implemented for {substituteType}");
            }
            var result = method.Invoke(null, new [] { objectToCast });
            return result ?? objectToCast;
        }

        public object GetInstanceOfSubstitutedTypeFor(Type original)
        {
            if (original == typeof(string))
            {
                return GetSubstitutedTypeFor(original, "");
            }

            var t = Activator.CreateInstance(original);
            return GetSubstitutedTypeFor(original, t);
        }

        public object GetOriginalTypeFor(Type substituted, object objectToCast)
        {
            var substituteType = _substitutes.FirstOrDefault(t => t == substituted);
            if (substituteType == null)
            {
                throw new UnityException($"No substitute for {substituted} " +
                                         $"(This should never happen, did you delete some types ???)");
            }
            
            var method = substituteType.GetMethod("op_Explicit", new[] { substituted });
            if (method == null)
            {
                throw new UnityException($"op_Explicit {substituted} should be implemented for {substituteType}");
            }
            
            var result = method.Invoke(null, new [] { objectToCast });
            return result ?? objectToCast;
        }
    }
}