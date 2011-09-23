using System;
using System.Collections.Generic;

namespace NServiceBus.Hosting
{
    public class EndpointsEventArgs : EventArgs
    {
        public IEnumerable<EndpointToHost> Endpoints { get; set; }
    }
}