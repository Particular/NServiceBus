namespace NServiceBus.TransportTests;

using System.Threading;
using System.Threading.Tasks;
using Transport;

/// <summary>
/// Provide a mechanism in components tests for transports
/// to configure a transport infrastructure for a test and then clean up afterwards.
/// </summary>
public interface IConfigureTransportInfrastructure
{
    /// <summary>
    /// Gets the transport specific queue name to use as the input queue for the test. 
    /// </summary>
    string GetInputQueueName(string testName, TransportTransactionMode transactionMode) => $"{testName}{transactionMode}";

    /// <summary>
    /// Gets the transport specific error queue name to use as the input queue for the test. 
    /// </summary>
    string GetErrorQueueName(string testName, TransportTransactionMode transactionMode) => $"{testName}{transactionMode}.error";

    /// <summary>
    /// Creates a <see cref="TransportDefinition"/> instance used for running a test case.
    /// </summary>
    TransportDefinition CreateTransportDefinition();

    /// <summary>
    /// Gives the transport a chance to configure before the test starts.
    /// </summary>
    Task<TransportInfrastructure> Configure(TransportDefinition transportDefinition, HostSettings hostSettings, QueueAddress inputQueueName, string errorQueueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gives the transport chance to clean up after the test is complete. Implementations of this class may store
    /// private variables during Configure to use during the cleanup phase.
    /// </summary>
    /// <returns>An async Task.</returns>
    Task Cleanup(CancellationToken cancellationToken = default);
}