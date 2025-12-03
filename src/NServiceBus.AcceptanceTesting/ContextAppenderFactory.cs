namespace NServiceBus.AcceptanceTesting;

using System;
using Logging;

class ContextAppenderFactory : ILoggerFactory
{
    public ILog GetLogger(Type type) => GetLogger(type.FullName!);

    public ILog GetLogger(string name) => new ContextAppender(name);
}