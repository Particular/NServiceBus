namespace NServiceBus.AcceptanceTesting.Support;

using System.Threading;
using System.Threading.Tasks;

// in the next major version we quite likely want to start using a full generic host
// in the acceptance test and no longer require something that "mimics" IHostedService via a combination
// of ComponentRunner and stoppable endpoints.
public interface IStoppableEndpointInstance
{
    IMessageSession MessageSession { get; }

    Task Stop(CancellationToken cancellationToken = default);
}