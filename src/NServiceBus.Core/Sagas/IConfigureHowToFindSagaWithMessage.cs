#nullable enable

namespace NServiceBus;

using System;
using System.Linq.Expressions;
using Sagas;

/// <summary>
/// Implementation provided by the infrastructure - don't implement this
/// or register implementations of it in the container unless you intend
/// to substantially change the way sagas work.
/// </summary>
public interface IConfigureHowToFindSagaWithMessage
{
    /// <summary>
    /// Specify that when the infrastructure is handling a message
    /// of the given type, which message property should be matched to
    /// which saga entity property in the persistent saga store.
    /// </summary>
    void ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object?>> sagaEntityProperty, Expression<Func<TMessage, object?>> messageProperty) where TSagaEntity : IContainSagaData;

    /// <summary>
    /// Configures a custom finder
    /// </summary>
    void ConfigureFinder<TSagaEntity, TMessage, TSagaFinder>() where TSagaFinder : ISagaFinder<TSagaEntity, TMessage> where TSagaEntity : IContainSagaData;

    /// <summary>
    /// Configures a handler when saga is not found
    /// </summary>
    void ConfigureNotFoundHandler<TSagaEntity, TMessage, TNotFoundHandler>() where TNotFoundHandler : ISagaNotFoundHandler<TMessage> where TSagaEntity : IContainSagaData;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSagaNotFoundHandler"></typeparam>
    void ConfigureCatchAllNotFoundHandler<TSagaNotFoundHandler>() where TSagaNotFoundHandler : ISagaNotFoundHandler;
}