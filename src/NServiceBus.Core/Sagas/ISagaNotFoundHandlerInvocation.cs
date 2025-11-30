#nullable enable
namespace NServiceBus;

using System;
using System.Threading.Tasks;

interface ISagaNotFoundHandlerInvocation
{
    Task Invoke(IServiceProvider serviceProvider, object message, IMessageProcessingContext context);
}