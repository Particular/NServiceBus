namespace NServiceBus.AcceptanceTesting.Support
{
    using System.Threading.Tasks;

    /// <summary>
    /// Provide a mechanism in acceptance tests for transports and persistences
    /// to configure an endpoint for a test and then clean up afterwards.
    /// </summary>
    public interface IConfigureEndpointTestExecution
    {
        /// <summary>
        /// Gives the transport/persistence a chance to configure before the test starts.
        /// </summary>
        /// <param name="endpointName">The endpoint name.</param>
        /// <param name="configuration">The EndpointConfiguration instance.</param>
        /// <param name="settings">Settings from the RunDescriptor specifying Transport, Persistence,
        /// connection strings, Serializer, Builder, and other details. Transports must call configuration.UseTransport&lt;T&gt;().
        /// Persistence must call configuration.UsePersistence&lt;T&gt;(). </param>
        /// <param name="publisherMetadata">Metadata about publishers and the events they own.</param>
        /// <returns>An async Task.</returns>
        Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata);

        /// <summary>
        /// Gives the transport/persistence a chance to clean up after the test is complete. Implementations of this class may store
        /// private variables during Configure to use during the cleanup phase.
        /// </summary>
        /// <returns>An async Task.</returns>
        Task Cleanup();
    }
}
