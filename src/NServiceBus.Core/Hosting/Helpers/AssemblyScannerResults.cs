namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

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
            Errors = new List<ErrorWhileScanningAssemblies>();
            Assemblies = new List<Assembly>();
            SkippedFiles = new List<SkippedFile>();
        }
        /// <summary>
        /// Dump error to console.
        /// </summary>
        public override string ToString()
        {
            if ((Errors == null) || (Errors.Count < 1)) return string.Empty;
            var sb = new StringBuilder();
            
            foreach (var error in Errors)
            {
                sb.Append(error);
                if (error.Exception is ReflectionTypeLoadException)
                {
                    var e = error.Exception as ReflectionTypeLoadException;
                    if (e.LoaderExceptions.Any())
                    {
                        sb.Append(Environment.NewLine + "Scanned type errors: ");
                        foreach (var ex in e.LoaderExceptions)
                            sb.Append(Environment.NewLine + ex.Message);
                    }
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// List of errors that occurred while attempting to load an assembly
        /// </summary>
        public List<ErrorWhileScanningAssemblies> Errors { get; private set; }
        
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
