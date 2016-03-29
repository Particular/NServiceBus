namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Routing;

    class TimeoutManagerRoutingStrategy : RoutingStrategy
    {
        string timeoutManagerAddress;
        string ultimateDestination;
        DateTime deliverAt;

        public TimeoutManagerRoutingStrategy(string timeoutManagerAddress, string ultimateDestination, DateTime deliverAt)
        {
            this.ultimateDestination = ultimateDestination;
            this.deliverAt = deliverAt;
            this.timeoutManagerAddress = timeoutManagerAddress;
        }

        public override AddressTag Apply(Dictionary<string, string> headers)
        {
            headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo] = ultimateDestination;
            headers[TimeoutManagerHeaders.Expire] = DateTimeExtensions.ToWireFormattedString(deliverAt);

            return new UnicastAddressTag(timeoutManagerAddress);
        }
    }
}