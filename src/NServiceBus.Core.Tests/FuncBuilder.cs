﻿namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ObjectBuilder;

    public class FuncBuilder : IBuilder
    {
        readonly IList<Tuple<Type, Func<object>>> funcs = new List<Tuple<Type, Func<object>>>();

        public void Dispose()
        {

        }
        public void Register<T>()
        {
            Register(typeof(T), ()=>Activator.CreateInstance<T>());
        }

        public void Register(Type t)
        {
            Register(t, () => Activator.CreateInstance(t));
        }
        public void Register<T>(Func<object> func)
        {
            Register(typeof(T), func);
        }

        public void Register(Type t, Func<object> func)
        {
            funcs.Add(new Tuple<Type, Func<object>>(t, func));
        }

        public object Build(Type typeToBuild)
        {
            try
            {
                var fn = funcs.FirstOrDefault(f => f.Item1 == typeToBuild);
          
                if (fn == null)
                {
                    var @interface = typeToBuild.GetInterfaces().FirstOrDefault();
                    if (@interface != null)
                    {
                        fn = funcs.FirstOrDefault(f => f.Item1 == @interface);
                    }
                }

                object result;

                if (fn != null)
                {
                    result = fn.Item2();                    
                }
                else
                {
                    result = Activator.CreateInstance(typeToBuild);
                }

                //enable property injection
                result.GetType().GetProperties()
                    .Select(p => p.PropertyType)
                    .Intersect(funcs.Select(f => f.Item1)).ToList()
                    .ForEach(propertyTypeToSet => result.GetType().GetProperties().First(p => p.PropertyType == propertyTypeToSet)
                                                      .SetValue(result, Build(propertyTypeToSet), null));

                return result;

            }
            catch (Exception ex)
            {
                
                throw new Exception("Failed to build type: " + typeToBuild,ex);
            }
        }

        public IBuilder CreateChildBuilder()
        {
            return this;
        }

        public T Build<T>()
        {
            try
            {
                return (T) Build(typeof(T));
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not build {0}", typeof(T)), exception);
            }
        }

        public IEnumerable<T> BuildAll<T>()
        {
            return funcs.Where(f => f.Item1 == typeof(T))
                .Select(f => (T)f.Item2())
                .ToList();
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return funcs.Where(f => f.Item1 == typeToBuild)
                .Select(f => f.Item2())
                .ToList();
        }

        public void Release(object instance)
        {
            
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            var obj = Build(typeToBuild);

            action(obj);
        }
    }
}
