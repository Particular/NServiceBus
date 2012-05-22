﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace NServiceBus
{
    /// <summary>
    /// Class for specifying which assemblies not to load.
    /// </summary>
    public class AllAssemblies : IExcludesBuilder, IIncludesBuilder
    {

        /// <summary>
        /// Indicate that assemblies matching the given expression are not to be used.
        /// Use the 'And' method to indicate other assemblies to be skipped.
        /// </summary>
        /// <param name="assemblyExpression"><see cref="Configure.IsMatch"/></param>
        /// <seealso cref="Configure.IsMatch"/>
        /// <returns></returns>
        public static IExcludesBuilder Except(string assemblyExpression)
        {
            return new AllAssemblies { assembliesToExclude = new List<string>(new[] { assemblyExpression })};
        }

        /// <summary>
        /// Indicate that assemblies matching the given expression are to be used.
        /// Use the 'And' method to indicate other assemblies to be included.
        /// </summary>
        /// <param name="assemblyExpression"><see cref="Configure.IsMatch"/></param>
        /// <seealso cref="Configure.IsMatch"/>
        /// <returns></returns>
        public static IIncludesBuilder Matching(string assemblyExpression)
        {
            return new AllAssemblies { assembliesToInclude = new List<string>(new[] { assemblyExpression }) };
        }

        IExcludesBuilder IExcludesBuilder.And(string assemblyExpression)
        {
            if (!assembliesToExclude.Contains(assemblyExpression))
                assembliesToExclude.Add(assemblyExpression);

            return this;
        }

        IExcludesBuilder IIncludesBuilder.Except(string assemblyExpression)
        {
            if (assembliesToExclude == null)
                assembliesToExclude = new List<string>();

            if (!assembliesToExclude.Contains(assemblyExpression))
                assembliesToExclude.Add(assemblyExpression);

            return this;
        }

        IIncludesBuilder IIncludesBuilder.And(string assemblyExpression)
        {
            if (!assembliesToInclude.Contains(assemblyExpression))
                assembliesToInclude.Add(assemblyExpression);

            return this;
        }

        /// <summary>
        /// Returns an enumerator for looping over the assemblies to be loaded.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Assembly> GetEnumerator()
        {
            Predicate<string> exclude = assembliesToExclude != null 
                ? name => assembliesToExclude.Any(skip => Configure.IsMatch(skip, name))
                : (Predicate<string>) null;

            Predicate<string> include = assembliesToInclude != null
                ? name => assembliesToInclude.Any(skip => Configure.IsMatch(skip, name))
                : (Predicate<string>) null;

            return Configure.FindAssemblies(directory, true, include, exclude).GetEnumerator();
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
				directory = HttpRuntime.BinDirectory;
            else
                directory = AppDomain.CurrentDomain.BaseDirectory;
        }

        private List<string> assembliesToExclude;
        private List<string> assembliesToInclude;
        private readonly string directory;
    }
}
