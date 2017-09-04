namespace NServiceBus
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

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
            return new FileStream(filePath, fileMode, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        }

        public static Task WriteText(string filePath, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);

            return WriteBytes(filePath, bytes);
        }

        public static async Task<string> ReadText(string filePath)
        {
            using (var stream = new StreamReader(CreateReadStream(filePath), Encoding.UTF8))
            {
                var result = await stream.ReadToEndAsync().ConfigureAwait(false);

                return result;
            }
        }

        public static async Task<byte[]> ReadBytes(string filePath, CancellationToken token = default)
        {
            using (var stream = CreateReadStream(filePath))
            {
                var length = (int)stream.Length;
                var body = new byte[length];
                await stream.ReadAsync(body, 0, length, token).ConfigureAwait(false);

                return body;
            }
        }

        static FileStream CreateReadStream(string filePath)
        {
            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        }
    }
}
