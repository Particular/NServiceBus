#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus.SecondLevelRetries.Helpers
{
    using System;

    [ObsoleteEx(
        Message = "Access the `TransportMessage.Headers` dictionary directly",
        RemoveInVersion = "6.0",
        TreatAsErrorFromVersion = "5.0")]
    public static class TransportMessageHelpers
    {
        [ObsoleteEx(
            Message = "Access the `TransportMessage.Headers` dictionary directly using the `FaultsHeaderKeys.FailedQ` key",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public static Address GetAddressOfFaultingEndpoint(TransportMessage message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Access the `TransportMessage.Headers` dictionary directly",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public static string GetHeader(TransportMessage message, string key)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Access the `TransportMessage.Headers` dictionary directly",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public static bool HeaderExists(TransportMessage message, string key)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Access the `TransportMessage.Headers` dictionary directly",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public static void SetHeader(TransportMessage message, string key, string value)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Access the `TransportMessage.Headers` dictionary directly using the `Headers.Retries` key",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public static int GetNumberOfRetries(TransportMessage message)
        {
            throw new NotImplementedException();
        }
    }
}