#if INCLUDE_SAGA
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Sagas;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Messages;

[HandlerRegistryContributor]
public sealed class ContributedSagaRegistry : IHandlerRegistryContributor<ContributedSagaRegistry>
{
    public static ContributedSagaRegistry Instance { get; } = new();

    public void AddHandlers(EndpointConfiguration configuration)
    {
    }

    public void AddSagas(EndpointConfiguration configuration)
    {
        var settings = configuration.GetSettings();
        var associatedMessages = new SagaMessage[]
        {
            new(typeof(CompleteContributedSagaCommand), false, false),
            new(typeof(StartContributedSagaCommand), true, false)
        };
        MessagePropertyAccessor[] propertyAccessors = [CompleteOrderIdAccessor.Instance, StartOrderIdAccessor.Instance];
        settings.GetOrCreate<SagaMetadataCollection>().Add(SagaMetadata.Create<ContributedSaga, ContributedSagaData>(associatedMessages, OrderIdAccessor.Instance, propertyAccessors));

        var messageHandlerRegistry = settings.GetOrCreate<MessageHandlerRegistry>();
        var messageMetadataRegistry = settings.GetOrCreate<MessageMetadataRegistry>();
        messageHandlerRegistry.AddMessageHandlerForMessage<ContributedSaga, CompleteContributedSagaCommand>();
        messageMetadataRegistry.RegisterMessageTypeWithHierarchy(typeof(CompleteContributedSagaCommand), []);
        messageHandlerRegistry.AddMessageHandlerForMessage<ContributedSaga, StartContributedSagaCommand>();
        messageMetadataRegistry.RegisterMessageTypeWithHierarchy(typeof(StartContributedSagaCommand), []);
    }

    sealed class CompleteOrderIdAccessor : MessagePropertyAccessor<CompleteContributedSagaCommand>
    {
        public static readonly MessagePropertyAccessor Instance = new CompleteOrderIdAccessor();

        protected override object? AccessFrom(CompleteContributedSagaCommand message) => message.OrderId;
    }

    sealed class StartOrderIdAccessor : MessagePropertyAccessor<StartContributedSagaCommand>
    {
        public static readonly MessagePropertyAccessor Instance = new StartOrderIdAccessor();

        protected override object? AccessFrom(StartContributedSagaCommand message) => message.OrderId;
    }

    sealed class OrderIdAccessor : CorrelationPropertyAccessor
    {
        public static readonly CorrelationPropertyAccessor Instance = new OrderIdAccessor();

        public override object? AccessFrom(IContainSagaData sagaData) => ((ContributedSagaData)sagaData).OrderId;

        public override void WriteTo(IContainSagaData sagaData, object value) => ((ContributedSagaData)sagaData).OrderId = (string)value;
    }
}

public class ContributedSaga : Saga<ContributedSagaData>,
    IAmStartedByMessages<StartContributedSagaCommand>,
    IHandleMessages<CompleteContributedSagaCommand>
{
    public static bool Started;
    public static bool WasCompleted;

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ContributedSagaData> mapper)
    {
        mapper.MapSaga(s => s.OrderId)
            .ToMessage<StartContributedSagaCommand>(m => m.OrderId)
            .ToMessage<CompleteContributedSagaCommand>(m => m.OrderId);
    }

    public Task Handle(StartContributedSagaCommand message, IMessageHandlerContext context)
    {
        Started = true;
        return Task.CompletedTask;
    }

    public Task Handle(CompleteContributedSagaCommand message, IMessageHandlerContext context)
    {
        WasCompleted = true;
        MarkAsComplete();
        return Task.CompletedTask;
    }
}

public class ContributedSagaData : ContainSagaData
{
    public string OrderId { get; set; } = string.Empty;
}

public class StartContributedSagaCommand : ICommand
{
    public string OrderId { get; set; } = string.Empty;
}

public class CompleteContributedSagaCommand : ICommand
{
    public string OrderId { get; set; } = string.Empty;
}
#endif
