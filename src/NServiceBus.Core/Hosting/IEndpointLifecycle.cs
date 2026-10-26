#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

interface IEndpointLifecycle : IAsyncDisposable
{
    ValueTask Create(CancellationToken cancellationToken = default);
    ValueTask Start(CancellationToken cancellationToken = default);
    ValueTask Stop(CancellationToken cancellationToken = default);
    ValueTask<RunningEndpointInstance> CreateAndStart(CancellationToken cancellationToken = default);
}