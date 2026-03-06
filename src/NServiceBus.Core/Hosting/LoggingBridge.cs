#nullable enable

namespace NServiceBus;

using System;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using MicrosoftLoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

static class LoggingBridge
{
    public static void ResolveSlotFactory(IServiceProvider serviceProvider, object slot)
    {
        var microsoftLoggerFactory = serviceProvider.GetService<MicrosoftLoggerFactory>();
        if (microsoftLoggerFactory is null)
        {
            LogManager.MarkSlotFactoryAsUnavailable(slot);
            return;
        }

        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(microsoftLoggerFactory));
    }
}