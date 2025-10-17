namespace NServiceBus;

using System.Threading.Tasks;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface ISagaNotFoundHandler<TMessage>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    Task Handle(TMessage message, IMessageProcessingContext context);
}