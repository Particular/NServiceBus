namespace Commands
{
    using System;

    public class MyCommand
    {
        public Guid CommandId { get; set; }
        public string EncryptedString { get; set; }
    }
}
