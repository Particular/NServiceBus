namespace Rhino.Licensing
{
    using System.ServiceModel;

    /// <summary>
    /// Service contract of subscription server.
    /// </summary>
    [ServiceContract]
    public interface ISubscriptionLicensingService
    {
        /// <summary>
        /// Issues a leased license
        /// </summary>
        [OperationContract]
        string LeaseLicense(string previousLicense);
    }
}