namespace NServiceBus.AcceptanceTesting;

using System;
using Logging;

class ContextAppenderNServiceBusLoggerFactory : ILoggerFactory
{
    public ILog GetLogger(Type type) => GetLogger(type.FullName!);

    public ILog GetLogger(string name) => new ContextAppenderNServiceBusLogger(name);
}