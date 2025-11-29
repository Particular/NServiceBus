#nullable enable
namespace NServiceBus.Features;

using System;
using System.Threading;
using System.Threading.Tasks;

interface IFeatureStartupTaskController
{
    string Name { get; }
    Task Start(IServiceProvider builder, IMessageSession messageSession, CancellationToken cancellationToken = default);
    Task Stop(IMessageSession messageSession, CancellationToken cancellationToken = default);
}