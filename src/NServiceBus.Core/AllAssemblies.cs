namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Web;
    using Hosting.Helpers;

    /// <summary>
    /// Class for specifying which assemblies not to load.
    /// </summary>
    public class AllAssemblies : IExcludesBuilder, IIncludesBuilder
    {
        /// <summary>
        /// Indicate that assemblies matching the given expression are not to be used.
        /// Use the 'And' method to indicate other assemblies to be skipped.
        /// </summary>
        public static IExcludesBuilder Except(string assemblyExpression)
        {
            return new AllAssemblies
                   {
                       assembliesToExclude =
                       {
                           assemblyExpression
                       }
                   };
        }

        /// <summary>
        /// Indicate that assemblies matching the given expression are to be used.
        /// Use the 'And' method to indicate other assemblies to be included.
        /// </summary>
        public static IIncludesBuilder Matching(string assemblyExpression)
        {
            return new AllAssemblies
                   {
                       assembliesToInclude =
                       {
                           assemblyExpression
                       }
                   };
        }

        IExcludesBuilder IExcludesBuilder.And(string assemblyExpression)
        {
            if (!assembliesToExclude.Contains(assemblyExpression))
            {
                assembliesToExclude.Add(assemblyExpression);
            }

            return this;
        }

        IExcludesBuilder IIncludesBuilder.Except(string assemblyExpression)
        {
            if (!assembliesToExclude.Contains(assemblyExpression))
            {
                assembliesToExclude.Add(assemblyExpression);
            }

            return this;
        }

        IIncludesBuilder IIncludesBuilder.And(string assemblyExpression)
        {
            if (!assembliesToInclude.Contains(assemblyExpression))
            {
                assembliesToInclude.Add(assemblyExpression);
            }

            return this;
        }

        /// <summary>
        /// Returns an enumerator for looping over the assemblies to be loaded.
        /// </summary>
        public IEnumerator<Assembly> GetEnumerator()
        {
            var assemblyScanner = new AssemblyScanner(directory)
            {
                IncludeAppDomainAssemblies = true,
                AssembliesToInclude = assembliesToInclude,
                AssembliesToSkip = assembliesToExclude
            };
            assemblyScanner.MustReferenceAtLeastOneAssembly.Add(typeof(IHandleMessages<>).Assembly);

            return assemblyScanner
                .GetScannableAssemblies()
                .Assemblies
                .GetEnumerator();
        }

        /// <summary>
        /// Return a non-generic enumerator.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        AllAssemblies()
        {
            var isWebApplication = HttpRuntime.AppDomainAppId != null;

            directory = isWebApplication
                            ? HttpRuntime.BinDirectory
                            : AppDomain.CurrentDomain.BaseDirectory;
        }

        string directory;
        List<string> assembliesToExclude = new List<string>();
        List<string> assembliesToInclude = new List<string>();
    }
}
