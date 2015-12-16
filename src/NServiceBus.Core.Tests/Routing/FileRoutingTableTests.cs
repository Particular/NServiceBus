namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NUnit.Framework;

    [TestFixture]
    public class FileRoutingTableTests
    {
        [Test]
        public async Task It_retries_loading_the_file_in_case_or_error()
        {
            var timer = new FakeTimer();
            var firstAttempt = true;
            var fileAccess = new FakeFileAccess(() =>
            {
                if (firstAttempt)
                {
                    firstAttempt = false;
                    throw new Exception("Simulated");
                }
                return XDocument.Parse(@"<endpoints><endpoint name=""A""><instance/></endpoint></endpoints>");
            });
            var settings = new SettingsHolder();
            var instances = new EndpointInstances();
            settings.Set<EndpointInstances>(instances);
            var table = new FileRoutingTable("unused", TimeSpan.Zero, timer, fileAccess, 2, settings);
            await table.PerformStartup(null);

            var instance = instances.FindInstances(new EndpointName("A")).Single();
            Assert.AreEqual(new EndpointInstance("A"), instance);
        }

        [Test]
        public async Task If_file_does_not_exist_when_starting_up_it_fails()
        {
            var timer = new FakeTimer();
            var fileAccess = new FakeFileAccess(() =>
            {
                throw new Exception("Simulated");
            });
            var settings = new SettingsHolder();
            var instances = new EndpointInstances();
            settings.Set<EndpointInstances>(instances);
            var table = new FileRoutingTable("unused", TimeSpan.Zero, timer, fileAccess, 3, settings);
            try
            {
                await table.PerformStartup(null);
                Assert.Fail("Expected exception saying file could not be loaed.");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Simulated", ex.Message);
            }
        }

        [Test]
        public async Task It_logs_error_when_filed_access_fails_during_runtime()
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
            var settings = new SettingsHolder();
            var instances = new EndpointInstances();
            settings.Set<EndpointInstances>(instances);
            var table = new FileRoutingTable("unused", TimeSpan.Zero, timer, fileAccess, 1, settings);
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

            public XDocument Load(string path)
            {
                return docCallback();
            }
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

            public Task Stop()
            {
                return Task.FromResult(0);
            }
        }
    }
}