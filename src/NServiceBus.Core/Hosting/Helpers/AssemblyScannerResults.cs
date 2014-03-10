namespace NServiceBus.Hosting.Helpers
{
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Holds GetScannableAssemblies results.
    /// Contains list of errors and list of scan-able assemblies.
    /// </summary>
    public class AssemblyScannerResults 
    {
        /// <summary>
        /// Constructor to initialize AssemblyScannerResults
        /// </summary>
        public AssemblyScannerResults()
        {
            Assemblies = new List<Assembly>();
            SkippedFiles = new List<SkippedFile>();
        }
       
        /// <summary>
        /// List of successfully found and loaded assemblies
        /// </summary>
        public List<Assembly> Assemblies { get; private set; }
        
        /// <summary>
        /// List of files that were skipped while scanning because they were a) explicitly excluded
        /// by the user, b) not a .NET DLL, or c) not referencing NSB and thus not capable of implementing
        /// <see cref="IHandleMessages{T}"/>
        /// </summary>
        public List<SkippedFile> SkippedFiles { get; private set; }
    }
}
