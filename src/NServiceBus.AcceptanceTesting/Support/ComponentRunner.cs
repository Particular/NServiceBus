namespace NServiceBus.AcceptanceTesting.Support
{
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class ComponentRunner
    {
        public abstract string Name { get; }

        public virtual Task Start(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public virtual Task ComponentsStarted(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public virtual Task Stop() => Task.FromResult(0);
    }
}