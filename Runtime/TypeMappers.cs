using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaravanSerialization
{
    internal class CaravanV3
    {
        public float X, Y, Z;

        public CaravanV3(Vector3 vector3)
        {
            this.X = vector3.x;
            this.Y = vector3.y;
            this.Z = vector3.z;
        }
    }

    internal class CaravanQuaternionEuler
    {
        public float X, Y, Z; 

        public CaravanQuaternionEuler(Quaternion q)
        {
            this.X = q.eulerAngles.x;
            this.Y = q.eulerAngles.y;
            this.Z = q.eulerAngles.z;
        }
    }

    internal class I32
    {
        public Int32 val;
        public I32(Int32 i32)
        {
            this.val = i32;
        }
    }

    internal class CaravanFloat
    {
        public float val;
        public CaravanFloat(float f)
        {
            this.val = f;
        }
    }

    internal class CaravanChar
    {
        public char val;
        public CaravanChar(char c)
        {
            this.val = c;
        }
    }

    internal class CaravanV3Wrapper : ICaravanWrapper<CaravanV3, Vector3>
    {
        public CaravanV3 OriginalToWrapped(Vector3 v3) => new(v3);
        public Vector3 WrappedToOriginal(CaravanV3 v3) => new(v3.X, v3.Y, v3.Z);
    }

    internal class CaravanQuaternionWrapper : ICaravanWrapper<CaravanQuaternionEuler, Quaternion>
    {
        public CaravanQuaternionEuler OriginalToWrapped(Quaternion q) => new(q);
        public Quaternion WrappedToOriginal(CaravanQuaternionEuler q) => Quaternion.Euler(q.X, q.Y, q.Z);
    }

    internal class I32Wrapper : ICaravanWrapper<I32, Int32>
    {
        public I32 OriginalToWrapped(int i) => new(i);
        public int WrappedToOriginal(I32 i) => (Int32)i.val;
    }

    internal class CaravanFloatWrapper : ICaravanWrapper<CaravanFloat, float>
    {
        public CaravanFloat OriginalToWrapped(float f) => new(f);
        public float WrappedToOriginal(CaravanFloat f) => f.val;
    }

    internal class CaravanCharWrapper : ICaravanWrapper<CaravanChar, char>
    {
        public CaravanChar OriginalToWrapped(char c) => new(c);
        public char WrappedToOriginal(CaravanChar c) => c.val;
    }

    internal class TypeMapperFinder : IMapperFinder
    {
        private List<IUserMapper> UserMappers;

        public TypeMapperFinder()
        {
            //Find all classes deriving from IUserMapper in the assembly
            //This will allow to make it work while in editor
            //TODO find a better way to identify user assemblies  
            var mappers = AppDomain
                            .CurrentDomain
                            .GetAssemblies()
                            .AsEnumerable()
                            .Where(ass => !ass.FullName.Contains("UnityEditor")
                                       && !ass.FullName.Contains("UnityEngine")
                                       && !ass.FullName.Contains("Unity.")
                                       && !ass.FullName.Contains("System")
                                       && !ass.FullName.Contains("Mono.")
                                       && !ass.FullName.Contains("mscorlib")
                                       && !ass.FullName.Contains("netstandard")
                                       && !ass.FullName.Contains("nunit")
                                       && !ass.FullName.Contains("log4net")
                                       && !ass.FullName.Contains("Bee.BeeDriver")
                                       && !ass.FullName.Contains("ReportGeneratorMerged")
                                       && !ass.FullName.Contains("PlayerBuildProgramLibrary.Data")
                                       && !ass.FullName.Contains("ExCSS.Unity")
                                       && !ass.FullName.Contains("unityplastic"))
                            .SelectMany(ass => ass.DefinedTypes
                                                .Where(t => t.GetInterface(typeof(IUserMapper).Name) != null))
                            .Select(t => (IUserMapper) Activator.CreateInstance(t))
                            .ToList();

            UserMappers = mappers; 
        }

        //We cannot pass it as a generic because we do not know it at compile time
        //We also can't return the generic for the same reason
        //We know all the possible variants inside this function,
        //which means we can use type inference to fill in NonGenericTypeMapper.CreateFrom()
        public ITypeMapper FindMapper(Type t)
        {
            /*  
             *  using dynamic avoids having to this for each case: 
             *  var mapper = new Mapper<CaravanV3Wrapper, CaravanV3, Vector3>();
             *  return NonGenericTypeMapper.CreateFrom(mapper);
             *   
             *   We cannot use object as we will not be able to beneficiate from
             *   type inference when calling NonGenericTypeMapper.CreateFrom
             *   
             *   All an all, this avoid either needlessly complex code or using reflection tricks.
             *   We sacrifice a bit of perf for readability, it's a fine tradeoff imho,
             *   as we will not be calling this except when saving/loading data.
             */
            dynamic mapper = null;

            //Abstract lists by creating the mapper if a mapper 
            //was found for the initial type (else we need to do it for each type)
            SwitchToListModeIfNeeded(ref t, out var listMode);

            //AsIsMapper is used for types that
            //will be serialized corretly by Json.NET
            if (t == typeof(Vector3) || t == typeof(CaravanV3))
                mapper = new Mapper<CaravanV3Wrapper, CaravanV3, Vector3>();
            if (t == typeof(string))
                mapper = new AsIsMapper<string>();
            if (t == typeof(Int32) || t == typeof(I32))
                mapper = new Mapper<I32Wrapper, I32, Int32>();
            if (t == typeof(float) || t == typeof(CaravanFloat))
                mapper = new Mapper<CaravanFloatWrapper, CaravanFloat, float>();
            if (t == typeof(double))
                mapper = new AsIsMapper<double>();
            if (t == typeof(bool))
                mapper = new AsIsMapper<bool>();
            if (t == typeof(char) || t == typeof(CaravanChar))
                mapper = new Mapper<CaravanCharWrapper, CaravanChar, char>();
            if (t == typeof(Quaternion) || t == typeof(CaravanQuaternionEuler))
                mapper = new Mapper<CaravanQuaternionWrapper, CaravanQuaternionEuler, Quaternion>();

            if (listMode && mapper != null)
                mapper = MapperHelpers.HandleLists(t, mapper);

            if (mapper != null) return NonGenericTypeMapper.CreateFrom(mapper);

            ITypeMapper userTypeMapper = null;
            foreach (var userMapper in UserMappers)
            {
                userTypeMapper = userMapper.FindUserTypeMapper(t, listMode);
                if (userTypeMapper != null) break;
            }
           
            if (userTypeMapper != null) return userTypeMapper;

            return null;
        }

        private void SwitchToListModeIfNeeded(ref Type t, out bool listMode)
        {
            listMode = false;
            //get List<> in a better way :)
            if (t.IsGenericType && t.Name.Contains("List`1"))
            {
                var generic = t.GetGenericArguments();

                if (generic.Count() == 1)
                {
                    t = generic[0];
                    listMode = true;
                }
            }
        }
    }

    //Public
    public interface ICaravanWrapper<Wrapped, Orig>
    {
        public Wrapped OriginalToWrapped(Orig o);
        public Orig WrappedToOriginal(Wrapped w);
    }

    public class AsIsWrapper<Origin> : ICaravanWrapper<Origin, Origin>
    {
        public Origin OriginalToWrapped(Origin o) => o;
        public Origin WrappedToOriginal(Origin w) => w;
    }

    public interface ITypeMapper<Wrapped, Orig>
    {
        public Orig Deserialize(Wrapped wrapped);
        public Wrapped Serialize(Orig orig);
    }

    public interface ITypeMapper
    {
        public object Deserialize(object wrapped);
        public object Serialize(object orig);
    }

    public class NonGenericTypeMapper : ITypeMapper
    {
        private readonly Func<object, object> _deserialize;
        private readonly Func<object, object> _serialize;

        private NonGenericTypeMapper(Func<object, object> deserialize, Func<object, object> serialize)
        {
            this._deserialize = deserialize;
            this._serialize = serialize;
        }

        //We dont know the type at compile type so we cannot really call / return the generic version
        //So we just wrap it inside a NonGeneric version (which is not type safe)
        //The magic happens inside FindMapper(), since we know the types at compile time here, we can use 
        //type inference to fill the CreateFrom and retrieve our non generic Mapper to pass to the rest
        //of the code that is still clueless about the types it encounters.
        //It's very ugly tho.
        public static NonGenericTypeMapper CreateFrom<Wrapped, Orig>(ITypeMapper<Wrapped, Orig> genericMapper)
        {
            return new NonGenericTypeMapper(
                (object o) => genericMapper.Deserialize((Wrapped)o),
                (object o) => genericMapper.Serialize((Orig)o)
             );
        }

        public object Deserialize(object wrapped) => _deserialize(wrapped);
        public object Serialize(object orig) => _serialize(orig);
    }

    public class AsIsMapper<Origin> : ITypeMapper<Origin, Origin>
    {
        public Origin Deserialize(Origin wrapper) => wrapper;
        public Origin Serialize(Origin orig) => orig;
    }

    public class Mapper<TypeWrapper, Wrapped, Orig> : ITypeMapper<Wrapped, Orig>
                                    where
                                        TypeWrapper : ICaravanWrapper<Wrapped, Orig>, new()
    {
        public Orig Deserialize(Wrapped wrapped) => new TypeWrapper().WrappedToOriginal(wrapped);
        public Wrapped Serialize(Orig orig) => new TypeWrapper().OriginalToWrapped(orig);
    }

    public class ListMapper<TypeWrapper, Wrapped, Orig> : ITypeMapper<List<Wrapped>, List<Orig>>
                                                 where
                                                    TypeWrapper : ICaravanWrapper<Wrapped, Orig>, new()
    {
        public List<Orig> Deserialize(List<Wrapped> wrapped) => wrapped.Select(w => new TypeWrapper().WrappedToOriginal(w)).ToList();
        public List<Wrapped> Serialize(List<Orig> orig) => orig.Select(o => new TypeWrapper().OriginalToWrapped(o)).ToList();
    }

    public static class MapperHelpers
    {
        public static dynamic HandleLists(Type t, dynamic currentMapper)
        {
            dynamic listMapper = null;
            if (currentMapper != null)
            {
                if (currentMapper.GetType().GetGenericArguments().Length == 1) //AsIs
                {
                    var asIsMapper = typeof(AsIsWrapper<>).MakeGenericType(t);
                    var mapperType = typeof(ListMapper<,,>).MakeGenericType(asIsMapper, t, t);
                    listMapper = Activator.CreateInstance(mapperType);
                }
                else //Mapper
                {
                    var types = currentMapper.GetType().GetGenericArguments();
                    var wrapper = types[0];
                    var wrapped = types[1];
                    var orig = types[2];

                    var mapperType = typeof(ListMapper<,,>).MakeGenericType(wrapper, wrapped, orig);
                    listMapper = Activator.CreateInstance(mapperType);
                }
            }

            return listMapper;
        }
    }

    public interface IUserMapper
    {
        ITypeMapper FindUserTypeMapper(Type t, bool isAList);
    }

    public interface IMapperFinder
    {
        //public void SetUserMapper(IUserMapper userMapper);
        public ITypeMapper FindMapper(Type t);
    }
}
