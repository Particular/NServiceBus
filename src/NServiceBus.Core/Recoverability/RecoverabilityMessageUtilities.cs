namespace NServiceBus
{
    using System;

    static class RecoverabilityExtensions
    {
        public static byte[] Copy(this byte[] body)
        {
            if (body == null)
            {
                return null;
            }

            var copyBody = new byte[body.Length];

            Buffer.BlockCopy(body, 0, copyBody, 0, body.Length);

            return copyBody;
        }
    }
}