#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class ConfigureCriticalErrorAction
    {

        [ObsoleteEx(
            Replacement = "ConfigureCriticalErrorAction.DefineCriticalErrorAction()", 
            Message = "Use `configuration.DefineCriticalErrorAction()`, where configuration is an instance of type `BusConfiguration`",
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
        public static Configure DefineCriticalErrorAction(this Configure config, Action<string, Exception> onCriticalError)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Inject an instace of CriticalError and call CriticalError.Raise", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
        public static void RaiseCriticalError(string errorMessage, Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}