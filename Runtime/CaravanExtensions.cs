using System;
using System.Reflection;

namespace CaravanSerialization
{
    public static class CaravanExtensions
    {

        public static void SetInstanceFieldValue(this object obj, string fieldName, object val)
        {
            obj.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | BindingFlags.Instance)
                .SetValue(obj, val);
        }
    }
}

