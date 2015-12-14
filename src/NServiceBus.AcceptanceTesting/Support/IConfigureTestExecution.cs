namespace NServiceBus.AcceptanceTesting.Support
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Provide a mechanism in acceptence tests for transports and persistences 
    /// to configure an endpoint for a test and then clean up afterwards.
    /// </summary>
    public interface IConfigureTestExecution
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration">The BusConfiguration instance.</param>
        /// <param name="settings">A string dictionary of settings from the RunDescriptor specifying Transport, Persistence,
        /// connection strings, Serializer, Builder, and other details. Transports must call configuration.UseTransport&lt;T&gt;().
        /// Persistence must call configuration.UsePersistence&lt;T&gt;(). </param>
        /// <returns>An async Task.</returns>
        Task Configure(BusConfiguration configuration, IDictionary<string, string> settings);

        /// <summary>
        /// Gives the transport/persistence a chance to clean up after the test is complete. Implementors of this class may store
        /// private variables during Configure to use during the cleanup phase.
        /// </summary>
        /// <returns>An async Task.</returns>
        Task Cleanup();
    }
}
