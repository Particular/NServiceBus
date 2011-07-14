using System;
namespace NServiceBus.Testing
{
    /// <summary>
    /// Predicate for checking the return code sent to Bus.Return.
    /// </summary>
    /// <param name="returnCode"></param>
    /// <returns></returns>
    public delegate bool ReturnPredicate(int returnCode);

    /// <summary>
    /// Predicate for checking the message passed to Bus.Publish.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <returns></returns>
    public delegate bool PublishPredicate<T>(T message) where T : IMessage;

    /// <summary>
    /// Predicate for checking the message passed to Bus.Send.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <returns></returns>
    public delegate bool SendPredicate<T>(T message) where T : IMessage;

    /// <summary>
    /// Predicate for checking the message and the destination passed to Bus.Send(msg, destination).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="destination"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public delegate bool SendToDestinationPredicate<T>(string destination, T message) where T : IMessage;

    internal delegate bool BusPublishDelegate<T>(T[] msgs) where T : IMessage;

    internal delegate bool BusSendWithDestinationAndCorrelationIdDelegate(string destination, string correlationId, IMessage[] msgs);

    internal delegate bool BusSendWithDestinationDelegate(string destination, IMessage[] msgs);

    internal delegate bool BusSendDelegate(IMessage[] msgs);

    /// <summary>
    /// Delegate used for dispatching the call to invoke the saga.
    /// </summary>
    public delegate void HandleMessageDelegate();
}
