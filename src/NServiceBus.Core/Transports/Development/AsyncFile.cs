namespace NServiceBus
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    //TODO: merge with dev persistence
    static class AsyncFile
    {

        public static async Task WriteBytes(string filePath, byte[] bytes)
        {
            using (var stream = GetWriteStream(filePath))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
        }

        public static async Task WriteLines(string filePath, IEnumerable<string> lines)
        {
            using (var stream = GetWriteStream(filePath))
            {
                foreach (var line in lines)
                {
                    var bytes = Encoding.UTF8.GetBytes(line);
                    await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }
            }
        }

        static FileStream GetWriteStream(string filePath)
        {
            return new FileStream(filePath,
                FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true);
        }

        public static Task WriteText(string filePath, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return WriteBytes(filePath, bytes);
        }

        public static async Task<string> ReadText(string filePath)
        {
            var utf8 = Encoding.UTF8;
            using (var stream = GetReadStream(filePath))
            {
                var builder = new StringBuilder();

                var buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0)
                {
                    builder.Append(utf8.GetString(buffer, 0, numRead));
                }

                return builder.ToString();
            }
        }
        public static async Task<byte[]> ReadBytes(string filePath)
        {
            using (var stream = GetReadStream(filePath))
            {
                var length = (int)stream.Length;
                var body = new byte[length];
                await stream.ReadAsync(body, 0, length).ConfigureAwait(false);
                return body;
            }
        }

        public static async Task<List<string>> ReadLines(string filePath)
        {
            using (var stream = GetReadStream(filePath))
            using (var reader = new StreamReader(stream))
            {
                var lines = new List<string>();
                while (true)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null)
                    {
                        break;
                    }
                    lines.Add(line);
                }
                return lines;
            }
        }

        static FileStream GetReadStream(string filePath)
        {
            return new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true);
        }
    }
}