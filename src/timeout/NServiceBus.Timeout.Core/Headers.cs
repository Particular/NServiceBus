namespace NServiceBus.Timeout.Core
{
    public class Headers
    {
        public static string SagaId = "NServiceBus.Timeout.SagaId";
        public static string ClearTimeout = "NServiceBus.Timeout.ClearTimeout";
        public static string Expire = "NServiceBus.Timeout.Expire";
        public static string IsTimeoutMessage = "NServiceBus.Timeout";

    }
}