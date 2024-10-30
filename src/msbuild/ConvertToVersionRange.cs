using System;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class ConvertToVersionRange : Task
{
    [Required]
    public ITaskItem[] References { get; set; } = [];

    [Required]
    public string VersionProperty { get; set; } = string.Empty;

    [Output]
    public ITaskItem[] ReferencesWithVersionRanges { get; private set; } = [];

    public override bool Execute()
    {
        var success = true;

        foreach (var reference in References)
        {
            var automaticVersionRange = reference.GetMetadata("AutomaticVersionRange");

            if (automaticVersionRange.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var privateAssets = reference.GetMetadata("PrivateAssets");

            if (privateAssets.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var version = reference.GetMetadata(VersionProperty);
            var match = Regex.Match(version, @"^\d+");

            if (match.Value.Equals(string.Empty, StringComparison.Ordinal))
            {
                Log.LogError("Reference '{0}' with version '{1}' is not valid for automatic version range conversion. Fix the version or exclude the reference from conversion by setting 'AutomaticVersionRange=\"false\"' on the reference.", reference.ItemSpec, version);
                success = false;
                continue;
            }

            var nextMajor = Convert.ToInt32(match.Value) + 1;

            var versionRange = $"[{version}, {nextMajor}.0.0)";
            reference.SetMetadata(VersionProperty, versionRange);
        }

        ReferencesWithVersionRanges = References;

        return success;
    }
}
