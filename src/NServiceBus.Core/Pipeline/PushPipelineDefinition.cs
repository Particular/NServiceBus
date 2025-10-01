namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Transport;

class PushPipelineDefinition : IPushPipeline
{
    public PushPipelineDefinition(string name) => Name = name;

    public string Name { get; }

    public Func<MessageContext, CancellationToken, Task> OnMessage { get; set; } = (_, _) => Task.CompletedTask;
    public Func<ErrorContext, CancellationToken, Task<ErrorHandleResult>> OnError { get; set; } = (_, _) => Task.FromResult(ErrorHandleResult.Handled);

    public Task Handle(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        return OnMessage(messageContext, cancellationToken);
    }

    public Task<ErrorHandleResult> Handle(ErrorContext errorContext, CancellationToken cancellationToken = default)
    {
        return OnError(errorContext, cancellationToken);
    }
}

public interface IPushPipeline
{
    Task Handle(MessageContext messageContext, CancellationToken cancellationToken = default);
    Task<ErrorHandleResult> Handle(ErrorContext errorContext, CancellationToken cancellationToken = default);
}