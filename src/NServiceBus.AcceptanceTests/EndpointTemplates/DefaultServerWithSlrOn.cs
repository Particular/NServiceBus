namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    class DefaultServerWithSLROn : DefaultServer
    {
        protected override bool DisableSLR()
        {
            return false;
        }
    }
}