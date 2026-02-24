#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

interface IEndpointStarter : IAsyncDisposable
{
    ValueTask<IEndpointInstance> GetOrStart(CancellationToken cancellationToken = default);
}