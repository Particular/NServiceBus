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

        public static async Task<bool> Move(string sourcePath, string targetPath)
        {
            try
            {
                File.Move(sourcePath, targetPath);
            }
            catch (IOException)
            {
                return false;
            }

            var count = 0;

            while (IsFileLocked(targetPath))
            {
                await Task.Delay(100).ConfigureAwait(false);

                count++;

                if (count > 10)
                {
                    break;
                }
            }

            return true;
        }

        static bool IsFileLocked(string filePath)
        {
            try
            {
                using (File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    //no-op
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }
    }
}
