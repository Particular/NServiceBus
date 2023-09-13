namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Holds <see cref="AssemblyScanner.GetScannableAssemblies" /> results.
    /// Contains list of errors and list of scannable assemblies.
    /// </summary>
    public class AssemblyScannerResults
    {
        /// <summary>
        /// Constructor to initialize AssemblyScannerResults.
        /// </summary>
        public AssemblyScannerResults()
        {
        }

        /// <summary>
        /// List of successfully found and loaded assemblies.
        /// </summary>
        public List<Assembly> Assemblies { get; private set; } = new();

        /// <summary>
        /// List of files that were skipped while scanning because they were a) explicitly excluded
        /// by the user, b) not a .NET DLL, or c) not referencing NSB and thus not capable of implementing
        /// <see cref="IHandleMessages{T}" />.
        /// </summary>
        public List<SkippedFile> SkippedFiles { get; } = new();

        /// <summary>
        /// True if errors where encountered during assembly scanning.
        /// </summary>
        public bool ErrorsThrownDuringScanning { get; internal set; }

        /// <summary>
        /// List of types.
        /// </summary>
        public List<Type> Types { get; private set; } = new();

        internal void RemoveDuplicates()
        {
            Assemblies = Assemblies.Distinct().ToList();
            Types = Types.Distinct().ToList();
        }
    }
}