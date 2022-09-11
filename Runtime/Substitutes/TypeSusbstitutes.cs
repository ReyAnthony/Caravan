using System;
using CaravanSerialization.Attributes;
using JetBrains.Annotations;
using UnityEngine;

namespace CaravanSerialization.Substitutes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [UsedImplicitly]
    public class SerializationSubstituteForAttribute : NestedAttribute 
    {
        public readonly Type Type;

        public SerializationSubstituteForAttribute(Type type)
        {
            Type = type;
        }
    }
    
    [SerializationSubstituteFor(typeof(Vector3))] 
    internal class CaravanV3
    {
        [SaveThat] public float X;
        [SaveThat] public float Y;
        [SaveThat] public float Z;

        internal CaravanV3(Vector3 vector3)
        {
            X = vector3.x;
            Y = vector3.y;
            Z = vector3.z;
        }
        
        public static explicit operator Vector3(CaravanV3 v3) => new (v3.X, v3.Y, v3.Z);
        public static explicit operator CaravanV3(Vector3 v3) => new (v3);
    }
    
    [SerializationSubstituteFor(typeof(Quaternion))]
    internal class CaravanQuaternionEuler
    {
        [SaveThat] public float X;
        [SaveThat] public float Y;
        [SaveThat] public float Z;

        public CaravanQuaternionEuler(Quaternion q)
        {
            X = q.eulerAngles.x;
            Y = q.eulerAngles.y;
            Z = q.eulerAngles.z;
        }

        public static explicit operator Quaternion(CaravanQuaternionEuler q) => Quaternion.Euler(q.X, q.Y, q.Z);
        public static explicit operator CaravanQuaternionEuler(Quaternion q) => new (q);
    }

    [SerializationSubstituteFor(typeof(int))]
    internal class CaravanI32
    {
        [SaveThat(IgnoreTypeSubstitution = true)] public Int32 Val;
        public CaravanI32(Int32 i32)
        {
            Val = i32;
        }
        
        public static explicit operator CaravanI32(int i) => new (i);
        public static explicit operator int(CaravanI32 ci) => ci.Val;
    }

    [SerializationSubstituteFor(typeof(float))]
    internal class CaravanFloat
    {
        [SaveThat(IgnoreTypeSubstitution = true)] public float Val;
        public CaravanFloat(float f)
        {
            Val = f;
        }
        
        public static explicit operator CaravanFloat(float f) => new (f);
        public static explicit operator float(CaravanFloat cf) => cf.Val;
    }
    
    [SerializationSubstituteFor(typeof(bool))]
    internal class CaravanBool
    {
        [SaveThat(IgnoreTypeSubstitution = true)] public bool Val;
        public CaravanBool(bool b)
        {
            Val = b;
        }
        
        public static explicit operator CaravanBool(bool b) => new (b);
        public static explicit operator bool(CaravanBool cb) => cb.Val;
    }

    [SerializationSubstituteFor(typeof(char))]
    internal class CaravanChar
    {
        [SaveThat(IgnoreTypeSubstitution = true)] public char Val;
        public CaravanChar(char c)
        {
            Val = c;
        }
        
        public static explicit operator CaravanChar(char c) => new (c);
        public static explicit operator char(CaravanChar cc) => cc.Val;
    }
    
    [SerializationSubstituteFor(typeof(string))]
    internal class CaravanString
    {
        [SaveThat(IgnoreTypeSubstitution = true)] public string Val;
        public CaravanString(string s)
        {
            Val = s;
        }
        
        public static explicit operator CaravanString(string s) => new (s);
        public static explicit operator string(CaravanString cs) => cs.Val;
    }
}