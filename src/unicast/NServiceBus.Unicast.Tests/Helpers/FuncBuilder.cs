namespace NServiceBus.Unicast.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.ObjectBuilder;

    public class FuncBuilder : IBuilder
    {
        IList<Tuple<Type, Func<object>>> funcs = new List<Tuple<Type, Func<object>>>();
        public void Dispose()
        {

        }

        public void Register<T>(Func<object> func)
        {
            Register(typeof(T),func);
        }

        public void Register(Type t,Func<object> func)
        {
            funcs.Add(new Tuple<Type, Func<object>>(t, func));
        }

        public object Build(Type typeToBuild)
        {
            var obj = funcs.First(f => f.Item1 == typeToBuild).Item2();

            //enable property injection
            obj.GetType().GetProperties()
                .Select(p=>p.PropertyType)
                .Intersect(funcs.Select(f=>f.Item1)).ToList()
                .ForEach(propertyTypeToSet=>
                             {
                                 obj.GetType().GetProperties().First(p=>p.PropertyType == propertyTypeToSet)
                                     .SetValue(obj, Build(propertyTypeToSet), null);
                             });

            return obj;
        }

        public IBuilder CreateChildBuilder()
        {
            return this;
        }

        public T Build<T>()
        {
            return (T)Build(typeof(T));
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

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            var obj = Build(typeToBuild);

            action(obj);
        }
		
        public void Release(object instance)
        {            
        }

        public void Release(IEnumerable<object> instances)
        {            
        }		
    }
}