using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace NServiceBus
{
    /// <summary>
    /// Used by ConfigUnicastBus to indicate the order in which
    /// handler assemblies are to run.
    /// 
    /// Not thread safe.
    /// </summary>
    /// <typeparam name="T">The type whose assembly will run first.</typeparam>
    public class First<T>
    {
        /// <summary>
        /// Specifies the type whose assembly will run next.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <returns></returns>
        public static First<T> Then<K>()
        {
            if (instance == null)
                instance = new First<T>();

            instance.Add<T>();
            instance.Add<K>();

            return instance;
        }

        /// <summary>
        /// Returns the list of assemblies specified.
        /// </summary>
        public IEnumerable<Assembly> Assemblies
        {
            get { return assemblies; }
        }

        private void Add<TYPE>()
        {
            if (!assemblies.Contains(typeof(TYPE).Assembly))
                assemblies.Add(typeof(TYPE).Assembly);
        }

        private IList<Assembly> assemblies = new List<Assembly>();

        private static First<T> instance;
    }
}
