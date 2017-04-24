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
            using (var stream = CreateWriteStream(filePath, FileMode.Create))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
        }

        public static async Task WriteLines(string filePath, IEnumerable<string> lines)
        {
            using (var stream = CreateWriteStream(filePath, FileMode.Create))
            {
                await WriteLines(stream, lines).ConfigureAwait(false);
            }
        }

        //write to temp file first so we can do a atomic move
        public static async Task WriteTextAtomic(string targetPath, string text)
        {
            var tempFile = Path.GetTempFileName();
            var bytes = Encoding.UTF8.GetBytes(text);
            try
            {
                using (var stream = CreateWriteStream(tempFile, FileMode.Open))
                {
                    await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }
            }
            catch
            {
                File.Delete(tempFile);
                throw;
            }
            File.Move(tempFile, targetPath);
        }

        static async Task WriteLines(FileStream stream, IEnumerable<string> lines)
        {
            using (var writer = new StreamWriter(stream))
            {
                foreach (var line in lines)
                {
                    await writer.WriteLineAsync(line).ConfigureAwait(false);
                }
            }
        }

        static FileStream CreateWriteStream(string filePath, FileMode fileMode)
        {
            return new FileStream(filePath,
                fileMode, FileAccess.Write, FileShare.None,
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
            using (var stream = CreateReadStream(filePath))
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
            using (var stream = CreateReadStream(filePath))
            {
                var length = (int)stream.Length;
                var body = new byte[length];
                await stream.ReadAsync(body, 0, length).ConfigureAwait(false);
                return body;
            }
        }

        static FileStream CreateReadStream(string filePath)
        {
            return new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true);
        }
    }
}