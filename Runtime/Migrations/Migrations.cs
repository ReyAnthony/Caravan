using System;
using System.Collections.Generic;
using UnityEngine;
using CaravanSerialization.ObjectModel;

namespace CaravanSerialization.Migrations
{
    public interface IMigrationHandler
    {
        public int Version { get; }

        //Defines how each scriptableObject should be handled upon loading the new Version
        public List<IMigrationDefinition> MigrationDefinitions { get; }
        public IMigrationDefinition FindDefinitionForType(Type t);
    }

    public interface IMigrationDefinition
    {
        public Type Type { get; }
        public void Migrate(ILoader loader, object obj);
    }

    public abstract class AbstractMigrationDefinition : IMigrationDefinition
    {
        public abstract Type Type { get; }

        public void Migrate(ILoader loader, object obj)
        {
            if (obj.GetType() != Type)
            {
                throw new UnityException($"Migration type must conform to the specified type Actual : {obj.GetType().Name} Excepted : {Type.Name}");
            }

            if(!obj.GetType().IsSubclassOf(typeof(ScriptableObject)))
            {
                throw new UnityException("Type must inherit from ScriptableObject");
            }

            InternalMigrate(loader, obj);
        }

        protected abstract void InternalMigrate(ILoader loader, object obj);
    }

}
