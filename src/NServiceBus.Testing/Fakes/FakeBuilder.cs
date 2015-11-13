namespace NServiceBus.Testing.Fakes
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;

    public class FakeBuilder : IBuilder
    {
        private Dictionary<Type, object> registeredTypes = new Dictionary<Type, object>();

        public void Register<T>(T instance)
        {
            registeredTypes.Add(typeof(T), instance);
        }

        public void Dispose()
        {
        }

        public object Build(Type typeToBuild)
        {
            if (!registeredTypes.ContainsKey(typeToBuild))
            {
                throw new InvalidOperationException($"no instance implementing {typeToBuild} has been registered.");
            }

            return registeredTypes[typeToBuild];
        }

        public IBuilder CreateChildBuilder()
        {
            throw new NotSupportedException();
        }

        public T Build<T>()
        {
            return (T)Build(typeof(T));
        }

        public IEnumerable<T> BuildAll<T>()
        {
            throw new NotSupportedException();
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            throw new NotSupportedException();
        }

        public void Release(object instance)
        {
            throw new NotSupportedException();
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            throw new NotSupportedException();
        }
    }
}