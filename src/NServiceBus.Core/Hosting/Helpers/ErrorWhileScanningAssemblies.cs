namespace NServiceBus.Hosting.Helpers
{
    using System;

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