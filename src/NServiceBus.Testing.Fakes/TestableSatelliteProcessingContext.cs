// ReSharper disable PartialTypeWithSinglePart
namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Transport;

    /// <summary>
    /// A testable implementation of <see cref="ISatelliteProcessingContext" />.
    /// </summary>

    public partial class TestableSatelliteProcessingContext : TestableBehaviorContext, ISatelliteProcessingContext
    {
        /// <summary>
        /// The physical message being processed.
        /// </summary>
        public IncomingMessage Message { get; set; } = new IncomingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);
    }
}