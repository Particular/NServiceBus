namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Hosting.Helpers;

    /// <summary>
    /// Configure the <see cref="AssemblyScanner"/>.
    /// </summary>
    public class AssemblyScannerConfiguration
    {
        /// <summary>
        /// Defines whether assemblies loaded into the current <see cref="AppDomain"/> should be scanned. Default value is <code>true</code>.
        /// </summary>
        public bool ScanAppDomainAssemblies { get; set; } = true;

        /// <summary>
        /// Defines whether exceptions occurring during assembly scanning should be rethrown or ignored. Default value is <code>true</code>.
        /// </summary>
        public bool ThrowExceptions { get; set; } = true;

        /// <summary>
        /// Defines whether nested directories should be included in the assembly scanning process. Default value is <code>false</code>.
        /// </summary>
        public bool ScanAssembliesInNestedDirectories { get; set; }

        /// <summary>
        /// Defines a custom path for assembly scanning.
        /// </summary>
        public string CustomAssemblyScanningPath { get; set; }

        /// <summary>
        /// A list of <see cref="Assembly" />s to ignore in the assembly scanning.
        /// </summary>
        /// <param name="assemblies">The file name of the assembly.</param>
        public void ExcludeAssemblies(params string[] assemblies)
        {
            Guard.AgainstNull(nameof(assemblies), assemblies);

            if (assemblies.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Passed in a null or empty assembly name.", nameof(assemblies));
            }

            ExcludedAssemblies.AddRange(assemblies);
        }

        /// <summary>
        /// A list of <see cref="Type" />s to ignore in the assembly scanning.
        /// </summary>
        public void ExcludeTypes(params Type[] types)
        {
            Guard.AgainstNull(nameof(types), types);
            if (types.Any(x => x == null))
            {
                throw new ArgumentException("Passed in a null or empty type.", nameof(types));
            }

            ExcludedTypes.AddRange(types);
        }

        internal List<string> ExcludedAssemblies { get; } = new List<string>(0);
        internal List<Type> ExcludedTypes { get; } = new List<Type>(0);
    }
}
