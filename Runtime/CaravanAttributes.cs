﻿using System;
using UnityEngine;

namespace CaravanSerialization
{
    public abstract class ValidableAttribute : Attribute
    {
        public abstract bool Validate();
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RequireSavedInInheritorsAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SavedAttribute : ValidableAttribute
    {
        public string File { get; private set; }
        public bool Instantiate { get; }

        public override bool Validate()
        {
            if (string.IsNullOrEmpty(File))
            {
                throw new UnityException("Saved Attributes must have non null or empty File");
            }

            return true;
        }

        public SavedAttribute(string file)
        {
            File = file;
        }

        public SavedAttribute(string file, bool instantiate) : this(file)
        {
            Instantiate = instantiate;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class NestedAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SaveThatAttribute : Attribute
    {

    }

    //Must be SerializedField and have a unique value
    [AttributeUsage(AttributeTargets.Field)]
    public class CaravanIdAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SaveCallback : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Method)]
    public class LoadCallback : Attribute
    {

    }
}
