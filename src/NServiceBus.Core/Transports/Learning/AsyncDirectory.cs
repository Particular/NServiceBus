namespace NServiceBus
{
    using System.IO;
    using System.Threading.Tasks;

    static class AsyncDirectory
    {
        public static async Task Move(string sourcePath, string targetPath)
        {
            var count = 0;
            while (count <= 10)
            {
                try
                {
                    Directory.Move(sourcePath, targetPath);
                    return;
                }
                catch (IOException)
                {
                    if (count == 10)
                    {
                        throw;
                    }
                }

                await Task.Delay(100).ConfigureAwait(false);
                count++;
            }
        }
    }
}