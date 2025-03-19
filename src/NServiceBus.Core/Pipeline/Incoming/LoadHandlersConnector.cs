﻿namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Transport;
using Persistence;
using Pipeline;
using Unicast;

class LoadHandlersConnector : StageConnector<IIncomingLogicalMessageContext, IInvokeHandlerContext>
{
    public LoadHandlersConnector(MessageHandlerRegistry messageHandlerRegistry) => this.messageHandlerRegistry = messageHandlerRegistry;

    public override async Task Invoke(IIncomingLogicalMessageContext context, Func<IInvokeHandlerContext, Task> stage)
    {
        ValidateTransactionMode(context);

        using var scope = context.Builder.CreateScope();
        
        var storageSession = 
            scope.ServiceProvider.GetService<ICompletableSynchronizedStorageSession>() 
            ?? NoOpCompletableSynchronizedStorageSession.Instance;
        
        await storageSession.Open(context).ConfigureAwait(false);

        var handlersToInvoke = messageHandlerRegistry.GetHandlersFor(context.Message.MessageType);

        if (!context.MessageHandled && handlersToInvoke.Count == 0)
        {
            var error = $"No handlers could be found for message type: {context.Message.MessageType}";
            throw new InvalidOperationException(error);
        }

        if (isDebugIsEnabled)
        {
            LogHandlersInvocation(context, handlersToInvoke);
        }

        // capture the message handler types to add them as tags to applicable metrics
        var availableMetricTags = context.Extensions.Get<IncomingPipelineMetricTags>();
        availableMetricTags.Add(MeterTags.MessageHandlerTypes, string.Join(';', handlersToInvoke.Select(x => x.HandlerType.FullName)));

        foreach (var messageHandler in handlersToInvoke)
        {
            messageHandler.Instance = context.Builder.GetRequiredService(messageHandler.HandlerType);

            var handlingContext = this.CreateInvokeHandlerContext(messageHandler, storageSession, context);
            await stage(handlingContext).ConfigureAwait(false);

            if (handlingContext.HandlerInvocationAborted)
            {
                //if the chain was aborted skip the other handlers
                break;
            }
        }
        context.MessageHandled = true;
        await storageSession.CompleteAsync(context.CancellationToken).ConfigureAwait(false);
    }

    static void ValidateTransactionMode(IIncomingLogicalMessageContext context)
    {
        var transportTransaction = context.Extensions.Get<TransportTransaction>();

        if (!transportTransaction.TryGet(out Transaction scopeOpenedByTransport))
        {
            return;
        }

        var currentScope = Transaction.Current ?? throw new InvalidOperationException("The TransactionScope created by the transport has been suppressed. " + scopeInconsistencyMessage);

        if (currentScope != scopeOpenedByTransport)
        {
            throw new InvalidOperationException("A TransactionScope has been created that is overriding the one created by the transport. " + scopeInconsistencyMessage);
        }
    }

    static void LogHandlersInvocation(IIncomingLogicalMessageContext context, List<MessageHandler> handlersToInvoke)
    {
        var builder = new StringBuilder($"Processing message type: {context.Message.MessageType}");
        builder.NewLine("Message headers:");

        foreach (var kvp in context.Headers)
        {
            builder.NewLine($"{kvp.Key} : {kvp.Value}");
        }

        builder.NewLine("Handlers to invoke:");

        foreach (var messageHandler in handlersToInvoke)
        {
            builder.NewLine(messageHandler.HandlerType.FullName);
        }

        logger.Debug(builder.ToString());
    }

    readonly MessageHandlerRegistry messageHandlerRegistry;

    static readonly ILog logger = LogManager.GetLogger<LoadHandlersConnector>();
    static readonly bool isDebugIsEnabled = logger.IsDebugEnabled;
    static readonly string scopeInconsistencyMessage =
        "This can result in inconsistent data because other enlisting operations won't be committed atomically with the receive transaction. " +
        $"The transport transaction mode must be changed to something other than '{nameof(TransportTransactionMode.TransactionScope)}' before attempting to manually control the TransactionScope in the pipeline.";
}