using System;
using NServiceBus.Logging.Log4Net;
using NUnit.Framework;

namespace NServiceBus.Logging.Tests.Log4Net
{
    [TestFixture]
    public class LoggerFactoryTests
    {

        [Test]
        public void Test()
        {
            var loggerFactory = new LoggerFactory();

            var log = loggerFactory.GetLogger("Test");

            Assert.IsInstanceOf<Log>(log);

            CallAllMethodsOnLog(log);

            log = loggerFactory.GetLogger(typeof(LoggerFactoryTests));

            CallAllMethodsOnLog(log);
        }

        private void CallAllMethodsOnLog(ILog log)
        {
            var enabled = log.IsDebugEnabled;
            enabled = log.IsErrorEnabled;
            enabled = log.IsFatalEnabled;
            enabled = log.IsInfoEnabled;
            enabled = log.IsWarnEnabled;

            log.Debug("Testing");
            log.Debug("Testing", new Exception());
            log.DebugFormat("Testing {0}", 1);

            log.Info("Testing");
            log.Info("Testing", new Exception());
            log.InfoFormat("Testing {0}", 1);

            log.Warn("Testing");
            log.Warn("Testing", new Exception());
            log.WarnFormat("Testing {0}", 1);

            log.Error("Testing");
            log.Error("Testing", new Exception());
            log.ErrorFormat("Testing {0}", 1);

            log.Fatal("Testing");
            log.Fatal("Testing", new Exception());
            log.FatalFormat("Testing {0}", 1);
        }
    }
}
