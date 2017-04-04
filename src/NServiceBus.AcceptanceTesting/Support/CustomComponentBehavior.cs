namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class CustomComponentBehavior<TContext> : IEndpointBehavior
        where TContext : ScenarioContext
    {
        string name;
        Func<TContext, CancellationToken, Task> onStart;
        Func<TContext, Task> onStop;
        Func<TContext, CancellationToken, Task> onStarted;

        public CustomComponentBehavior(string name, Func<TContext, CancellationToken, Task> onStart, Func<TContext, Task> onStop, Func<TContext, CancellationToken, Task> onStarted)
        {
            Guard.AgainstNull(nameof(onStart), onStart);
            Guard.AgainstNull(nameof(onStop), onStop);

            this.name = name;
            this.onStart = onStart;
            this.onStop = onStop;
            this.onStarted = onStarted ?? ((c, t) => Task.FromResult(0));
        }

        public Task<IEndpointRunner> CreateRunner(RunDescriptor run)
        {
            return Task.FromResult<IEndpointRunner>(new Runner((TContext)run.ScenarioContext, onStart, onStarted, onStop, name));
        }

        class Runner : IEndpointRunner
        {
            readonly TContext context;
            Func<TContext, CancellationToken, Task> onStart;
            Func<TContext, CancellationToken, Task> onStarted;
            Func<TContext, Task> onStop;
            string name;

            public Runner(TContext context, Func<TContext, CancellationToken, Task> onStart, Func<TContext, CancellationToken, Task> onStarted, Func<TContext, Task> onStop, string name)
            {
                this.context = context;
                this.onStart = onStart;
                this.onStarted = onStarted;
                this.onStop = onStop;
                this.name = name;
            }

            public bool FailOnErrorMessage => false;

            public Task Start(CancellationToken token)
            {
                return onStart(context, token);
            }

            public Task Whens(CancellationToken token)
            {
                return onStarted(context, token);
            }

            public Task Stop()
            {
                return onStop(context);
            }

            public string Name()
            {
                return name;
            }
        }
    }
}