namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using NServiceBus.Logging;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class InstanceMappingTableTests
    {
        [Test]
        public void Reload_should_throw_when_file_does_not_exist()
        {
            const string filePath = "some file path";
            var timer = new FakeTimer();
            var fileAccessException = new Exception("Simulated");
            var fileAccess = new FakeFileAccess(() =>
            {
                throw fileAccessException;
            });
            var monitor = new InstanceMappingFileMonitor(filePath, TimeSpan.Zero, timer, fileAccess, new EndpointInstances());

            var exception = Assert.Throws<Exception>(() => monitor.ReloadData());

            Assert.That(exception.Message, Does.Contain($"An error occurred while reading the endpoint instance mapping file at {filePath}. See the inner exception for more details."));
            Assert.That(exception.InnerException, Is.EqualTo(fileAccessException));
        }

        [Test]
        public async Task It_logs_error_when_file_access_fails_during_runtime()
        {
            var errorCallbackInvoked = false;
            var timer = new FakeTimer(ex =>
            {
                errorCallbackInvoked = true;
            });
            var fail = false;
            var fileAccess = new FakeFileAccess(() =>
            {
                // ReSharper disable once AccessToModifiedClosure
                if (fail)
                {
                    throw new Exception("Simulated");
                }
                return XDocument.Parse(@"<endpoints><endpoint name=""A""><instance/></endpoint></endpoints>");
            });

            var monitor = new InstanceMappingFileMonitor("unused", TimeSpan.Zero, timer, fileAccess, new EndpointInstances());
            await monitor.PerformStartup(null);

            fail = true;
            await timer.Trigger();

            Assert.IsTrue(errorCallbackInvoked);
        }

        [Test]
        public void Should_log_added_endpoints()
        {
            var stringBuilder = SetupLogger();

            var fileAccess = new FakeFileAccess(() => XDocument.Parse(@"<endpoints><endpoint name=""A""><instance discriminator=""1"" /><instance discriminator=""2"" /></endpoint></endpoints>"));
            var monitor = new InstanceMappingFileMonitor("filepath", TimeSpan.Zero, new FakeTimer(), fileAccess, new EndpointInstances());

            monitor.ReloadData();

            Assert.That(stringBuilder.ToString(), Does.Contain(@"Updating instance mapping table from 'filepath':
Added endpoint 'A' with 2 instances"));
        }

        [Test]
        public void Should_log_removed_endpoints()
        {
            var stringBuilder = SetupLogger();

            var fileData = new Queue<string>();
            fileData.Enqueue(@"<endpoints><endpoint name=""A""><instance discriminator=""1"" /><instance discriminator=""2"" /></endpoint></endpoints>");
            fileData.Enqueue(@"<endpoints></endpoints>");
            var fileAccess = new FakeFileAccess(() => XDocument.Parse(fileData.Dequeue()));
            var monitor = new InstanceMappingFileMonitor("filepath", TimeSpan.Zero, new FakeTimer(), fileAccess, new EndpointInstances());

            monitor.ReloadData();
            stringBuilder.Clear();
            monitor.ReloadData();

            Assert.That(stringBuilder.ToString(), Does.Contain(@"Updating instance mapping table from 'filepath':
Removed all instances of endpoint 'A'"));
        }

        [Test]
        public void Should_log_changed_instances()
        {
            var stringBuilder = SetupLogger();

            var fileData = new Queue<string>();
            fileData.Enqueue(@"<endpoints><endpoint name=""A""><instance discriminator=""1"" /><instance discriminator=""2"" /></endpoint></endpoints>");
            fileData.Enqueue(@"<endpoints><endpoint name=""A""><instance discriminator=""1"" /><instance discriminator=""3"" /><instance discriminator=""4"" /></endpoint></endpoints>");
            var fileAccess = new FakeFileAccess(() => XDocument.Parse(fileData.Dequeue()));
            var monitor = new InstanceMappingFileMonitor("filepath", TimeSpan.Zero, new FakeTimer(), fileAccess, new EndpointInstances());

            monitor.ReloadData();
            stringBuilder.Clear();
            monitor.ReloadData();

            Assert.That(stringBuilder.ToString(), Does.Contain(@"Updating instance mapping table from 'filepath':
Updated endpoint 'A': +2 instances, -1 instance"));
        }

        static StringBuilder SetupLogger()
        {
            var loggerFactory = LogManager.Use<TestingLoggerFactory>();
            loggerFactory.Level(LogLevel.Info);
            var sb = new StringBuilder();
            var stringWriter = new StringWriter(sb);
            loggerFactory.WriteTo(stringWriter);
            return sb;
        }

        class FakeFileAccess : IInstanceMappingFileAccess
        {
            readonly Func<XDocument> docCallback;

            public FakeFileAccess(Func<XDocument> docCallback)
            {
                this.docCallback = docCallback;
            }

            public XDocument Load(string path) => docCallback();
        }

        class FakeTimer : IAsyncTimer
        {
            Func<Task> theCallback;
            Action<Exception> theErrorCallback;
            Action<Exception> errorSpyCallback;

            public FakeTimer(Action<Exception> errorSpyCallback = null)
            {
                this.errorSpyCallback = errorSpyCallback;
            }

            public async Task Trigger()
            {
                try
                {
                    await theCallback().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    theErrorCallback(ex);
                    errorSpyCallback?.Invoke(ex);
                }
            }

            public void Start(Func<Task> callback, TimeSpan interval, Action<Exception> errorCallback)
            {
                theCallback = callback;
                theErrorCallback = errorCallback;
            }

            public Task Stop() => TaskEx.CompletedTask;
        }
    }
}