#nullable enable

namespace NServiceBus;

using System;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using MicrosoftLoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

static class LoggingBridge
{
    public static IDisposable BeginScope(object slot) => LogManager.BeginSlotScope(slot);

    public static void RegisterMicrosoftFactoryIfAvailable(IServiceProvider serviceProvider, object slot)
    {
        var microsoftLoggerFactory = serviceProvider.GetService<MicrosoftLoggerFactory>();
        if (microsoftLoggerFactory is null)
        {
            return;
        }

        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(microsoftLoggerFactory));
    }
}
