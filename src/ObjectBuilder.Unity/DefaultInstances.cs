namespace NServiceBus.ObjectBuilder.Unity
{
    using System;
    using System.Collections.Generic;

    static class DefaultInstances
    {
        static readonly HashSet<Type> typesWithDefaultInstances = new HashSet<Type>();

        public static bool Contains(Type type)
        {
            return typesWithDefaultInstances.Contains(type);
        }

        public static void Add(Type type)
        {
            typesWithDefaultInstances.Add(type);
        }

        public static void Clear()
        {
            typesWithDefaultInstances.Clear();
        }
    }
}