namespace NServiceBus.Core.Tests
{
    using System.IO;

    public static class StringStreamExtensions
    {
        public static Stream ConvertToStream(this string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        public static string ConvertToString(this Stream stream)
        {
            stream.Position = 0;
            var sr = new StreamReader(stream);
            return sr.ReadToEnd();
        }
    }
}