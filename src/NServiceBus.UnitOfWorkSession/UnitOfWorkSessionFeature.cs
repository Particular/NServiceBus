namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Outbox;
    using Persistence;
    using Pipeline;
    using Transport;

    public class UnitOfWorkSessionFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            // check if not send only
            // check for outbox

            factory = new UnitOfWorkMessageSessionFactory();

            context.RegisterStartupTask(serviceProvider => new RegisterSessionStartupTask(factory,
                serviceProvider.GetRequiredService<IMessageDispatcher>(),
                serviceProvider.GetRequiredService<IOutboxStorage>(),
                serviceProvider.GetRequiredService<ISynchronizedStorageAdapter>(),
                serviceProvider.GetRequiredService<ISynchronizedStorage>()));
        }

        UnitOfWorkMessageSessionFactory? factory;

        class RegisterSessionStartupTask : FeatureStartupTask
        {
            readonly UnitOfWorkMessageSessionFactory factory;
            readonly IMessageDispatcher messageDispatcher;
            readonly IOutboxStorage outboxStorage;
            readonly ISynchronizedStorageAdapter synchronizedStorageAdapter;
            readonly ISynchronizedStorage synchronizedStorage;

            public RegisterSessionStartupTask(UnitOfWorkMessageSessionFactory factory, IMessageDispatcher messageDispatcher, IOutboxStorage outboxStorage, ISynchronizedStorageAdapter synchronizedStorageAdapter, ISynchronizedStorage synchronizedStorage)
            {
                this.outboxStorage = outboxStorage;
                this.messageDispatcher = messageDispatcher;
                this.factory = factory;
                this.synchronizedStorageAdapter = synchronizedStorageAdapter;
                this.synchronizedStorage = synchronizedStorage;
            }

            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                factory.Initialize(session, messageDispatcher, outboxStorage, synchronizedStorageAdapter, synchronizedStorage);
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }

    class InterceptOutgoingPublishesBehavior : IBehavior<IOutgoingPublishContext, IOutgoingPublishContext>
    {
        public Task Invoke(IOutgoingPublishContext context, Func<IOutgoingPublishContext, Task> next)
        {
            throw new NotImplementedException();
        }
    }

    class InterceptOutgoingSendsBehavior : IBehavior<IOutgoingSendContext, IOutgoingSendContext>
    {
        public Task Invoke(IOutgoingSendContext context, Func<IOutgoingSendContext, Task> next)
        {
            throw new NotImplementedException();
        }
    }

    class InterceptOutgoingRepliesBehavior : IBehavior<IOutgoingReplyContext, IOutgoingReplyContext>
    {
        public Task Invoke(IOutgoingReplyContext context, Func<IOutgoingReplyContext, Task> next)
        {
            throw new NotImplementedException();
        }
    }
}