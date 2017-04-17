namespace Particular.Licensing
{
    using System.Collections.Generic;

    class ActiveLicenseFindResult
    {
        internal License License { get; set; }

        internal bool HasExpired { get; set; }

        internal string Location { get; set; }

        internal List<string> Report = new List<string>();

        internal List<string> SelectedLicenseReport = new List<string>();
    }
}
