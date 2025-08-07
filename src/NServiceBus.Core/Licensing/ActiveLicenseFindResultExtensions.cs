#nullable enable

namespace NServiceBus;

using Particular.Licensing;

static class ActiveLicenseFindResultExtensions
{
    public static bool HasLicenseExpired(this ActiveLicenseFindResult result) => result.License.HasExpired();
}