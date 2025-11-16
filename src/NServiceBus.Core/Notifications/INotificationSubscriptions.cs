#nullable enable

namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;

interface INotificationSubscriptions<in TEvent>
{
    Task Raise(TEvent @event, CancellationToken cancellationToken = default);
}