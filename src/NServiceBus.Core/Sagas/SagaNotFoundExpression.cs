namespace NServiceBus;

/// <summary>
/// 
/// </summary>
public class SagaNotFoundExpression<TSagaData, TMessage> where TSagaData : IContainSagaData
{
    readonly IConfigureHowToFindSagaWithMessage configureHowToFindSagaWithMessage;

    internal SagaNotFoundExpression(IConfigureHowToFindSagaWithMessage configureHowToFindSagaWithMessage) => this.configureHowToFindSagaWithMessage = configureHowToFindSagaWithMessage;

    /// <summary>
    /// 
    /// </summary>
    public void RegisterNotFoundHandler<TSagaNotFoundHandler>() where TSagaNotFoundHandler : ISagaNotFoundHandler<TMessage> => configureHowToFindSagaWithMessage.ConfigureNotFoundHandler<TSagaData, TMessage, TSagaNotFoundHandler>();
}