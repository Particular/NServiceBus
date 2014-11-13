namespace NServiceBus.Core.Tests.Sagas
{
    using System;
    using System.IO;
    using System.Threading;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class FileBasedRoundRobinRoutingDistributorTests
    {
        FileBasedRoundRobinRoutingDistributor routing;
        string basePath;

        [TestFixtureSetUp]
        public void Init()
        {
            basePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            Directory.CreateDirectory(basePath);

        }

        [SetUp]
        public void Setup()
        {
            routing = new FileBasedRoundRobinRoutingDistributor(basePath, TimeSpan.FromMilliseconds(100));
        }

        [TearDown]
        public void TearDown()
        {
            routing.Dispose();
            routing = null;
        }

        [TestFixtureTearDown]
        public void CleanUp()
        {
            Directory.Delete(basePath, true);
        }

        [Test]
        public void NoRoutingFile()
        {
            string address;

            Assert.IsFalse(routing.TryGetRouteAddress("QueueA", out address));
        }

        [Test]
        public void WithRoutingFile()
        {
            string address;

            File.WriteAllLines(Path.Combine(basePath, "QueueB.txt"), new[]
            {
                "WorkerA",
                "WorkerB"
            });

            StartMonitoring("QueueB");
            Assert.IsTrue(routing.TryGetRouteAddress("QueueB", out address));
        }

        [Test]
        public void WithRoutingFile_RoundRobbin()
        {
            string address;

            File.WriteAllLines(Path.Combine(basePath, "QueueB.txt"), new[]
            {
                "WorkerA",
                "WorkerB"
            });

            StartMonitoring("QueueB");

            routing.TryGetRouteAddress("QueueB", out address);
            Assert.AreEqual("WorkerA", address);
            routing.TryGetRouteAddress("QueueB", out address);
            Assert.AreEqual("WorkerB", address);
            routing.TryGetRouteAddress("QueueB", out address);
            Assert.AreEqual("WorkerA", address);
            routing.TryGetRouteAddress("QueueB", out address);
            Assert.AreEqual("WorkerB", address);
        }

        [Test]
        public void WithRoutingFile_ModifyFile()
        {
            string address;

            File.WriteAllLines(Path.Combine(basePath, "QueueB.txt"), new[]
            {
                "WorkerA",
                "WorkerB"
            });

            StartMonitoring("QueueB");

            routing.TryGetRouteAddress("QueueB", out address);
            Assert.AreEqual("WorkerA", address);

            File.WriteAllLines(Path.Combine(basePath, "QueueB.txt"), new[]
            {
                "WorkerA",
                "WorkerB",
                "WorkerC"
            });

            Thread.Sleep(2000);

            routing.TryGetRouteAddress("QueueB", out address);
            Assert.AreEqual("WorkerA", address);
            routing.TryGetRouteAddress("QueueB", out address);
            Assert.AreEqual("WorkerB", address);
            routing.TryGetRouteAddress("QueueB", out address);
            Assert.AreEqual("WorkerC", address);
        }

        void StartMonitoring(string queueName)
        {
            string address;
            routing.TryGetRouteAddress(queueName, out address);
            Thread.Sleep(1000);
        }
    }
}