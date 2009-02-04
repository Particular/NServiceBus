using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace NServiceBus
{
    /// <summary>
    /// Used by ConfigUnicastBus to indicate the order in which
    /// handler types are to run.
    /// 
    /// Not thread safe.
    /// </summary>
    /// <typeparam name="T">The type which will run first.</typeparam>
    public class First<T>
    {
        /// <summary>
        /// Specifies the type which will run next.
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
        /// Returns the ordered list of types specified.
        /// </summary>
        public IEnumerable<Type> Types
        {
            get { return types; }
        }

        private void Add<TYPE>()
        {
            if (!types.Contains(typeof(TYPE)))
                types.Add(typeof(TYPE));
        }

        private IList<Type> types = new List<Type>();

        private static First<T> instance;
    }
}
