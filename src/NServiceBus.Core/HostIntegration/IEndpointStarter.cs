#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

interface IEndpointStarter : IAsyncDisposable
{
    string ServiceKey { get; }

    ValueTask<IEndpointInstance> GetOrStart(CancellationToken cancellationToken = default);
}