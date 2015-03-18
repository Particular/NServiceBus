namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ObjectBuilder;
    using ObjectBuilder.Common;

    public class FuncBuilder : IBuilder,IContainer
    {
        readonly IList<Tuple<Type, Func<object>>> funcs = new List<Tuple<Type, Func<object>>>();

        public void Dispose()
        {

        }
        public void Register<T>() where T : new()
        {
            Register(typeof(T), ()=> new T());
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
                var propertyInfos = result.GetType().GetProperties().Where(pi => pi.CanWrite).Where(pi => pi.PropertyType != result.GetType());
                var propsWithoutFuncs = propertyInfos
                    .Select(p => p.PropertyType)
                    .Intersect(funcs.Select(f => f.Item1)).ToList();

                propsWithoutFuncs.ForEach(propertyTypeToSet => propertyInfos.First(p => p.PropertyType == propertyTypeToSet)
                                                      .SetValue(result, Build(propertyTypeToSet), null));

                return result;

            }
            catch (Exception ex)
            {
                throw new Exception("Failed to build type: " + typeToBuild,ex);
            }
        }

        public IContainer BuildChildContainer()
        {
            throw new NotImplementedException();
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

        public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            Register(component);
        }

        public void Configure<T>(Func<T> component, DependencyLifecycle dependencyLifecycle)
        {
            throw new NotImplementedException();
        }

        public void ConfigureProperty(Type component, string property, object value)
        {
            
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            Register(lookupType,()=>instance);
        }

        public bool HasComponent(Type componentType)
        {
            throw new NotImplementedException();
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
