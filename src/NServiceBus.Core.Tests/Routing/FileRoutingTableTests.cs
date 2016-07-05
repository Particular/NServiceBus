﻿namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class FileRoutingTableTests
    {
        [Test]
        public void If_file_does_not_exist_when_starting_up_it_fails()
        {
            var timer = new FakeTimer();
            var fileAccess = new FakeFileAccess(() =>
            {
                throw new Exception("Simulated");
            });

            var table = new FileRoutingTable("unused", TimeSpan.Zero, timer, fileAccess, 3);

            var exception = Assert.ThrowsAsync<Exception>(async () => await table.PerformStartup(null));
            Assert.That(exception.Message, Does.Contain("The endpoint instance mapping file"));
            Assert.That(exception.Message, Does.Contain("does not exist."));
        }

        [Test]
        public void If_file_does_not_exist_when_resolving_for_the_first_time_it_fails()
        {
            var timer = new FakeTimer();
            var fileAccess = new FakeFileAccess(() =>
            {
                throw new Exception("Simulated");
            });

            var table = new FileRoutingTable("unused", TimeSpan.Zero, timer, fileAccess, 3);

            var exception = Assert.ThrowsAsync<Exception>(async () => await table.FindInstances("SomeEndpoint"));
            Assert.That(exception.InnerException.Message, Does.Contain("Simulated"));
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
            await table.FindInstances("SomeEndpoint");

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