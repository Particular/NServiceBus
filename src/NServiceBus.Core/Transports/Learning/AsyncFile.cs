namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Logging;

    static class AsyncFile
    {
        static readonly ILog log = LogManager.GetLogger(typeof(AsyncFile));

        public static async Task WriteBytes(string filePath, byte[] bytes, CancellationToken cancellationToken = default)
        {
            using (var stream = CreateWriteStream(filePath, FileMode.Create))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
            }
        }

        //write to temp file first so we can do a atomic move
        public static async Task WriteTextAtomic(string targetPath, string text, CancellationToken cancellationToken = default)
        {
            var tempFile = Path.GetTempFileName();
            var bytes = Encoding.UTF8.GetBytes(text);

            try
            {
                using (var stream = CreateWriteStream(tempFile, FileMode.Open))
                {
                    await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
            {
                // When we introduced cancellation, the general catch was already present.
                // We didn't want to change the behavior there,
                // but we did want to prevent deleting the file from throwing an exception
                // which would mask the OperationCanceledException.
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception deleteEx)
                {
                    log.Warn("Failed to delete file.", deleteEx);
                }

                throw;
            }
            catch
            {
                File.Delete(tempFile);
                throw;
            }

            File.Move(tempFile, targetPath);
        }

        static FileStream CreateWriteStream(string filePath, FileMode fileMode)
        {
            return new FileStream(filePath, fileMode, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        }

        public static Task WriteText(string filePath, string text, CancellationToken cancellationToken = default)
        {
            var bytes = Encoding.UTF8.GetBytes(text);

            return WriteBytes(filePath, bytes, cancellationToken);
        }

        public static async Task<string> ReadText(string filePath, CancellationToken cancellationToken = default)
        {
            using (var stream = new StreamReader(CreateReadStream(filePath), Encoding.UTF8))
            {
                // The token isn't currently required but later, a method in a descendant call stack may get a token overload.
                // When that happens, we want CA2016 to tell us to forward the token, so we want to keep the parameter.
                // This line makes the parameter "required".
                cancellationToken.ThrowIfCancellationRequested();

                var result = await stream.ReadToEndAsync().ConfigureAwait(false);

                return result;
            }
        }

        public static async Task<byte[]> ReadBytes(string filePath, CancellationToken cancellationToken = default)
        {
            using (var stream = CreateReadStream(filePath))
            {
                var length = (int)stream.Length;
                var body = new byte[length];
                await stream.ReadAsync(body, 0, length, cancellationToken).ConfigureAwait(false);

                return body;
            }
        }

        static FileStream CreateReadStream(string filePath)
        {
            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        }

        public static async Task<bool> Move(string sourcePath, string targetPath, CancellationToken cancellationToken = default)
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

            while (count <= 10)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsFileLocked(targetPath))
                {
                    break;
                }

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);

                count++;
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
