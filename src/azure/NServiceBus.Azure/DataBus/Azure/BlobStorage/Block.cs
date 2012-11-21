namespace NServiceBus.DataBus.Azure.BlobStorage
{
    using System;

    public class Block
    {
        public int Offset { get; set; }

        public int Length { get; set; }

        public int Attempt { get; set; }

        public string Name { get{
            return Convert.ToBase64String(BitConverter.GetBytes(Offset));
        }}
    }
}