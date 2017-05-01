namespace NServiceBus.Core.Tests.Transports.Learning
{
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class AsyncFileTests
    {
        [Test]
        public async Task When_file_content_larger_than_buffer_size()
        {
            var originalContent = string.Join("", Enumerable.Repeat("a#~×ψؾࢯ‽%1", 2000));
            const string filePath = "test.txt";

            try
            {
                await AsyncFile.WriteText(filePath, originalContent);

                var content = await AsyncFile.ReadText(filePath);

                Assert.AreEqual(originalContent, content);
            }
            finally
            {
                File.Delete(filePath);
            }
        }
    }
}