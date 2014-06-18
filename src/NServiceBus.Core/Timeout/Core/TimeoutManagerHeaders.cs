namespace NServiceBus.Timeout
{
    /// <summary>
    /// A collection of well known message headers used to control message timeouts
    /// </summary>
    public class TimeoutManagerHeaders
    {
        public const string Expire = "NServiceBus.Timeout.Expire";
        public const string RouteExpiredTimeoutTo = "NServiceBus.Timeout.RouteExpiredTimeoutTo";
        public const string ClearTimeouts = "NServiceBus.ClearTimeouts";
    }
}