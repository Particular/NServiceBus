namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using NServiceBus.Routing;
    using NUnit.Framework;

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
            var table = new InstanceMappingTable(filePath, TimeSpan.Zero, timer, fileAccess, new EndpointInstances());

            var exception = Assert.Throws<Exception>(() => table.ReloadData());

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

            var table = new InstanceMappingTable("unused", TimeSpan.Zero, timer, fileAccess, new EndpointInstances());
            await table.PerformStartup(null);

            fail = true;
            await timer.Trigger();

            Assert.IsTrue(errorCallbackInvoked);
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