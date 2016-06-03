namespace NServiceBus
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Pipeline;

    class DecryptBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        public DecryptBehavior(EncryptionInspector messageInspector, IEncryptionService encryptionService)
        {
            this.messageInspector = messageInspector;
            this.encryptionService = encryptionService;
        }

        public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            var current = context.Message.Instance;

            foreach (var item in messageInspector.ScanObject(current))
            {
                DecryptMember(item.Item1, item.Item2, context);
            }

            context.UpdateMessageInstance(current);

            return next();
        }


        void DecryptMember(object target, MemberInfo property, IIncomingLogicalMessageContext context)
        {
            var encryptedValue = property.GetValue(target);

            var wireEncryptedString = encryptedValue as WireEncryptedString;
            if (wireEncryptedString != null)
            {
                encryptionService.DecryptValue(wireEncryptedString, context);
            }

            var stringToDecrypt = encryptedValue as string;
            if (stringToDecrypt != null)
            {
                encryptionService.DecryptValue(ref stringToDecrypt, context);
                property.SetValue(target, stringToDecrypt);
            }
        }

        IEncryptionService encryptionService;
        EncryptionInspector messageInspector;

        public class DecryptRegistration : RegisterStep
        {
            public DecryptRegistration(EncryptionInspector inspector, IEncryptionService encryptionService)
                : base("InvokeDecryption", typeof(DecryptBehavior), "Invokes the decryption logic", b => new DecryptBehavior(inspector, encryptionService))
            {
                InsertBefore("MutateIncomingMessages");
            }
        }
    }
}