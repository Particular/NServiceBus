using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Web;

namespace NServiceBus
{
    /// <summary>
    /// Class for specifying which assemblies not to load.
    /// </summary>
    public class AllAssemblies : IEnumerable<Assembly>
    {
        /// <summary>
        /// Indicate that the given assembly is not to be used.
        /// Use the 'And' method to indicate other assemblies to be skipped.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static AllAssemblies Except(string assembly)
        {
            return new AllAssemblies { assembliesToSkip = new List<string>(new[] { assembly })};
        }

        /// <summary>
        /// Indicate that the given assembly should not be used.
        /// You can call this method multiple times.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public AllAssemblies And(string assembly)
        {
            if (!assembliesToSkip.Contains(assembly))
                assembliesToSkip.Add(assembly);

            return this;
        }

        /// <summary>
        /// Returns an enumerator for looping over the assemblies to be loaded.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Assembly> GetEnumerator()
        {
            return Configure.GetAssembliesInDirectory(directory, assembliesToSkip.ToArray()).GetEnumerator();
        }

        /// <summary>
        /// Return a non-generic enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private AllAssemblies()
        {
            if (HttpContext.Current != null)
                directory = AppDomain.CurrentDomain.DynamicDirectory;
            else
                directory = AppDomain.CurrentDomain.BaseDirectory;
        }

        private List<string> assembliesToSkip;
        private readonly string directory;
    }
}
