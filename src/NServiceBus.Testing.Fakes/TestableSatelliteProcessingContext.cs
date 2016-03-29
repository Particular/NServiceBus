namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// A testable implementation of <see cref="ISatelliteProcessingContext" />.
    /// </summary>
    public class TestableSatelliteProcessingContext : TestableBehaviorContext, ISatelliteProcessingContext
    {
        /// <summary>
        /// The physical message being processed.
        /// </summary>
        public IncomingMessage Message { get; set; } = new IncomingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), Stream.Null);
    }
}