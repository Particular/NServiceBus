namespace NServiceBus.Testing
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Pipeline;
    using Transport;

    /// <summary>
    /// A testable implementation of <see cref="IBatchDispatchContext" />.
    /// </summary>
    public partial class TestableBatchDispatchContext : TestableBehaviorContext, IBatchDispatchContext
    {
        /// <summary>
        /// The captured transport operations to dispatch.
        /// </summary>
        public IList<TransportOperation> Operations { get; set; } = new List<TransportOperation>();

        IReadOnlyCollection<TransportOperation> IBatchDispatchContext.Operations => new ReadOnlyCollection<TransportOperation>(Operations);
    }
}