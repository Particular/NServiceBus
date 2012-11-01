namespace NServiceBus.Gateway.Utils
{
    using System.IO;

    public static class StreamExtensions
    {
        public static void CopyTo_net35(this Stream input, Stream output)
        {
            CopyTo_net35(input,output,4096);
        }
        public static void CopyTo_net35(this Stream input, Stream output,long bufferSize)
        {
            int num;
            var buffer = new byte[bufferSize];
            while ((num = input.Read(buffer, 0, buffer.Length)) != 0)
            {
                output.Write(buffer, 0, num);
            }
        }
    }
}