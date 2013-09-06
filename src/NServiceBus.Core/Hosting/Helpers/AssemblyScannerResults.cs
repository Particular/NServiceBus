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
                sb.Append(error.ToString());
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
        /// List of errors that occurred during 
        /// </summary>
        public List<ErrorWhileScanningAssemblies> Errors { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<Assembly> Assemblies { get; set; }
    }
    /// <summary>
    /// Error information that occurred while scanning assemblies.
    /// </summary>
    public class ErrorWhileScanningAssemblies
    {
        /// <summary>
        /// Adding an error
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="errorMessage"></param>
        internal ErrorWhileScanningAssemblies(Exception ex, string errorMessage) 
        {
            Exception = ex;
            ErrorMessage = errorMessage;
        }
        /// <summary>
        /// Convert to string errors while scanning assemblies
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ErrorMessage + Environment.NewLine + Exception;
        }
        /// <summary>
        /// Exception message.
        /// </summary>
        internal string ErrorMessage { get; private set; }
        /// <summary>
        /// Exception that occurred.
        /// </summary>
        internal Exception Exception { get; private set; }
    }
}
