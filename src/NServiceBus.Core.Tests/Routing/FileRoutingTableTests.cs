namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class FileRoutingTableTests
    {
        [Test]
        public void Reload_should_throw_when_file_does_not_exist()
        {
            var timer = new FakeTimer();
            var fileAccess = new FakeFileAccess(() =>
            {
                throw new Exception("Simulated");
            });
            var table = new FileRoutingTable("unused", TimeSpan.Zero, timer, fileAccess, 3);

            var exception = Assert.Throws<Exception>(() => table.ReloadData());

            Assert.That(exception.Message, Does.Contain("Simulated"));
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

            var table = new FileRoutingTable("unused", TimeSpan.Zero, timer, fileAccess, 1);
            await table.PerformStartup(null);

            fail = true;
            await timer.Trigger();

            Assert.IsTrue(errorCallbackInvoked);
        }

        class FakeFileAccess : IRoutingFileAccess
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