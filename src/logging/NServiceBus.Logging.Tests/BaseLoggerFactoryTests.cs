using System;
using NUnit.Framework;

namespace NServiceBus.Logging.Tests
{
    public abstract class BaseLoggerFactoryTests
    {
        protected void CallAllMethodsOnLog(ILog log)
        {
            Assert.IsTrue(log.IsDebugEnabled);
            Assert.IsTrue(log.IsErrorEnabled);
            Assert.IsTrue(log.IsFatalEnabled);
            Assert.IsTrue(log.IsInfoEnabled);
            Assert.IsTrue(log.IsWarnEnabled);

            log.Debug("Testing DEBUG");
            log.Debug("Testing DEBUG Exception", new Exception());
            log.DebugFormat("Testing DEBUG With {0}", "Format");

            log.Info("Testing INFO");
            log.Info("Testing INFO Exception", new Exception());
            log.InfoFormat("Testing INFO With {0}", "Format");

            log.Warn("Testing WARN");
            log.Warn("Testing WARN Exception", new Exception());
            log.WarnFormat("Testing WARN With {0}", "Format");

            log.Error("Testing ERROR");
            log.Error("Testing ERROR Exception", new Exception());
            log.ErrorFormat("Testing ERROR With {0}", "Format");

            log.Fatal("Testing FATAL");
            log.Fatal("Testing FATAL Exception", new Exception());
            log.FatalFormat("Testing FATAL With {0}", "Format");
        }
    }
}