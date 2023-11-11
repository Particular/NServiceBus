namespace NServiceBus.AcceptanceTesting.Support;

using System.Threading;
using System.Threading.Tasks;

public abstract class ComponentRunner
{
    public abstract string Name { get; }

    public virtual Task Start(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public virtual Task ComponentsStarted(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public virtual Task Stop(CancellationToken cancellationToken = default) => Task.CompletedTask;
}