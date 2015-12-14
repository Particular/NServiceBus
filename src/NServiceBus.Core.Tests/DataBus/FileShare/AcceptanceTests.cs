namespace NServiceBus.Core.Tests.DataBus.FileShare
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class AcceptanceTests
    {
        [SetUp]
        public void SetUp()
        {
            dataBus = new FileShareDataBusImplementation(basePath) {MaxMessageTimeToLive = TimeSpan.MaxValue};
        }

        FileShareDataBusImplementation dataBus;
        string basePath = Path.GetTempPath();

        Task<string> Put(string content, TimeSpan timeToLive)
        {
            var byteArray = Encoding.ASCII.GetBytes(content);
            using (var stream = new MemoryStream(byteArray))
            {
                return dataBus.Put(stream, timeToLive);
            }
        }

        [Test]
        public async Task Should_handle_be_able_to_read_stored_values()
        {
            const string content = "Test";

            var key = await Put(content, TimeSpan.MaxValue);
            using (var stream = await dataBus.Get(key))
            {
                Assert.AreEqual(new StreamReader(stream).ReadToEnd(), content);
            }
        }

        [Test]
        public async Task Should_handle_be_able_to_read_stored_values_concurrently()
        {
            const string content = "Test";

            var key = await Put(content, TimeSpan.MaxValue);

            Parallel.For(0, 10, async i =>
            {
                using (var stream = await dataBus.Get(key))
                {
                    Assert.AreEqual(new StreamReader(stream).ReadToEnd(), content);
                }
            });
        }

        [Test]
        public void Should_handle_max_ttl()
        {
            Put("Test", TimeSpan.MaxValue);
            Assert.True(Directory.Exists(Path.Combine(basePath, DateTime.MaxValue.ToString("yyyy-MM-dd_HH"))));
        }

        [Test]
        public void Should_honor_the_ttl_limit()
        {
            dataBus.MaxMessageTimeToLive = TimeSpan.FromDays(1);

            Put("Test", TimeSpan.MaxValue);
            Assert.True(Directory.Exists(Path.Combine(basePath, DateTime.Now.AddDays(1).ToString("yyyy-MM-dd_HH"))));
        }
    }
}