// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class ConfigureCriticalErrorAction
    {

        /// <summary>
        ///     Sets the function to be used when critical error occurs.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="onCriticalError">Assigns the action to perform on critical error.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(Replacement = "Configure.With(c=>.DefineCriticalErrorAction())", RemoveInVersion = "6.0",TreatAsErrorFromVersion = "5.0")]
        public static Configure DefineCriticalErrorAction(this Configure config, Action<string, Exception> onCriticalError)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Execute the configured Critical error action. The action will be performed on a separate thread
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The critical exception thrown.</param>
        [ObsoleteEx(Replacement = "Inject an instace of CriticalError and call CriticalError.Raise", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static void RaiseCriticalError(string errorMessage, Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}