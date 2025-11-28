#nullable enable
namespace NServiceBus.Sagas;

using System;
using System.Threading.Tasks;

interface ISagaNotFoundHandlerInvocation
{
    Task Invoke(IServiceProvider serviceProvider, object message, IMessageProcessingContext context);
}